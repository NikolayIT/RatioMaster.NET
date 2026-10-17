namespace RatioMaster.App.ViewModels
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using RatioMaster.App.Converters;
    using RatioMaster.App.Resources;
    using RatioMaster.App.Services.Abstractions;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Sessions;

    /// <summary>
    /// One row in the torrent list. Owns the engine session for that torrent, marshals its events onto the
    /// UI thread and exposes the columns and detail values.
    /// </summary>
    public sealed partial class TorrentItemViewModel : ViewModelBase
    {
        private readonly TorrentSessionFactory factory;
        private readonly IUiDispatcher dispatcher;
        private readonly IClipboardService clipboard;
        private readonly IUrlLauncher launcher;

        private TorrentSession? session;
        private TorrentSettings settings;

        /// <summary>The resume point this run last wrote into the settings, and whether the session file has it yet.</summary>
        private double? appliedResumePercent;
        private bool resumePointUnsaved;

        [ObservableProperty]
        private int index;

        [ObservableProperty]
        private string displayName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        [NotifyCanExecuteChangedFor(nameof(StartCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
        private TorrentSessionState state = TorrentSessionState.Idle;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private string? stopReason;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private long uploaded;

        [ObservableProperty]
        private long downloaded;

        [ObservableProperty]
        private long left;

        [ObservableProperty]
        private double? ratio;

        [ObservableProperty]
        private long uploadRate;

        [ObservableProperty]
        private long downloadRate;

        [ObservableProperty]
        private int? seeders;

        [ObservableProperty]
        private int? leechers;

        [ObservableProperty]
        private TimeSpan? nextUpdateIn;

        [ObservableProperty]
        private TimeSpan? stopAfterRemaining;

        [ObservableProperty]
        private TimeSpan totalRunningTime;

        [ObservableProperty]
        private string trackerUrl;

        [ObservableProperty]
        private string peerIdInUse = string.Empty;

        [ObservableProperty]
        private string keyInUse = string.Empty;

        [ObservableProperty]
        private string portInUse = string.Empty;

        [ObservableProperty]
        private string numWantInUse = string.Empty;

        public TorrentItemViewModel(
            TorrentDescriptor descriptor,
            TorrentSettings settings,
            string displayName,
            TorrentSessionFactory factory,
            IUiDispatcher dispatcher,
            IClipboardService clipboard,
            IUrlLauncher launcher,
            IFileDialogService files)
        {
            this.Descriptor = descriptor;
            this.settings = settings;
            this.displayName = displayName;
            this.factory = factory;
            this.dispatcher = dispatcher;
            this.clipboard = clipboard;
            this.launcher = launcher;
            this.trackerUrl = descriptor.TrackerUrl;
            this.progress = Math.Clamp(settings.FinishedPercent, 0, 100);
            this.AddedAt = DateTimeOffset.Now;

            this.Log = new LogViewModel(displayName, files, clipboard);
            this.Tracker = new TrackerHistoryViewModel();
            this.Settings = new TorrentSettingsViewModel(factory.Catalog, factory, settings);
        }

        /// <summary>Raised when the row wants to be removed from the list.</summary>
        public event EventHandler? RemoveRequested;

        /// <summary>Raised when the settings in effect changed, so the session autosave can pick them up.</summary>
        public event EventHandler? SettingsChanged;

        public TorrentDescriptor Descriptor { get; }

        public LogViewModel Log { get; }

        public TrackerHistoryViewModel Tracker { get; }

        public TorrentSettingsViewModel Settings { get; }

        public DateTimeOffset AddedAt { get; }

        public string InfoHashHex => Convert.ToHexString(this.Descriptor.InfoHash);

        public string TorrentPath => this.Descriptor.FilePath ?? Formatting.Unknown;

        public long TotalSize => this.Descriptor.TotalLength;

        public string ClientName => this.settings.ClientName;

        public string TrackerHost => Formatting.Host(this.TrackerUrl);

        public bool IsRunning => this.State is TorrentSessionState.Starting or TorrentSessionState.Downloading
            or TorrentSessionState.Seeding or TorrentSessionState.Updating;

        public bool CanStart => !this.IsRunning && this.State != TorrentSessionState.Stopping;

        public string StatusText => this.State switch
        {
            TorrentSessionState.Idle => Strings.StateIdle,
            TorrentSessionState.Starting => Strings.StateStarting,
            TorrentSessionState.Downloading => Strings.StateDownloading,
            TorrentSessionState.Seeding => Strings.StateSeeding,
            TorrentSessionState.Updating => Strings.StateUpdating,
            TorrentSessionState.Stopping => Strings.StateStopping,
            TorrentSessionState.Error => this.StopReason is { Length: > 0 } e
                ? string.Format(CultureInfo.CurrentCulture, Strings.StateErrorWithReason, e)
                : Strings.StateError,
            _ => this.StopReason is { Length: > 0 } r
                ? string.Format(CultureInfo.CurrentCulture, Strings.StateStoppedWithReason, r)
                : Strings.StateStopped,
        };

        public string StopConditionText
        {
            get
            {
                var value = this.settings.Stop.Value;
                var whole = value.ToString("0", CultureInfo.CurrentCulture);
                return this.settings.Stop.Type switch
                {
                    StopConditionType.Never => Strings.StopSummaryNever,
                    StopConditionType.AfterSeconds => string.Format(CultureInfo.CurrentCulture, Strings.StopSummaryAfterSeconds, whole),
                    StopConditionType.SeedersBelow => string.Format(CultureInfo.CurrentCulture, Strings.StopSummarySeedersBelow, whole),
                    StopConditionType.LeechersBelow => string.Format(CultureInfo.CurrentCulture, Strings.StopSummaryLeechersBelow, whole),
                    StopConditionType.UploadedAboveMb => string.Format(CultureInfo.CurrentCulture, Strings.StopSummaryUploadedAbove, whole),
                    StopConditionType.DownloadedAboveMb => string.Format(CultureInfo.CurrentCulture, Strings.StopSummaryDownloadedAbove, whole),
                    _ => string.Format(CultureInfo.CurrentCulture, Strings.StopSummaryRatioBelow, value.ToString("0.###", CultureInfo.CurrentCulture)),
                };
            }
        }

        public string ProxyText => this.settings.Proxy.Type == Core.Networking.ProxyType.None
            ? DisplayNames.Of(this.settings.Proxy.Type)
            : $"{DisplayNames.Of(this.settings.Proxy.Type)} {this.settings.Proxy.Host}:{this.settings.Proxy.Port}";

        /// <summary>Gets the settings currently in effect (what a Start would use).</summary>
        public TorrentSettings CurrentSettings => this.settings;

        public string FormattedIdentity => string.Create(CultureInfo.InvariantCulture, $"{this.PeerIdInUse} / {this.KeyInUse} / port {this.PortInUse}");

        /// <summary>Called every second on the UI thread by the main view model.</summary>
        public void Refresh()
        {
            this.Log.Drain();
            this.Tracker.Drain();

            if (this.session is null)
            {
                return;
            }

            var stats = this.session.Snapshot;
            this.State = stats.State;
            this.StopReason = stats.StopReason;
            this.Progress = stats.FinishedPercent;
            this.Uploaded = stats.Uploaded;
            this.Downloaded = stats.Downloaded;
            this.Left = stats.Left;
            this.Ratio = stats.Ratio;
            this.UploadRate = stats.UploadRateBytes;
            this.DownloadRate = stats.DownloadRateBytes;
            this.Seeders = stats.Seeders;
            this.Leechers = stats.Leechers;
            this.NextUpdateIn = stats.NextUpdateIn;
            this.StopAfterRemaining = stats.StopAfterRemaining;
            this.TotalRunningTime = stats.TotalRunningTime;
            this.Settings.IsRunning = this.IsRunning;
            this.KeepResumePoint(stats);
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        public void Start()
        {
            // A session that stopped on its own (a stop condition, a tracker error) since the last refresh may have got
            // further than that refresh saw, up to complete: carry on from where it really got to.
            if (this.session is { } previous)
            {
                this.KeepResumePoint(previous.Snapshot);
            }

            this.ApplySettings();
            this.appliedResumePercent = null;
            this.session = this.CreateSession();
            this.session.Start();
            this.State = TorrentSessionState.Starting;
        }

        [RelayCommand(CanExecute = nameof(IsRunning))]
        public async Task StopAsync()
        {
            if (this.session is not null)
            {
                await this.session.StopAsync();
                this.Refresh();
            }
        }

        [RelayCommand(CanExecute = nameof(IsRunning))]
        public void UpdateNow() => this.session?.RequestUpdate();

        [RelayCommand]
        public async Task CopyInfoHashAsync() => await this.clipboard.SetTextAsync(this.InfoHashHex);

        [RelayCommand]
        public async Task CopyTrackerUrlAsync() => await this.clipboard.SetTextAsync(this.TrackerUrl);

        [RelayCommand]
        public async Task OpenContainingFolderAsync()
        {
            if (this.Descriptor.FilePath is { Length: > 0 } path)
            {
                await this.launcher.RevealFileAsync(path);
            }
        }

        [RelayCommand]
        public void Remove() => this.RemoveRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>Pushes the editor's values into the settings, and into a running session where allowed.</summary>
        [RelayCommand]
        public void ApplySettings()
        {
            this.settings = this.Settings.ToSettings();
            this.session?.UpdateLiveSettings(this.settings);
            this.OnPropertyChanged(nameof(this.ClientName));
            this.OnPropertyChanged(nameof(this.StopConditionText));
            this.OnPropertyChanged(nameof(this.ProxyText));
            this.SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Discards editor changes and reloads from the settings in effect.</summary>
        [RelayCommand]
        public void RevertSettings() => this.Settings.LoadFrom(this.settings);

        /// <summary>Replaces the settings wholesale (used by "set speed for all").</summary>
        public void ReplaceSettings(TorrentSettings settings)
        {
            this.settings = settings;
            this.Settings.LoadFrom(settings);
            this.session?.UpdateLiveSettings(settings);
            this.OnPropertyChanged(nameof(this.ClientName));
            this.OnPropertyChanged(nameof(this.StopConditionText));
            this.OnPropertyChanged(nameof(this.ProxyText));
        }

        public SessionEntry ToSessionEntry() => new()
        {
            Name = this.DisplayName,
            TorrentPath = this.Descriptor.FilePath,
            TrackerUrl = this.TrackerUrl,
            Settings = this.settings,
        };

        public async ValueTask DisposeSessionAsync()
        {
            if (this.session is not null)
            {
                await this.session.DisposeAsync();
                this.session = null;
            }
        }

        /// <summary>Used by the "set upload/download speed for all" prompts.</summary>
        public void SetSpeeds(double? uploadKb, double? downloadKb)
        {
            var settings = this.settings;
            if (uploadKb is { } up)
            {
                settings = settings with { UploadRateBytes = (long)(up * 1024) };
                this.Settings.UploadKb = up;
            }

            if (downloadKb is { } down)
            {
                settings = settings with { DownloadRateBytes = (long)(down * 1024) };
                this.Settings.DownloadKb = down;
            }

            this.settings = settings;
            this.session?.UpdateLiveSettings(settings);
        }

        private TorrentSession CreateSession()
        {
            var descriptor = this.Descriptor with { TrackerUrl = this.TrackerUrl };
            var session = this.factory.Create(descriptor, this.settings, message => this.Log.Enqueue(
                new Core.Logging.LogEntry(DateTimeOffset.Now, Core.Logging.LogLevel.Info, message)));

            session.LogEmitted += (_, entry) => this.Log.Enqueue(entry);
            session.TrackerExchangeCompleted += (_, exchange) => this.Tracker.Enqueue(exchange);
            session.StateChanged += (_, state) => this.dispatcher.Post(() =>
            {
                this.State = state;
                this.StopReason = session.Snapshot.StopReason;
            });
            session.Adjusted += (_, adjustment) => this.dispatcher.Post(() => this.ApplyEngineAdjustment(adjustment));

            this.PeerIdInUse = session.Identity.PeerId;
            this.KeyInUse = session.Identity.Key;
            this.PortInUse = session.Identity.Port;
            this.NumWantInUse = session.Identity.NumWant;
            return session;
        }

        /// <summary>
        /// Moves Finished (%) along with the download, so the next start carries on from the progress reached, as a
        /// real client does and 0.43 did: a completed torrent comes back as a seed instead of downloading it all again.
        /// Only a change this run made is written, so a value typed in after it stopped is kept. The session file gets
        /// it when the download completes or the torrent stops; until then it is in memory, which quitting saves.
        /// </summary>
        private void KeepResumePoint(TorrentStats stats)
        {
            if (stats.ResumeFinishedPercent is { } reached && reached != this.appliedResumePercent)
            {
                this.appliedResumePercent = reached;
                this.settings = this.settings with { FinishedPercent = reached };
                this.Settings.FinishedPercent = reached;
                this.resumePointUnsaved = true;
            }

            if (this.resumePointUnsaved && (this.settings.FinishedPercent >= 100 || !stats.IsRunning))
            {
                this.resumePointUnsaved = false;
                this.SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyEngineAdjustment(EngineAdjustment adjustment)
        {
            if (adjustment.IntervalSeconds is { } interval)
            {
                this.settings = this.settings with { IntervalSeconds = interval };
                this.Settings.IntervalSeconds = interval;
            }

            this.SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        partial void OnDisplayNameChanged(string value) => OnPropertyChanged(nameof(Log));

        partial void OnTrackerUrlChanged(string value) => OnPropertyChanged(nameof(TrackerHost));
    }
}
