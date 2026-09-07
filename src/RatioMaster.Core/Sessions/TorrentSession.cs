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
    /// <summary>After an announce that got no answer, try again this soon instead of waiting the full interval.</summary>
    internal const int RetryDelaySeconds = 60;
    private const int MinIntervalSeconds = 60;
    private const int MaxIntervalSeconds = 24 * 60 * 60;
    private const long RatioMinimumDownloadedBytes = 100 * 1024;

    private readonly TorrentDescriptor descriptor;
    private readonly ClientProfile profile;
    private readonly ClientIdentity identity;
    private readonly ITrackerClient tracker;
    private readonly ILocalIpProvider localIpProvider;
    private readonly IRandomSource random;
    private readonly ISystemClock clock;
    private readonly Lock gate = new();

    private TorrentSettings settings;
    private TorrentSessionState state = TorrentSessionState.Idle;
    private string? stopReason;

    private long uploaded;
    private long downloaded;
    private long left;
    private long totalSize;
    private bool seedMode;
    private bool haveInitialPeers;

    private long currentUploadRate;
    private long currentDownloadRate;
    private bool uploadRandomEnabled;

    private int currentInterval;
    private int elapsedInInterval;
    private int totalRunningSeconds;
    private bool forceUpdate;

    private int? seeders;
    private int? leechers;

    private PeerListener? listener;
    private CancellationTokenSource? loopCts;
    private Task? loop;
    private bool shouldStop;

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
        this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        this.localIpProvider = localIpProvider ?? LocalIpProvider.Instance;
        this.random = random ?? SystemRandomSource.Instance;
        this.clock = clock ?? SystemClock.Instance;
        this.currentUploadRate = settings.UploadRateBytes;
        this.currentDownloadRate = settings.DownloadRateBytes;
        this.uploadRandomEnabled = settings.UploadRandomEnabled;
        this.currentInterval = ClampInterval(settings.IntervalSeconds);
    }

    public event EventHandler<TorrentSessionState>? StateChanged;

    public event EventHandler<LogEntry>? LogEmitted;

    public event EventHandler<TrackerExchange>? TrackerExchangeCompleted;

    public event EventHandler<EngineAdjustment>? Adjusted;

    public TorrentDescriptor Descriptor => this.descriptor;

    public ClientProfile Profile => this.profile;

    public ClientIdentity Identity => this.identity;

    public TorrentSettings Settings => this.settings;

    public TorrentSessionState State => this.state;

    public bool IsListening => this.listener?.IsListening ?? false;

    public TorrentStats Snapshot
    {
        get
        {
            lock (this.gate)
            {
                return new TorrentStats
                {
                    State = this.state,
                    StopReason = this.stopReason,
                    Uploaded = this.uploaded,
                    Downloaded = this.downloaded,
                    Left = this.left,
                    TotalSize = this.totalSize,
                    Ratio = this.ComputeRatio(),
                    FinishedPercent = this.ComputeFinishedPercent(),
                    Seeders = this.seeders,
                    Leechers = this.leechers,
                    UploadRateBytes = this.UploadPaused ? 0 : this.currentUploadRate,
                    DownloadRateBytes = this.currentDownloadRate,
                    TotalRunningTime = TimeSpan.FromSeconds(this.totalRunningSeconds),
                    NextUpdateIn = this.state is TorrentSessionState.Idle or TorrentSessionState.Stopped or TorrentSessionState.Error
                        ? null
                        : TimeSpan.FromSeconds(Math.Max(0, this.currentInterval - this.elapsedInInterval)),
                    StopAfterRemaining = this.ComputeStopAfterRemaining(),
                };
            }
        }
    }

    /// <summary>
    /// True while the tracker's last swarm figures said nobody is downloading and the settings ask to
    /// stop uploading then. Read under <see cref="gate"/>. This gates the upload counter itself, so no
    /// other rule (a random speed on the next update, a live edit of the speed) can upload past it.
    /// </summary>
    private bool UploadPaused => this.settings.StopUploadWhenNoLeechers && this.leechers == 0;

    private string LocalIp { get; set; } = LocalIpProvider.Fallback;

    /// <summary>
    /// Starts the background run loop (started announce, per-second ticks, stopped announce on cancel).
    /// A session that stopped on its own (stop condition, tracker error) can be started again.
    /// </summary>
    public void Start()
    {
        if (this.loop is { IsCompleted: false })
        {
            return;
        }

        this.loopCts?.Dispose();
        var cts = new CancellationTokenSource();
        this.loopCts = cts;
        this.loop = Task.Run(() => this.RunAsync(cts.Token));
    }

    /// <summary>Stops the run loop and sends the stopped announce.</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (this.loop is null)
        {
            if (this.state is not (TorrentSessionState.Stopped or TorrentSessionState.Idle or TorrentSessionState.Error))
            {
                await this.StopWithReasonAsync(null, isError: false, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (this.loopCts is not null)
        {
            await this.loopCts.CancelAsync().ConfigureAwait(false);
        }

        try
        {
            await this.loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        this.loop = null;
        this.loopCts?.Dispose();
        this.loopCts = null;
    }

    /// <summary>Requests an immediate announce on the next tick (the old Manual Update button).</summary>
    public void RequestUpdate()
    {
        lock (this.gate)
        {
            this.forceUpdate = true;
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
        lock (this.gate)
        {
            pausedBefore = this.UploadPaused;
            this.settings = settings;
            this.currentUploadRate = settings.UploadRateBytes;
            this.currentDownloadRate = this.seedMode ? 0 : settings.DownloadRateBytes;
            this.uploadRandomEnabled = settings.UploadRandomEnabled;
            pausedNow = this.UploadPaused;
        }

        this.LogPauseChange(pausedBefore, pausedNow, "the pause on no leechers was switched off");
    }

    public async ValueTask DisposeAsync()
    {
        await this.StopAsync().ConfigureAwait(false);
        await this.CloseListenerAsync().ConfigureAwait(false);
    }

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (this.gate)
        {
            this.uploaded = 0;
            this.downloaded = 0;
            this.totalRunningSeconds = 0;
            this.elapsedInInterval = 0;
            this.haveInitialPeers = false;
            this.shouldStop = false;
            this.forceUpdate = false;
            this.seeders = null;
            this.leechers = null;
            this.stopReason = null;
            this.currentUploadRate = this.settings.UploadRateBytes;
            this.currentDownloadRate = this.settings.DownloadRateBytes;
            this.uploadRandomEnabled = this.settings.UploadRandomEnabled;
            this.currentInterval = ClampInterval(this.settings.IntervalSeconds);
            this.InitializeSizeFromFinishedPercent();
        }

        this.SetState(TorrentSessionState.Starting);
        if (TrackerUrl.Validate(this.descriptor.TrackerUrl) is { } problem)
        {
            // There is nothing to announce to, so no stopped announce either.
            await this.StopWithReasonAsync(problem, isError: true, cancellationToken, announceStopped: false).ConfigureAwait(false);
            return;
        }

        this.LocalIp = await this.localIpProvider.GetLocalIpAsync(cancellationToken).ConfigureAwait(false);
        this.OpenListener();

        var response = await this.DoAnnounceAsync(TrackerEvent.Started, cancellationToken).ConfigureAwait(false);
        if (await this.HandleFailureAsync(response, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await this.DoScrapeAsync(cancellationToken).ConfigureAwait(false);
        this.SetRunningState();
    }

    internal async Task TickAsync(CancellationToken cancellationToken)
    {
        if (this.state is TorrentSessionState.Stopped or TorrentSessionState.Error or TorrentSessionState.Idle)
        {
            return;
        }

        if (this.haveInitialPeers)
        {
            bool completed;
            lock (this.gate)
            {
                completed = this.ApplyCounterGrowth();
            }

            if (completed)
            {
                await this.DoAnnounceAsync(TrackerEvent.Completed, cancellationToken).ConfigureAwait(false);
                await this.DoScrapeAsync(cancellationToken).ConfigureAwait(false);
                this.SetState(TorrentSessionState.Seeding);
            }
        }

        bool announceNow;
        lock (this.gate)
        {
            this.totalRunningSeconds++;
            announceNow = this.forceUpdate || this.currentInterval - this.elapsedInInterval <= 0;
        }

        if (this.EvaluateStopConditions() is { } reason)
        {
            await this.StopWithReasonAsync(reason, isError: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!announceNow)
        {
            lock (this.gate)
            {
                this.elapsedInInterval++;
            }

            return;
        }

        lock (this.gate)
        {
            this.forceUpdate = false;
            this.elapsedInInterval = 0;
            this.ApplyNextUpdateRandomSpeeds();
        }

        this.OpenListener();
        var response = await this.DoAnnounceAsync(TrackerEvent.None, cancellationToken).ConfigureAwait(false);
        if (await this.HandleFailureAsync(response, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await this.DoScrapeAsync(cancellationToken).ConfigureAwait(false);
        this.SetRunningState();
    }

    private static int ClampInterval(int seconds) => Math.Clamp(seconds, MinIntervalSeconds, MaxIntervalSeconds);

    private static string DescribeEvent(TrackerEvent trackerEvent) => trackerEvent switch
    {
        TrackerEvent.Started => "started event",
        TrackerEvent.Stopped => "stopped event",
        TrackerEvent.Completed => "completed event",
        _ => "update",
    };

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this.StartAsync(cancellationToken).ConfigureAwait(false);
            if (this.state == TorrentSessionState.Error)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (!this.shouldStop && await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await this.TickAsync(cancellationToken).ConfigureAwait(false);
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
            this.Log(LogLevel.Error, $"Unexpected error: {ex.Message}");
            await this.StopWithReasonAsync($"unexpected error: {ex.Message}", isError: true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (this.state is not (TorrentSessionState.Stopped or TorrentSessionState.Error))
            {
                await this.StopWithReasonAsync(null, isError: false, CancellationToken.None).ConfigureAwait(false);
            }

            await this.CloseListenerAsync().ConfigureAwait(false);
        }
    }

    private void InitializeSizeFromFinishedPercent()
    {
        var finished = Math.Clamp(this.settings.FinishedPercent, 0, 100);
        if (finished <= 0)
        {
            this.totalSize = this.descriptor.TotalLength;
            this.seedMode = false;
        }
        else if (finished >= 100)
        {
            this.totalSize = 0;
            this.seedMode = true;
        }
        else
        {
            this.totalSize = (long)(this.descriptor.TotalLength * (100 - finished) / 100);
            this.seedMode = false;
        }

        this.left = this.totalSize;
        if (this.seedMode)
        {
            this.currentDownloadRate = 0;
        }
    }

    /// <summary>Grows the counters for one second. Returns true when the download has just completed.</summary>
    private bool ApplyCounterGrowth()
    {
        if (!this.UploadPaused)
        {
            var uploadedThisTick = this.currentUploadRate + this.Jitter(this.uploadRandomEnabled, this.settings.UploadRandomMinKb, this.settings.UploadRandomMaxKb);
            this.uploaded += Math.Max(0, uploadedThisTick);
        }

        if (!this.seedMode && this.currentDownloadRate > 0)
        {
            var downloadedThisTick = this.currentDownloadRate + this.Jitter(this.settings.DownloadRandomEnabled, this.settings.DownloadRandomMinKb, this.settings.DownloadRandomMaxKb);
            this.downloaded += Math.Max(0, downloadedThisTick);
            this.left = this.totalSize - this.downloaded;
        }

        if (this.left <= 0)
        {
            this.downloaded = this.totalSize;
            this.left = 0;
            this.currentDownloadRate = 0;
            if (!this.seedMode)
            {
                this.seedMode = true;
                return true;
            }
        }

        return false;
    }

    private long Jitter(bool enabled, int minKb, int maxKb)
    {
        if (!enabled)
        {
            return this.random.Next(10);
        }

        var lo = Math.Min(minKb, maxKb);
        var hi = Math.Max(minKb, maxKb);
        var kb = hi <= lo ? lo : this.random.Next(lo, hi);
        return (long)kb * 1024;
    }

    private void ApplyNextUpdateRandomSpeeds()
    {
        if (this.settings.NextUpdateRandomUpload)
        {
            this.currentUploadRate = this.RandomRateBytes(this.settings.NextUpdateUploadMinKb, this.settings.NextUpdateUploadMaxKb);
        }

        if (this.settings.NextUpdateRandomDownload && !this.seedMode)
        {
            this.currentDownloadRate = this.RandomRateBytes(this.settings.NextUpdateDownloadMinKb, this.settings.NextUpdateDownloadMaxKb);
        }
    }

    private long RandomRateBytes(int minKb, int maxKb)
    {
        var lo = Math.Min(minKb, maxKb);
        var hi = Math.Max(minKb, maxKb);
        var kb = hi <= lo ? lo : this.random.Next(lo, hi);
        return (long)kb * 1024;
    }

    private AnnounceValues BuildAnnounceValues()
    {
        lock (this.gate)
        {
            this.uploaded = AnnounceMath.RoundDown(this.uploaded, AnnounceMath.UploadedDenominator);
            this.downloaded = AnnounceMath.RoundDown(this.downloaded, AnnounceMath.DownloadedDenominator);
            if (this.left > 0)
            {
                this.left = this.totalSize - this.downloaded;
            }

            return new AnnounceValues
            {
                InfoHashEncoded = InfoHashEncoder.Encode(this.descriptor.InfoHash, this.profile.HashUpperCase),
                PeerId = this.identity.PeerId,
                Port = this.identity.Port,
                Uploaded = this.uploaded,
                Downloaded = this.downloaded,
                Left = this.left,
                Key = this.identity.Key,
                NumWant = this.identity.NumWant,
                LocalIp = this.LocalIp,
            };
        }
    }

    private async Task<AnnounceResponse?> DoAnnounceAsync(TrackerEvent trackerEvent, CancellationToken cancellationToken)
    {
        if (trackerEvent != TrackerEvent.Started && trackerEvent != TrackerEvent.Stopped)
        {
            this.SetState(TorrentSessionState.Updating);
        }

        var values = this.BuildAnnounceValues();
        this.Log(LogLevel.Info, $"Announcing {DescribeEvent(trackerEvent)} to tracker");
        try
        {
            var outcome = await this.tracker.AnnounceAsync(
                this.profile, this.descriptor.TrackerUrl, values, trackerEvent, this.settings.Proxy, this.settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            this.TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
            this.ApplyAnnounceResponse(outcome.Response);
            return outcome.Response;
        }
        catch (Exception ex) when (ex is TrackerException or IOException or ProxyException)
        {
            this.Log(LogLevel.Warning, $"No connection to tracker: {ex.Message}");
            this.ScheduleRetry();
            return null;
        }
    }

    /// <summary>Pulls the next announce forward so a tracker that did not answer is retried within a minute.</summary>
    private void ScheduleRetry()
    {
        lock (this.gate)
        {
            this.elapsedInInterval = Math.Max(this.elapsedInInterval, this.currentInterval - RetryDelaySeconds);
        }

        this.Log(LogLevel.Info, $"Will retry in {RetryDelaySeconds} seconds.");
    }

    private void ApplyAnnounceResponse(AnnounceResponse response)
    {
        if (response.HasFailure)
        {
            this.Log(LogLevel.Error, $"Tracker error: {response.FailureReason}");
            return;
        }

        foreach (var (key, value) in response.ExtraKeys)
        {
            this.Log(LogLevel.Info, $"{key}: {value}");
        }

        if (response.Interval is { } interval)
        {
            var clamped = ClampInterval(interval);
            if (clamped != this.currentInterval)
            {
                this.currentInterval = clamped;
                this.Adjusted?.Invoke(this, new EngineAdjustment { IntervalSeconds = clamped, Reason = "Tracker set the announce interval." });
            }
        }

        if (response.Complete is { } complete && response.Incomplete is { } incomplete)
        {
            this.UpdateSwarmStats(complete, incomplete);
        }

        this.haveInitialPeers = true;
        if (response.Peers.Count > 0)
        {
            this.Log(LogLevel.Info, $"peers: ({response.Peers.Count})");
        }
    }

    private async Task DoScrapeAsync(CancellationToken cancellationToken)
    {
        if (!this.settings.RequestScrape)
        {
            return;
        }

        var encoded = InfoHashEncoder.Encode(this.descriptor.InfoHash, this.profile.HashUpperCase);
        try
        {
            var outcome = await this.tracker.ScrapeAsync(
                this.profile, this.descriptor.TrackerUrl, encoded, this.descriptor.InfoHash, this.settings.Proxy, this.settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            this.TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
            if (outcome.Response is { HasFailure: false } scrape && scrape is { Complete: { } complete, Incomplete: { } incomplete })
            {
                this.Log(LogLevel.Info, $"scrape: complete={complete} incomplete={incomplete} downloaded={scrape.Downloaded}");
                this.UpdateSwarmStats(complete, incomplete);
            }
        }
        catch (Exception ex) when (ex is TrackerException or IOException or ProxyException)
        {
            this.Log(LogLevel.Warning, $"Scrape error: {ex.Message}");
        }
    }

    private void UpdateSwarmStats(int complete, int incomplete)
    {
        bool pausedBefore, pausedNow;
        lock (this.gate)
        {
            pausedBefore = this.UploadPaused;
            this.seeders = complete;
            this.leechers = incomplete;
            pausedNow = this.UploadPaused;
        }

        this.LogPauseChange(pausedBefore, pausedNow, $"the tracker reports {incomplete} leechers");
    }

    private void LogPauseChange(bool pausedBefore, bool pausedNow, string resumeReason)
    {
        if (pausedNow && !pausedBefore)
        {
            this.Log(LogLevel.Info, "The tracker reports no leechers; upload paused until it reports some.");
        }
        else if (!pausedNow && pausedBefore)
        {
            this.Log(LogLevel.Info, $"Upload resumed: {resumeReason}.");
        }
    }

    private async Task<bool> HandleFailureAsync(AnnounceResponse? response, CancellationToken cancellationToken)
    {
        if (response is { HasFailure: true } && !this.settings.IgnoreFailureReason)
        {
            await this.StopWithReasonAsync($"Tracker error: {response.FailureReason}", isError: true, cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private string? EvaluateStopConditions()
    {
        var stop = this.settings.Stop;
        switch (stop.Type)
        {
            case StopConditionType.AfterSeconds when stop.Value > 0 && this.totalRunningSeconds >= stop.Value:
                return $"ran for {stop.Value:0} seconds";
            case StopConditionType.SeedersBelow when this.seeders is { } s && s < stop.Value:
                return $"seeders < {stop.Value:0}";
            case StopConditionType.LeechersBelow when this.leechers is { } l && l < stop.Value:
                return $"leechers < {stop.Value:0}";
            case StopConditionType.UploadedAboveMb when this.uploaded > (long)(stop.Value * 1024 * 1024):
                return $"uploaded > {stop.Value:0} MB";
            case StopConditionType.DownloadedAboveMb when this.downloaded > (long)(stop.Value * 1024 * 1024):
                return $"downloaded > {stop.Value:0} MB";
            case StopConditionType.LeecherSeederRatioBelow when this.seeders is { } sd and > 0 && this.leechers is { } lc && lc / (double)sd < stop.Value:
                return $"leechers/seeders < {stop.Value}";
            default:
                return null;
        }
    }

    private async Task StopWithReasonAsync(string? reason, bool isError, CancellationToken cancellationToken, bool announceStopped = true)
    {
        lock (this.gate)
        {
            if (this.state is TorrentSessionState.Stopped or TorrentSessionState.Error)
            {
                return;
            }

            this.shouldStop = true;
            this.stopReason = reason;
        }

        this.SetState(TorrentSessionState.Stopping);
        await this.CloseListenerAsync().ConfigureAwait(false);
        if (announceStopped)
        {
            await this.SendStoppedAsync(cancellationToken).ConfigureAwait(false);
        }

        if (reason is not null)
        {
            this.Log(isError ? LogLevel.Error : LogLevel.Info, $"Stopped: {reason}");
        }

        this.SetState(isError ? TorrentSessionState.Error : TorrentSessionState.Stopped);
    }

    private async Task SendStoppedAsync(CancellationToken cancellationToken)
    {
        var values = this.BuildAnnounceValues();
        try
        {
            var outcome = await this.tracker.AnnounceAsync(
                this.profile, this.descriptor.TrackerUrl, values, TrackerEvent.Stopped, this.settings.Proxy, this.settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
            this.TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
        }
        catch (Exception ex)
        {
            // The stopped announce is best effort: whatever went wrong, the session still ends.
            this.Log(LogLevel.Warning, $"Stopped announce failed: {ex.Message}");
        }
    }

    private void SetRunningState() => this.SetState(this.seedMode ? TorrentSessionState.Seeding : TorrentSessionState.Downloading);

    private void SetState(TorrentSessionState state)
    {
        bool changed;
        lock (this.gate)
        {
            changed = this.state != state;
            this.state = state;
        }

        if (changed)
        {
            this.StateChanged?.Invoke(this, state);
        }
    }

    private void OpenListener()
    {
        if (!this.settings.UseTcpListener || !this.settings.Proxy.IsDirect || this.listener is not null)
        {
            return;
        }

        if (!int.TryParse(this.identity.Port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
        {
            return;
        }

        var listener = new PeerListener(port, this.descriptor.InfoHash, this.identity.PeerId, message => this.Log(LogLevel.Info, message));
        if (listener.Start())
        {
            this.listener = listener;
        }
    }

    private async Task CloseListenerAsync()
    {
        if (this.listener is not null)
        {
            await this.listener.DisposeAsync().ConfigureAwait(false);
            this.listener = null;
        }
    }

    private double? ComputeRatio() =>
        this.downloaded < RatioMinimumDownloadedBytes ? null : this.uploaded / (double)this.downloaded;

    private double ComputeFinishedPercent()
    {
        if (this.descriptor.TotalLength <= 0)
        {
            return 100;
        }

        var percent = (this.descriptor.TotalLength - this.left) / (double)this.descriptor.TotalLength * 100;
        return Math.Clamp(percent, 0, 100);
    }

    private TimeSpan? ComputeStopAfterRemaining()
    {
        if (this.settings.Stop.Type != StopConditionType.AfterSeconds || this.settings.Stop.Value <= 0)
        {
            return null;
        }

        var remaining = (int)this.settings.Stop.Value - this.totalRunningSeconds;
        return TimeSpan.FromSeconds(Math.Max(0, remaining));
    }

    private void Log(LogLevel level, string text)
    {
        if (this.settings.EnableLog || level == LogLevel.Error)
        {
            this.LogEmitted?.Invoke(this, new LogEntry(this.clock.Now, level, text));
        }
    }
}
