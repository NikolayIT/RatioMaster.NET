using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Converters;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Sessions;

namespace RatioMaster.App.ViewModels;

/// <summary>
/// One row in the torrent list. Owns the engine session for that torrent, marshals its events onto the
/// UI thread and exposes the columns and detail values.
/// </summary>
public sealed partial class TorrentItemViewModel : ViewModelBase
{
    private readonly TorrentSessionFactory _factory;
    private readonly IUiDispatcher _dispatcher;
    private readonly IClipboardService _clipboard;
    private readonly IUrlLauncher _launcher;

    private TorrentSession? _session;
    private TorrentSettings _settings;

    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateNowCommand))]
    private TorrentSessionState _state = TorrentSessionState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string? _stopReason;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private long _uploaded;

    [ObservableProperty]
    private long _downloaded;

    [ObservableProperty]
    private long _left;

    [ObservableProperty]
    private double? _ratio;

    [ObservableProperty]
    private long _uploadRate;

    [ObservableProperty]
    private long _downloadRate;

    [ObservableProperty]
    private int? _seeders;

    [ObservableProperty]
    private int? _leechers;

    [ObservableProperty]
    private TimeSpan? _nextUpdateIn;

    [ObservableProperty]
    private TimeSpan? _stopAfterRemaining;

    [ObservableProperty]
    private TimeSpan _totalRunningTime;

    [ObservableProperty]
    private string _trackerUrl;

    [ObservableProperty]
    private string _peerIdInUse = string.Empty;

    [ObservableProperty]
    private string _keyInUse = string.Empty;

    [ObservableProperty]
    private string _portInUse = string.Empty;

    [ObservableProperty]
    private string _numWantInUse = string.Empty;

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
        Descriptor = descriptor;
        _settings = settings;
        _displayName = displayName;
        _factory = factory;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        _launcher = launcher;
        _trackerUrl = descriptor.TrackerUrl;
        AddedAt = DateTimeOffset.Now;

        Log = new LogViewModel(displayName, files, clipboard);
        Tracker = new TrackerHistoryViewModel();
        Settings = new TorrentSettingsViewModel(factory.Catalog, factory, settings);
    }

    public TorrentDescriptor Descriptor { get; }

    public LogViewModel Log { get; }

    public TrackerHistoryViewModel Tracker { get; }

    public TorrentSettingsViewModel Settings { get; }

    public DateTimeOffset AddedAt { get; }

    public string InfoHashHex => Convert.ToHexString(Descriptor.InfoHash);

    public string TorrentPath => Descriptor.FilePath ?? Formatting.Unknown;

    public long TotalSize => Descriptor.TotalLength;

    public string ClientName => _settings.ClientName;

    public string TrackerHost => Formatting.Host(TrackerUrl);

    public bool IsRunning => State is TorrentSessionState.Starting or TorrentSessionState.Downloading
        or TorrentSessionState.Seeding or TorrentSessionState.Updating;

    public bool CanStart => !IsRunning && State != TorrentSessionState.Stopping;

    public string StatusText => State switch
    {
        TorrentSessionState.Idle => "Idle",
        TorrentSessionState.Starting => "Starting",
        TorrentSessionState.Downloading => "Downloading",
        TorrentSessionState.Seeding => "Seeding",
        TorrentSessionState.Updating => "Updating",
        TorrentSessionState.Stopping => "Stopping",
        TorrentSessionState.Error => StopReason is { Length: > 0 } e ? "Error: " + e : "Error",
        _ => StopReason is { Length: > 0 } r ? "Stopped: " + r : "Stopped",
    };

    public string StopConditionText => _settings.Stop.Type switch
    {
        StopConditionType.Never => "Never",
        StopConditionType.AfterSeconds => $"After {_settings.Stop.Value:0} s",
        StopConditionType.SeedersBelow => $"Seeders < {_settings.Stop.Value:0}",
        StopConditionType.LeechersBelow => $"Leechers < {_settings.Stop.Value:0}",
        StopConditionType.UploadedAboveMb => $"Uploaded > {_settings.Stop.Value:0} MB",
        StopConditionType.DownloadedAboveMb => $"Downloaded > {_settings.Stop.Value:0} MB",
        _ => $"Leechers/seeders < {_settings.Stop.Value}",
    };

    public string ProxyText => _settings.Proxy.Type == Core.Networking.ProxyType.None
        ? "None"
        : $"{_settings.Proxy.Type} {_settings.Proxy.Host}:{_settings.Proxy.Port}";

    /// <summary>The settings currently in effect (what a Start would use).</summary>
    public TorrentSettings CurrentSettings => _settings;

    /// <summary>Raised when the row wants to be removed from the list.</summary>
    public event EventHandler? RemoveRequested;

    /// <summary>Raised when the settings in effect changed, so the session autosave can pick them up.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>Called every second on the UI thread by the main view model.</summary>
    public void Refresh()
    {
        Log.Drain();
        Tracker.Drain();

        if (_session is null)
        {
            return;
        }

        var stats = _session.Snapshot;
        State = stats.State;
        StopReason = stats.StopReason;
        Progress = stats.FinishedPercent;
        Uploaded = stats.Uploaded;
        Downloaded = stats.Downloaded;
        Left = stats.Left;
        Ratio = stats.Ratio;
        UploadRate = stats.UploadRateBytes;
        DownloadRate = stats.DownloadRateBytes;
        Seeders = stats.Seeders;
        Leechers = stats.Leechers;
        NextUpdateIn = stats.NextUpdateIn;
        StopAfterRemaining = stats.StopAfterRemaining;
        TotalRunningTime = stats.TotalRunningTime;
        Settings.IsRunning = IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    public void Start()
    {
        ApplySettings();
        _session = CreateSession();
        _session.Start();
        State = TorrentSessionState.Starting;
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    public async Task StopAsync()
    {
        if (_session is not null)
        {
            await _session.StopAsync();
            Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    public void UpdateNow() => _session?.RequestUpdate();

    [RelayCommand]
    public async Task CopyInfoHashAsync() => await _clipboard.SetTextAsync(InfoHashHex);

    [RelayCommand]
    public async Task CopyTrackerUrlAsync() => await _clipboard.SetTextAsync(TrackerUrl);

    [RelayCommand]
    public async Task OpenContainingFolderAsync()
    {
        if (Descriptor.FilePath is { Length: > 0 } path)
        {
            await _launcher.RevealFileAsync(path);
        }
    }

    [RelayCommand]
    public void Remove() => RemoveRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Pushes the editor's values into the settings, and into a running session where allowed.</summary>
    [RelayCommand]
    public void ApplySettings()
    {
        _settings = Settings.ToSettings();
        _session?.UpdateLiveSettings(_settings);
        OnPropertyChanged(nameof(ClientName));
        OnPropertyChanged(nameof(StopConditionText));
        OnPropertyChanged(nameof(ProxyText));
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Discards editor changes and reloads from the settings in effect.</summary>
    [RelayCommand]
    public void RevertSettings() => Settings.LoadFrom(_settings);

    /// <summary>Replaces the settings wholesale (used by "set speed for all").</summary>
    public void ReplaceSettings(TorrentSettings settings)
    {
        _settings = settings;
        Settings.LoadFrom(settings);
        _session?.UpdateLiveSettings(settings);
        OnPropertyChanged(nameof(ClientName));
        OnPropertyChanged(nameof(StopConditionText));
        OnPropertyChanged(nameof(ProxyText));
    }

    public SessionEntry ToSessionEntry() => new()
    {
        Name = DisplayName,
        TorrentPath = Descriptor.FilePath,
        TrackerUrl = TrackerUrl,
        Settings = _settings,
    };

    public async ValueTask DisposeSessionAsync()
    {
        if (_session is not null)
        {
            await _session.DisposeAsync();
            _session = null;
        }
    }

    private TorrentSession CreateSession()
    {
        var descriptor = Descriptor with { TrackerUrl = TrackerUrl };
        var session = _factory.Create(descriptor, _settings, message => Log.Enqueue(
            new Core.Logging.LogEntry(DateTimeOffset.Now, Core.Logging.LogLevel.Info, message)));

        session.LogEmitted += (_, entry) => Log.Enqueue(entry);
        session.TrackerExchangeCompleted += (_, exchange) => Tracker.Enqueue(exchange);
        session.StateChanged += (_, state) => _dispatcher.Post(() =>
        {
            State = state;
            StopReason = session.Snapshot.StopReason;
        });
        session.Adjusted += (_, adjustment) => _dispatcher.Post(() => ApplyEngineAdjustment(adjustment));

        PeerIdInUse = session.Identity.PeerId;
        KeyInUse = session.Identity.Key;
        PortInUse = session.Identity.Port;
        NumWantInUse = session.Identity.NumWant;
        return session;
    }

    private void ApplyEngineAdjustment(EngineAdjustment adjustment)
    {
        if (adjustment.IntervalSeconds is { } interval)
        {
            _settings = _settings with { IntervalSeconds = interval };
            Settings.IntervalSeconds = interval;
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnDisplayNameChanged(string value) => OnPropertyChanged(nameof(Log));

    partial void OnTrackerUrlChanged(string value) => OnPropertyChanged(nameof(TrackerHost));

    /// <summary>Used by the "set upload/download speed for all" prompts.</summary>
    public void SetSpeeds(double? uploadKb, double? downloadKb)
    {
        var settings = _settings;
        if (uploadKb is { } up)
        {
            settings = settings with { UploadRateBytes = (long)(up * 1024) };
            Settings.UploadKb = up;
        }

        if (downloadKb is { } down)
        {
            settings = settings with { DownloadRateBytes = (long)(down * 1024) };
            Settings.DownloadKb = down;
        }

        _settings = settings;
        _session?.UpdateLiveSettings(settings);
    }

    public string FormattedIdentity => string.Create(CultureInfo.InvariantCulture, $"{PeerIdInUse} / {KeyInUse} / port {PortInUse}");
}
