using System.Globalization;
using RatioMaster.Core.Abstractions;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Logging;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Sessions;

/// <summary>
/// The ratio-faking engine for one torrent. Ports the behaviour of the old RM control: it announces
/// started/regular/completed/stopped events, grows the uploaded/downloaded counters each second, honours
/// the tracker interval, scrapes, and evaluates stop conditions. UI-free and driven by injected time,
/// randomness and tracker client so it is fully testable.
/// </summary>
public sealed class TorrentSession : IAsyncDisposable
{
    private const int MinIntervalSeconds = 60;
    private const int MaxIntervalSeconds = 24 * 60 * 60;
    private const long RatioMinimumDownloadedBytes = 100 * 1024;

    /// <summary>After an announce that got no answer, try again this soon instead of waiting the full interval.</summary>
    internal const int RetryDelaySeconds = 60;

    private readonly TorrentDescriptor _descriptor;
    private readonly ClientProfile _profile;
    private readonly ClientIdentity _identity;
    private readonly ITrackerClient _tracker;
    private readonly ILocalIpProvider _localIpProvider;
    private readonly IRandomSource _random;
    private readonly ISystemClock _clock;
    private readonly Lock _gate = new();

    private TorrentSettings _settings;
    private TorrentSessionState _state = TorrentSessionState.Idle;
    private string? _stopReason;

    private long _uploaded;
    private long _downloaded;
    private long _left;
    private long _totalSize;
    private bool _seedMode;
    private bool _haveInitialPeers;

    private long _currentUploadRate;
    private long _currentDownloadRate;
    private bool _uploadRandomEnabled;

    private int _currentInterval;
    private int _elapsedInInterval;
    private int _totalRunningSeconds;
    private bool _forceUpdate;

    private int? _seeders;
    private int? _leechers;

    private PeerListener? _listener;
    private CancellationTokenSource? _loopCts;
    private Task? _loop;
    private bool _shouldStop;

