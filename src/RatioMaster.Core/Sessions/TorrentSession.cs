namespace RatioMaster.Core.Sessions
{
    using System.Globalization;

    using RatioMaster.Core.Abstractions;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Logging;
    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Resources;
    using RatioMaster.Core.Tracker;

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
        private readonly ITrackerClient tracker;
        private readonly ILocalIpProvider localIpProvider;
        private readonly IRandomSource random;
        private readonly ISystemClock clock;
        private readonly PeerListenerPool listeners;
        private readonly Lock gate = new();

        private ClientIdentity identity;
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

        /// <summary>
        /// Whether the tracker accepted the started and the completed announce. Until it has, the next update carries
        /// the event again, as libtorrent does (start_sent, complete_sent): one that got no answer, or was refused, is
        /// otherwise lost, and with it the tracker's record that the torrent was started or completed.
        /// </summary>
        private bool startedSent;
        private bool completedSent;

        /// <summary>When the current key was generated, for the clients that roll theirs while running.</summary>
        private int keyGeneratedAtSecond;

        private PeerListenerRegistration? listener;
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
            ISystemClock? clock = null,
            PeerListenerPool? listeners = null)
        {
            this.descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            this.localIpProvider = localIpProvider ?? LocalIpProvider.Instance;
            this.random = random ?? SystemRandomSource.Instance;
            this.clock = clock ?? SystemClock.Instance;
            this.listeners = listeners ?? PeerListenerPool.Shared;
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

        public bool IsListening => this.listener is not null;

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
                        ResumeFinishedPercent = this.ComputeResumeFinishedPercent(),
                        Seeders = this.seeders,
                        Leechers = this.leechers,
                        UploadRateBytes = this.Counting && !this.UploadPaused ? this.currentUploadRate : 0,
                        DownloadRateBytes = this.Counting ? this.currentDownloadRate : 0,
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
        /// Gets a value indicating whether the tracker's last swarm figures said nobody is downloading and the settings ask to
        /// stop uploading then. Read under <see cref="gate"/>. This gates the upload counter itself, so no
        /// other rule (a random speed on the next update, a live edit of the speed) can upload past it.
        /// </summary>
        private bool UploadPaused => this.settings.StopUploadWhenNoLeechers && this.leechers == 0;

        /// <summary>
        /// Gets a value indicating whether the counters grow each second: from the tracker's first answer until the session
        /// stops. Read under <see cref="gate"/>. The speeds in the snapshot are the ones the counters grow at, so a stopped
        /// torrent, or one whose tracker has not answered yet, shows none.
        /// </summary>
        private bool Counting => this.haveInitialPeers
            && this.state is TorrentSessionState.Downloading or TorrentSessionState.Seeding or TorrentSessionState.Updating;

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

            this.LogPauseChange(pausedBefore, pausedNow, Strings.ResumeReasonPauseSwitchedOff);
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
                this.keyGeneratedAtSecond = 0;
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

                // A torrent that starts complete was completed before, so it never sends completed, as in libtorrent.
                this.startedSent = false;
                this.completedSent = this.seedMode;
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
                    // The completed announce starts the next interval, as in a real client and in 0.43; otherwise the
                    // update that was due would follow it within seconds, which a tracker can refuse as too early.
                    lock (this.gate)
                    {
                        this.elapsedInInterval = 0;
                        this.forceUpdate = false;
                    }

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

        private static string AnnouncingText(TrackerEvent trackerEvent) => trackerEvent switch
        {
            TrackerEvent.Started => Strings.LogAnnouncingStarted,
            TrackerEvent.Stopped => Strings.LogAnnouncingStopped,
            TrackerEvent.Completed => Strings.LogAnnouncingCompleted,
            _ => Strings.LogAnnouncingUpdate,
        };

        /// <summary>Formats a stop-condition threshold the way the user typed it: whole numbers without decimals.</summary>
        private static string Threshold(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

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
                this.Log(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, Strings.LogUnexpectedError, ex.Message));
                await this.StopWithReasonAsync(string.Format(CultureInfo.CurrentCulture, Strings.StopReasonUnexpectedError, ex.Message), isError: true, CancellationToken.None).ConfigureAwait(false);
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

        /// <summary>
        /// Rolls the tracker key when the emulated client would have. uTorrent and BitTorrent make up a new one
        /// every ten minutes, so a session that announced the same key for hours would not look like them.
        /// Only a generated identity rotates: a key read out of the real client's memory, or typed by the user,
        /// is theirs to keep. The caller holds the gate, so this reports back rather than logging itself.
        /// </summary>
        private bool RefreshKeyIfDue()
        {
            if (this.profile.KeyRefresh != KeyRefreshPolicy.Timed
                || this.identity.Source != ClientIdentitySource.Generated)
            {
                return false;
            }

            var lifetimeSeconds = Math.Max(1, this.profile.KeyRefreshMinutes) * 60;
            if (this.totalRunningSeconds - this.keyGeneratedAtSecond < lifetimeSeconds)
            {
                return false;
            }

            this.keyGeneratedAtSecond = this.totalRunningSeconds;
            this.identity = this.identity with { Key = new ClientIdentityGenerator(this.random).GenerateValue(this.profile.Key) };
            return true;
        }

        /// <summary>
        /// Gets the numwant for the next announce: the identity's, unless that is still the client's own default and
        /// the client asks for fewer peers once it seeds (<see cref="ClientProfile.SeedingNumWant"/>). Call it inside the gate.
        /// </summary>
        private string CurrentNumWant() =>
            this.left == 0
            && this.profile.SeedingNumWant is { } seeding
            && this.identity.NumWant == this.profile.DefaultNumWant.ToString(CultureInfo.InvariantCulture)
                ? seeding.ToString(CultureInfo.InvariantCulture)
                : this.identity.NumWant;

        private AnnounceValues BuildAnnounceValues()
        {
            bool keyRolled;
            AnnounceValues values;
            lock (this.gate)
            {
                keyRolled = this.RefreshKeyIfDue();

                this.uploaded = AnnounceMath.RoundDown(this.uploaded, AnnounceMath.UploadedDenominator);
                this.downloaded = AnnounceMath.RoundDown(this.downloaded, AnnounceMath.DownloadedDenominator);
                if (this.left > 0)
                {
                    this.left = this.totalSize - this.downloaded;
                }

                values = new AnnounceValues
                {
                    InfoHashEncoded = InfoHashEncoder.Encode(this.descriptor.InfoHash, this.profile.HashUpperCase, this.profile.Unreserved),
                    PeerId = this.identity.PeerId,
                    Port = this.identity.Port,
                    Uploaded = this.uploaded,
                    Downloaded = this.downloaded,
                    Left = this.left,
                    Key = this.identity.Key,
                    NumWant = this.CurrentNumWant(),
                    LocalIp = this.LocalIp,
                };
            }

            // Outside the gate: the log event runs whatever a subscriber hooked up to it.
            if (keyRolled)
            {
                this.Log(LogLevel.Info, Strings.LogNewTrackerKey);
            }

            return values;
        }

        private async Task<AnnounceResponse?> DoAnnounceAsync(TrackerEvent trackerEvent, CancellationToken cancellationToken)
        {
            if (trackerEvent != TrackerEvent.Started && trackerEvent != TrackerEvent.Stopped)
            {
                this.SetState(TorrentSessionState.Updating);
            }

            TrackerEvent sending;
            lock (this.gate)
            {
                sending = this.WithPendingEvent(trackerEvent);
            }

            var values = this.BuildAnnounceValues();
            this.Log(LogLevel.Info, AnnouncingText(sending));
            try
            {
                var outcome = await this.tracker.AnnounceAsync(
                    this.profile, this.descriptor.TrackerUrl, values, sending, this.settings.Proxy, this.settings.IgnoreCertificateErrors, cancellationToken).ConfigureAwait(false);
                this.TrackerExchangeCompleted?.Invoke(this, outcome.Exchange);
                this.ApplyAnnounceResponse(outcome.Response);
                if (!outcome.Response.HasFailure)
                {
                    this.MarkSent(sending);
                }

                return outcome.Response;
            }
            catch (Exception ex) when (ex is TrackerException or IOException or ProxyException)
            {
                this.Log(LogLevel.Warning, string.Format(CultureInfo.CurrentCulture, Strings.LogNoConnection, ex.Message));
                this.ScheduleRetry();
                return null;
            }
        }

        /// <summary>
        /// Gets the event an announce carries: the one asked for, or, on an update, the started or completed the tracker
        /// has not accepted yet. Call it inside the gate.
        /// </summary>
        private TrackerEvent WithPendingEvent(TrackerEvent requested) => requested switch
        {
            TrackerEvent.None when !this.startedSent => TrackerEvent.Started,
            TrackerEvent.None when !this.completedSent && this.left == 0 => TrackerEvent.Completed,
            _ => requested,
        };

        private void MarkSent(TrackerEvent sent)
        {
            lock (this.gate)
            {
                if (sent == TrackerEvent.Started)
                {
                    this.startedSent = true;
                }
                else if (sent == TrackerEvent.Completed)
                {
                    this.completedSent = true;
                }
            }
        }

        /// <summary>Pulls the next announce forward so a tracker that did not answer is retried within a minute.</summary>
        private void ScheduleRetry()
        {
            lock (this.gate)
            {
                this.elapsedInInterval = Math.Max(this.elapsedInInterval, this.currentInterval - RetryDelaySeconds);
            }

            this.Log(LogLevel.Info, string.Format(CultureInfo.CurrentCulture, Strings.LogWillRetry, RetryDelaySeconds));
        }

        private void ApplyAnnounceResponse(AnnounceResponse response)
        {
            if (response.HasFailure)
            {
                this.Log(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, Strings.LogTrackerError, response.FailureReason));
                return;
            }

            // Not an error, so the torrent keeps running, but it can be the reason the tracker did not count an
            // announce (NexusPHP answers one that came too early with a warning and drops it).
            if (response.WarningMessage is { Length: > 0 } warning)
            {
                this.Log(LogLevel.Warning, string.Format(CultureInfo.CurrentCulture, Strings.LogTrackerWarning, warning));
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
                this.Log(LogLevel.Info, string.Format(CultureInfo.CurrentCulture, Strings.LogPeers, response.Peers.Count));
            }
        }

        private async Task DoScrapeAsync(CancellationToken cancellationToken)
        {
            if (!this.settings.RequestScrape)
            {
                return;
            }

            var encoded = InfoHashEncoder.Encode(this.descriptor.InfoHash, this.profile.HashUpperCase, this.profile.Unreserved);
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
                this.Log(LogLevel.Warning, string.Format(CultureInfo.CurrentCulture, Strings.LogScrapeError, ex.Message));
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

            this.LogPauseChange(pausedBefore, pausedNow, string.Format(CultureInfo.CurrentCulture, Strings.ResumeReasonLeechers, incomplete));
        }

        private void LogPauseChange(bool pausedBefore, bool pausedNow, string resumeReason)
        {
            if (pausedNow && !pausedBefore)
            {
                this.Log(LogLevel.Info, Strings.LogUploadPausedNoLeechers);
            }
            else if (!pausedNow && pausedBefore)
            {
                this.Log(LogLevel.Info, string.Format(CultureInfo.CurrentCulture, Strings.LogUploadResumed, resumeReason));
            }
        }

        private async Task<bool> HandleFailureAsync(AnnounceResponse? response, CancellationToken cancellationToken)
        {
            if (response is { HasFailure: true } && !this.settings.IgnoreFailureReason)
            {
                await this.StopWithReasonAsync(string.Format(CultureInfo.CurrentCulture, Strings.LogTrackerError, response.FailureReason), isError: true, cancellationToken).ConfigureAwait(false);
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
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonRanFor, Threshold(stop.Value));
                case StopConditionType.SeedersBelow when this.seeders is { } s && s < stop.Value:
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonSeedersBelow, Threshold(stop.Value));
                case StopConditionType.LeechersBelow when this.leechers is { } l && l < stop.Value:
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonLeechersBelow, Threshold(stop.Value));
                case StopConditionType.UploadedAboveMb when this.uploaded > (long)(stop.Value * 1024 * 1024):
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonUploadedAbove, Threshold(stop.Value));
                case StopConditionType.DownloadedAboveMb when this.downloaded > (long)(stop.Value * 1024 * 1024):
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonDownloadedAbove, Threshold(stop.Value));
                case StopConditionType.LeecherSeederRatioBelow when this.seeders is { } sd and > 0 && this.leechers is { } lc && lc / (double)sd < stop.Value:
                    return string.Format(CultureInfo.CurrentCulture, Strings.StopReasonRatioBelow, Threshold(stop.Value, "0.###"));
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
                this.Log(isError ? LogLevel.Error : LogLevel.Info, string.Format(CultureInfo.CurrentCulture, Strings.LogStopped, reason));
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
                this.Log(LogLevel.Warning, string.Format(CultureInfo.CurrentCulture, Strings.LogStoppedAnnounceFailed, ex.Message));
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

            // Shared with every other torrent announced with this port, as a real client's one listening port is.
            this.listener = this.listeners.Join(port, this.descriptor.InfoHash, this.identity.PeerId, message => this.Log(LogLevel.Info, message));
        }

        private async Task CloseListenerAsync()
        {
            if (this.listener is { } listener)
            {
                this.listener = null;
                await listener.DisposeAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Counted in whole bytes rather than from <see cref="ComputeFinishedPercent"/>: rounding the double down would
        /// sometimes lose a hundredth (0.29 is 0.28999... in binary).
        /// </summary>
        private double? ComputeResumeFinishedPercent()
        {
            if (this.downloaded <= 0)
            {
                return null;
            }

            var total = this.descriptor.TotalLength;
            if (this.left <= 0 || total <= 0)
            {
                return 100;
            }

            var hundredths = (long)((Int128)(total - this.left) * 10_000 / total);
            return hundredths / 100d;
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
}