    public TorrentSession(
        TorrentDescriptor descriptor,
        ClientProfile profile,
        ClientIdentity identity,
        TorrentSettings settings,
        ITrackerClient tracker,
        ILocalIpProvider? localIpProvider = null,
        IRandomSource? random = null,
        ISystemClock? clock = null)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _localIpProvider = localIpProvider ?? LocalIpProvider.Instance;
        _random = random ?? SystemRandomSource.Instance;
        _clock = clock ?? SystemClock.Instance;
        _currentUploadRate = settings.UploadRateBytes;
        _currentDownloadRate = settings.DownloadRateBytes;
        _uploadRandomEnabled = settings.UploadRandomEnabled;
        _currentInterval = ClampInterval(settings.IntervalSeconds);
    }

    public event EventHandler<TorrentSessionState>? StateChanged;

    public event EventHandler<LogEntry>? LogEmitted;

    public event EventHandler<TrackerExchange>? TrackerExchangeCompleted;

    public event EventHandler<EngineAdjustment>? Adjusted;

    public TorrentDescriptor Descriptor => _descriptor;

    public ClientProfile Profile => _profile;

    public ClientIdentity Identity => _identity;

    public TorrentSettings Settings => _settings;

    public TorrentSessionState State => _state;

    public bool IsListening => _listener?.IsListening ?? false;

    public TorrentStats Snapshot
    {
        get
        {
            lock (_gate)
            {
                return new TorrentStats
                {
                    State = _state,
                    StopReason = _stopReason,
                    Uploaded = _uploaded,
                    Downloaded = _downloaded,
                    Left = _left,
                    TotalSize = _totalSize,
                    Ratio = ComputeRatio(),
                    FinishedPercent = ComputeFinishedPercent(),
                    Seeders = _seeders,
                    Leechers = _leechers,
                    UploadRateBytes = UploadPaused ? 0 : _currentUploadRate,
                    DownloadRateBytes = _currentDownloadRate,
                    TotalRunningTime = TimeSpan.FromSeconds(_totalRunningSeconds),
                    NextUpdateIn = _state is TorrentSessionState.Idle or TorrentSessionState.Stopped or TorrentSessionState.Error
                        ? null
                        : TimeSpan.FromSeconds(Math.Max(0, _currentInterval - _elapsedInInterval)),
                    StopAfterRemaining = ComputeStopAfterRemaining(),
                };
            }
        }
    }

    /// <summary>
    /// Starts the background run loop (started announce, per-second ticks, stopped announce on cancel).
    /// A session that stopped on its own (stop condition, tracker error) can be started again.
    /// </summary>
    public void Start()
    {
        if (_loop is { IsCompleted: false })
        {
            return;
        }

        _loopCts?.Dispose();
        var cts = new CancellationTokenSource();
        _loopCts = cts;
        _loop = Task.Run(() => RunAsync(cts.Token));
    }

    /// <summary>Stops the run loop and sends the stopped announce.</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is null)
        {
            if (_state is not (TorrentSessionState.Stopped or TorrentSessionState.Idle or TorrentSessionState.Error))
            {
                await StopWithReasonAsync(null, isError: false, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (_loopCts is not null)
        {
            await _loopCts.CancelAsync().ConfigureAwait(false);
        }

        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        _loop = null;
        _loopCts?.Dispose();
        _loopCts = null;
    }

    /// <summary>Requests an immediate announce on the next tick (the old Manual Update button).</summary>
    public void RequestUpdate()
    {
        lock (_gate)
        {
            _forceUpdate = true;
        }
    }

    /// <summary>
    /// Applies user edits that are allowed while running (speeds, random ranges, the no-leecher pause,
    /// log, scrape, ignore-failure).
    /// </summary>
    public void UpdateLiveSettings(TorrentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        bool pausedBefore, pausedNow;
        lock (_gate)
        {
            pausedBefore = UploadPaused;
            _settings = settings;
            _currentUploadRate = settings.UploadRateBytes;
            _currentDownloadRate = _seedMode ? 0 : settings.DownloadRateBytes;
            _uploadRandomEnabled = settings.UploadRandomEnabled;
            pausedNow = UploadPaused;
        }

        LogPauseChange(pausedBefore, pausedNow, "the pause on no leechers was switched off");
    }

    /// <summary>
    /// True while the tracker's last swarm figures said nobody is downloading and the settings ask to
    /// stop uploading then. Read under <see cref="_gate"/>. This gates the upload counter itself, so no
    /// other rule (a random speed on the next update, a live edit of the speed) can upload past it.
    /// </summary>
    private bool UploadPaused => _settings.StopUploadWhenNoLeechers && _leechers == 0;

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);
            if (_state == TorrentSessionState.Error)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (!_shouldStop && await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled by StopAsync; fall through to the stopped announce.
        }
        catch (Exception ex)
        {
            // Nothing may leave the loop dead while the session still looks like it is running: report the
            // failure the way a tracker error is reported, so the user sees it and can start again.
            Log(LogLevel.Error, $"Unexpected error: {ex.Message}");
            await StopWithReasonAsync($"unexpected error: {ex.Message}", isError: true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (_state is not (TorrentSessionState.Stopped or TorrentSessionState.Error))
            {
                await StopWithReasonAsync(null, isError: false, CancellationToken.None).ConfigureAwait(false);
            }

            await CloseListenerAsync().ConfigureAwait(false);
        }
    }

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _uploaded = 0;
            _downloaded = 0;
            _totalRunningSeconds = 0;
            _elapsedInInterval = 0;
            _haveInitialPeers = false;
            _shouldStop = false;
            _forceUpdate = false;
            _seeders = null;
            _leechers = null;
            _stopReason = null;
            _currentUploadRate = _settings.UploadRateBytes;
            _currentDownloadRate = _settings.DownloadRateBytes;
            _uploadRandomEnabled = _settings.UploadRandomEnabled;
            _currentInterval = ClampInterval(_settings.IntervalSeconds);
            InitializeSizeFromFinishedPercent();
        }

        SetState(TorrentSessionState.Starting);
        if (TrackerUrl.Validate(_descriptor.TrackerUrl) is { } problem)
        {
            // There is nothing to announce to, so no stopped announce either.
            await StopWithReasonAsync(problem, isError: true, cancellationToken, announceStopped: false).ConfigureAwait(false);
            return;
        }

        LocalIp = await _localIpProvider.GetLocalIpAsync(cancellationToken).ConfigureAwait(false);
        OpenListener();

        var response = await DoAnnounceAsync(TrackerEvent.Started, cancellationToken).ConfigureAwait(false);
        if (await HandleFailureAsync(response, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await DoScrapeAsync(cancellationToken).ConfigureAwait(false);
        SetRunningState();
    }

    internal async Task TickAsync(CancellationToken cancellationToken)
    {
        if (_state is TorrentSessionState.Stopped or TorrentSessionState.Error or TorrentSessionState.Idle)
        {
            return;
        }

        if (_haveInitialPeers)
        {
            bool completed;
            lock (_gate)
            {
                completed = ApplyCounterGrowth();
            }

            if (completed)
            {
                await DoAnnounceAsync(TrackerEvent.Completed, cancellationToken).ConfigureAwait(false);
                await DoScrapeAsync(cancellationToken).ConfigureAwait(false);
                SetState(TorrentSessionState.Seeding);
            }
        }

        bool announceNow;
        lock (_gate)
        {
            _totalRunningSeconds++;
            announceNow = _forceUpdate || _currentInterval - _elapsedInInterval <= 0;
        }

        if (EvaluateStopConditions() is { } reason)
        {
            await StopWithReasonAsync(reason, isError: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!announceNow)
        {
            lock (_gate)
            {
                _elapsedInInterval++;
            }

            return;
        }

        lock (_gate)
        {
            _forceUpdate = false;
            _elapsedInInterval = 0;
            ApplyNextUpdateRandomSpeeds();
        }

        OpenListener();
        var response = await DoAnnounceAsync(TrackerEvent.None, cancellationToken).ConfigureAwait(false);
        if (await HandleFailureAsync(response, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await DoScrapeAsync(cancellationToken).ConfigureAwait(false);
        SetRunningState();
    }

    private string LocalIp { get; set; } = LocalIpProvider.Fallback;

    private void InitializeSizeFromFinishedPercent()
    {
        var finished = Math.Clamp(_settings.FinishedPercent, 0, 100);
        if (finished <= 0)
        {
            _totalSize = _descriptor.TotalLength;
            _seedMode = false;
        }
        else if (finished >= 100)
        {
            _totalSize = 0;
            _seedMode = true;
        }
        else
        {
            _totalSize = (long)(_descriptor.TotalLength * (100 - finished) / 100);
            _seedMode = false;
        }

        _left = _totalSize;
        if (_seedMode)
        {
            _currentDownloadRate = 0;
        }
    }

    /// <summary>Grows the counters for one second. Returns true when the download has just completed.</summary>
    private bool ApplyCounterGrowth()
    {
        if (!UploadPaused)
        {
            var uploadedThisTick = _currentUploadRate + Jitter(_uploadRandomEnabled, _settings.UploadRandomMinKb, _settings.UploadRandomMaxKb);
            _uploaded += Math.Max(0, uploadedThisTick);
        }

        if (!_seedMode && _currentDownloadRate > 0)
        {
            var downloadedThisTick = _currentDownloadRate + Jitter(_settings.DownloadRandomEnabled, _settings.DownloadRandomMinKb, _settings.DownloadRandomMaxKb);
            _downloaded += Math.Max(0, downloadedThisTick);
            _left = _totalSize - _downloaded;
        }

        if (_left <= 0)
        {
            _downloaded = _totalSize;
            _left = 0;
            _currentDownloadRate = 0;
            if (!_seedMode)
            {
                _seedMode = true;
                return true;
            }
        }

        return false;
    }

    private long Jitter(bool enabled, int minKb, int maxKb)
    {
        if (!enabled)
        {
            return _random.Next(10);
        }

        var lo = Math.Min(minKb, maxKb);
        var hi = Math.Max(minKb, maxKb);
        var kb = hi <= lo ? lo : _random.Next(lo, hi);
        return (long)kb * 1024;
    }

    private void ApplyNextUpdateRandomSpeeds()
    {
        if (_settings.NextUpdateRandomUpload)
        {
            _currentUploadRate = RandomRateBytes(_settings.NextUpdateUploadMinKb, _settings.NextUpdateUploadMaxKb);
        }

        if (_settings.NextUpdateRandomDownload && !_seedMode)
        {
            _currentDownloadRate = RandomRateBytes(_settings.NextUpdateDownloadMinKb, _settings.NextUpdateDownloadMaxKb);
        }
    }

    private long RandomRateBytes(int minKb, int maxKb)
    {
        var lo = Math.Min(minKb, maxKb);
        var hi = Math.Max(minKb, maxKb);
        var kb = hi <= lo ? lo : _random.Next(lo, hi);
        return (long)kb * 1024;
    }

    private AnnounceValues BuildAnnounceValues()
    {
        lock (_gate)
        {
            _uploaded = AnnounceMath.RoundDown(_uploaded, AnnounceMath.UploadedDenominator);
            _downloaded = AnnounceMath.RoundDown(_downloaded, AnnounceMath.DownloadedDenominator);
            if (_left > 0)
            {
                _left = _totalSize - _downloaded;
            }

            return new AnnounceValues
            {
                InfoHashEncoded = InfoHashEncoder.Encode(_descriptor.InfoHash, _profile.HashUpperCase),
                PeerId = _identity.PeerId,
                Port = _identity.Port,
                Uploaded = _uploaded,
                Downloaded = _downloaded,
                Left = _left,
                Key = _identity.Key,
                NumWant = _identity.NumWant,
                LocalIp = LocalIp,
            };
        }
    }

    private async Task<AnnounceResponse?> DoAnnounceAsync(TrackerEvent trackerEvent, CancellationToken cancellationToken)
    {
        if (trackerEvent != TrackerEvent.Started && trackerEvent != TrackerEvent.Stopped)
        {
            SetState(TorrentSessionState.Updating);
        }

        var values = BuildAnnounceValues();
        Log(LogLevel.Info, $"Announcing {DescribeEvent(trackerEvent)} to tracker");
        try
        {
            var outcome = await _tracker.AnnounceAsync(
                _profile, _descriptor.TrackerUrl, values, trackerEvent, _settings.Proxy, _settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
            ApplyAnnounceResponse(outcome.Response);
            return outcome.Response;
        }
        catch (Exception ex) when (ex is TrackerException or IOException or ProxyException)
        {
            Log(LogLevel.Warning, $"No connection to tracker: {ex.Message}");
            ScheduleRetry();
            return null;
        }
    }

    /// <summary>Pulls the next announce forward so a tracker that did not answer is retried within a minute.</summary>
    private void ScheduleRetry()
    {
        lock (_gate)
        {
            _elapsedInInterval = Math.Max(_elapsedInInterval, _currentInterval - RetryDelaySeconds);
        }

        Log(LogLevel.Info, $"Will retry in {RetryDelaySeconds} seconds.");
    }

    private void ApplyAnnounceResponse(AnnounceResponse response)
    {
        if (response.HasFailure)
        {
            Log(LogLevel.Error, $"Tracker error: {response.FailureReason}");
            return;
        }

        foreach (var (key, value) in response.ExtraKeys)
        {
            Log(LogLevel.Info, $"{key}: {value}");
        }

        if (response.Interval is { } interval)
        {
            var clamped = ClampInterval(interval);
            if (clamped != _currentInterval)
            {
                _currentInterval = clamped;
                Adjusted?.Invoke(this, new EngineAdjustment { IntervalSeconds = clamped, Reason = "Tracker set the announce interval." });
            }
        }

        if (response.Complete is { } complete && response.Incomplete is { } incomplete)
        {
            UpdateSwarmStats(complete, incomplete);
        }

        _haveInitialPeers = true;
        if (response.Peers.Count > 0)
        {
            Log(LogLevel.Info, $"peers: ({response.Peers.Count})");
        }
    }

    private async Task DoScrapeAsync(CancellationToken cancellationToken)
    {
        if (!_settings.RequestScrape)
        {
            return;
        }

        var encoded = InfoHashEncoder.Encode(_descriptor.InfoHash, _profile.HashUpperCase);
        try
        {
            var outcome = await _tracker.ScrapeAsync(
                _profile, _descriptor.TrackerUrl, encoded, _descriptor.InfoHash, _settings.Proxy, _settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
            if (outcome.Response is { HasFailure: false } scrape && scrape is { Complete: { } complete, Incomplete: { } incomplete })
            {
                Log(LogLevel.Info, $"scrape: complete={complete} incomplete={incomplete} downloaded={scrape.Downloaded}");
                UpdateSwarmStats(complete, incomplete);
            }
        }
        catch (Exception ex) when (ex is TrackerException or IOException or ProxyException)
        {
            Log(LogLevel.Warning, $"Scrape error: {ex.Message}");
        }
    }

    private void UpdateSwarmStats(int complete, int incomplete)
    {
        bool pausedBefore, pausedNow;
        lock (_gate)
        {
            pausedBefore = UploadPaused;
            _seeders = complete;
            _leechers = incomplete;
            pausedNow = UploadPaused;
        }

        LogPauseChange(pausedBefore, pausedNow, $"the tracker reports {incomplete} leechers");
    }

    private void LogPauseChange(bool pausedBefore, bool pausedNow, string resumeReason)
    {
        if (pausedNow && !pausedBefore)
        {
            Log(LogLevel.Info, "The tracker reports no leechers; upload paused until it reports some.");
        }
        else if (!pausedNow && pausedBefore)
        {
            Log(LogLevel.Info, $"Upload resumed: {resumeReason}.");
        }
    }

    private async Task<bool> HandleFailureAsync(AnnounceResponse? response, CancellationToken cancellationToken)
    {
        if (response is { HasFailure: true } && !_settings.IgnoreFailureReason)
        {
            await StopWithReasonAsync($"Tracker error: {response.FailureReason}", isError: true, cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private string? EvaluateStopConditions()
    {
        var stop = _settings.Stop;
        switch (stop.Type)
        {
            case StopConditionType.AfterSeconds when stop.Value > 0 && _totalRunningSeconds >= stop.Value:
                return $"ran for {stop.Value:0} seconds";
            case StopConditionType.SeedersBelow when _seeders is { } s && s < stop.Value:
                return $"seeders < {stop.Value:0}";
            case StopConditionType.LeechersBelow when _leechers is { } l && l < stop.Value:
                return $"leechers < {stop.Value:0}";
            case StopConditionType.UploadedAboveMb when _uploaded > (long)(stop.Value * 1024 * 1024):
                return $"uploaded > {stop.Value:0} MB";
            case StopConditionType.DownloadedAboveMb when _downloaded > (long)(stop.Value * 1024 * 1024):
                return $"downloaded > {stop.Value:0} MB";
            case StopConditionType.LeecherSeederRatioBelow when _seeders is { } sd and > 0 && _leechers is { } lc && lc / (double)sd < stop.Value:
                return $"leechers/seeders < {stop.Value}";
            default:
                return null;
        }
    }

    private async Task StopWithReasonAsync(string? reason, bool isError, CancellationToken cancellationToken, bool announceStopped = true)
    {
        lock (_gate)
        {
            if (_state is TorrentSessionState.Stopped or TorrentSessionState.Error)
            {
                return;
            }

            _shouldStop = true;
            _stopReason = reason;
        }

        SetState(TorrentSessionState.Stopping);
        await CloseListenerAsync().ConfigureAwait(false);
        if (announceStopped)
        {
            await SendStoppedAsync(cancellationToken).ConfigureAwait(false);
        }

        if (reason is not null)
        {
            Log(isError ? LogLevel.Error : LogLevel.Info, $"Stopped: {reason}");
        }

        SetState(isError ? TorrentSessionState.Error : TorrentSessionState.Stopped);
    }

    private async Task SendStoppedAsync(CancellationToken cancellationToken)
    {
        var values = BuildAnnounceValues();
        try
        {
            var outcome = await _tracker.AnnounceAsync(
                _profile, _descriptor.TrackerUrl, values, TrackerEvent.Stopped, _settings.Proxy, _settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
        }
        catch (Exception ex)
        {
            // The stopped announce is best effort: whatever went wrong, the session still ends.
            Log(LogLevel.Warning, $"Stopped announce failed: {ex.Message}");
        }
    }

    private void SetRunningState() => SetState(_seedMode ? TorrentSessionState.Seeding : TorrentSessionState.Downloading);

    private void SetState(TorrentSessionState state)
    {
        bool changed;
        lock (_gate)
        {
            changed = _state != state;
            _state = state;
        }

        if (changed)
        {
            StateChanged?.Invoke(this, state);
        }
    }

    private void OpenListener()
    {
        if (!_settings.UseTcpListener || !_settings.Proxy.IsDirect || _listener is not null)
        {
            return;
        }

        if (!int.TryParse(_identity.Port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
        {
            return;
        }

        var listener = new PeerListener(port, _descriptor.InfoHash, _identity.PeerId, message => Log(LogLevel.Info, message));
        if (listener.Start())
        {
            _listener = listener;
        }
    }

    private async Task CloseListenerAsync()
    {
        if (_listener is not null)
        {
            await _listener.DisposeAsync().ConfigureAwait(false);
            _listener = null;
        }
    }

    private double? ComputeRatio() =>
        _downloaded < RatioMinimumDownloadedBytes ? null : _uploaded / (double)_downloaded;

    private double ComputeFinishedPercent()
    {
        if (_descriptor.TotalLength <= 0)
        {
            return 100;
        }

        var percent = (_descriptor.TotalLength - _left) / (double)_descriptor.TotalLength * 100;
        return Math.Clamp(percent, 0, 100);
    }

    private TimeSpan? ComputeStopAfterRemaining()
    {
        if (_settings.Stop.Type != StopConditionType.AfterSeconds || _settings.Stop.Value <= 0)
        {
            return null;
        }

        var remaining = (int)_settings.Stop.Value - _totalRunningSeconds;
        return TimeSpan.FromSeconds(Math.Max(0, remaining));
    }

    private void Log(LogLevel level, string text)
    {
        if (_settings.EnableLog || level == LogLevel.Error)
        {
            LogEmitted?.Invoke(this, new LogEntry(_clock.Now, level, text));
        }
    }

    private static int ClampInterval(int seconds) => Math.Clamp(seconds, MinIntervalSeconds, MaxIntervalSeconds);

    private static string DescribeEvent(TrackerEvent trackerEvent) => trackerEvent switch
    {
        TrackerEvent.Started => "started event",
        TrackerEvent.Stopped => "stopped event",
        TrackerEvent.Completed => "completed event",
        _ => "update",
    };

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await CloseListenerAsync().ConfigureAwait(false);
    }
}
