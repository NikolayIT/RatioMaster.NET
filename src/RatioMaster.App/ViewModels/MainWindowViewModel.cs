using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Converters;
using RatioMaster.App.Services;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.App.ViewModels.Dialogs;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;
using RatioMaster.Core.Settings;
using RatioMaster.Core.Torrents;
using RatioMaster.Core.Updates;

namespace RatioMaster.App.ViewModels;

/// <summary>How the torrent list is filtered.</summary>
public enum TorrentFilter
{
    All,
    Running,
    Stopped,
    Downloading,
    Seeding,
    Error,
}

/// <summary>The main window: the torrent list, all commands, the 1 Hz refresh and the status bar.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly TorrentSessionFactory _factory;
    private readonly ISettingsStore _settingsStore;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly IClipboardService _clipboard;
    private readonly IUrlLauncher _launcher;
    private readonly INotificationService _notifications;
    private readonly IUiDispatcher _dispatcher;
    private readonly ILocalIpProvider _localIp;
    private readonly UpdateChecker _updateChecker;
    private readonly DispatcherTimer _timer;

    private AppSettings _settings;
    private bool _suspendAutosave;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private TorrentItemViewModel? _selectedTorrent;

    [ObservableProperty]
    private TorrentFilter _filter = TorrentFilter.All;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isDetailsPaneVisible = true;

    [ObservableProperty]
    private string _localIpText = "…";

    [ObservableProperty]
    private string _statusSummary = string.Empty;

    [ObservableProperty]
    private string _aggregateSpeeds = string.Empty;

    [ObservableProperty]
    private string? _updateMessage;

    [ObservableProperty]
    private string _trayToolTip = "RatioMaster.NET";

    public MainWindowViewModel(
        TorrentSessionFactory factory,
        ISettingsStore settingsStore,
        IDialogService dialogs,
        IFileDialogService files,
        IClipboardService clipboard,
        IUrlLauncher launcher,
        INotificationService notifications,
        IUiDispatcher dispatcher,
        ILocalIpProvider localIp,
        UpdateChecker updateChecker)
    {
        _factory = factory;
        _settingsStore = settingsStore;
        _dialogs = dialogs;
        _files = files;
        _clipboard = clipboard;
        _launcher = launcher;
        _notifications = notifications;
        _dispatcher = dispatcher;
        _localIp = localIp;
        _updateChecker = updateChecker;
        _settings = settingsStore.Load();

        TorrentsView = new DataGridCollectionView(Torrents) { Filter = FilterTorrent };
        Torrents.CollectionChanged += OnTorrentsChanged;

        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Tick());
        UpdateStatusSummary();
    }

    public ObservableCollection<TorrentItemViewModel> Torrents { get; } = [];

    public DataGridCollectionView TorrentsView { get; }

    /// <summary>Rows selected in the grid; kept in step by the view.</summary>
    public List<TorrentItemViewModel> SelectedTorrents { get; } = [];

    public bool HasSelection => SelectedTorrent is not null;

    public bool HasTorrents => Torrents.Count > 0;

    public string VersionText => "v" + AppVersion.Public;

    public AppSettings Settings => _settings;

    /// <summary>Raised when the application should quit.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Raised when the window should be restored from the tray.</summary>
    public event EventHandler? RestoreRequested;

    public static IReadOnlyList<TorrentFilter> Filters { get; } = Enum.GetValues<TorrentFilter>();

    /// <summary>Loads settings, restores the last session and checks for updates.</summary>
    public async Task InitializeAsync()
    {
        LogTimeConverter.Use24HourTime = _settings.Use24HourTime;
        IsDetailsPaneVisible = true;
        _timer.Start();

        LocalIpText = await _localIp.GetLocalIpAsync();

        if (_settings.RestoreLastSessionOnStartup && File.Exists(AppPaths.LastSessionFile))
        {
            await RestoreSessionAsync(AppPaths.LastSessionFile, startImmediately: false, announce: false);
        }

        if (_settings.CheckForUpdatesOnStartup)
        {
            _ = CheckForUpdatesAsync(quiet: true);
        }
    }

    private void Tick()
    {
        long up = 0, down = 0;
        var running = 0;
        foreach (var torrent in Torrents)
        {
            torrent.Refresh();
            if (torrent.IsRunning)
            {
                running++;
                up += torrent.UploadRate;
                down += torrent.DownloadRate;
            }
        }

        AggregateSpeeds = $"↑ {Formatting.Speed(up)}   ↓ {Formatting.Speed(down)}";
        StatusSummary = $"Torrents: {Torrents.Count} ({running} running)";
        TrayToolTip = BuildTrayToolTip(running, up, down);
    }

    private string BuildTrayToolTip(int running, long up, long down)
    {
        var header = $"RatioMaster.NET — {running} running   ↑ {Formatting.Speed(up)} ↓ {Formatting.Speed(down)}";
        if (!_settings.ShowTorrentListInTrayTooltip || Torrents.Count == 0)
        {
            return header;
        }

        var lines = Torrents.Take(10).Select(t => $"{t.DisplayName} — {t.StatusText}");
        return header + Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private void UpdateStatusSummary()
    {
        StatusSummary = $"Torrents: {Torrents.Count} (0 running)";
        AggregateSpeeds = "↑ 0 kB/s   ↓ 0 kB/s";
    }

    private void OnTorrentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (var i = 0; i < Torrents.Count; i++)
        {
            Torrents[i].Index = i + 1;
        }

        OnPropertyChanged(nameof(HasTorrents));
        TorrentsView.Refresh();
        SaveLastSession();
    }

    private bool FilterTorrent(object item)
    {
        if (item is not TorrentItemViewModel torrent)
        {
            return false;
        }

        var matchesFilter = Filter switch
        {
            TorrentFilter.Running => torrent.IsRunning,
            TorrentFilter.Stopped => !torrent.IsRunning,
            TorrentFilter.Downloading => torrent.State == TorrentSessionState.Downloading,
            TorrentFilter.Seeding => torrent.State == TorrentSessionState.Seeding,
            TorrentFilter.Error => torrent.State == TorrentSessionState.Error,
            _ => true,
        };

        if (!matchesFilter)
        {
            return false;
        }

        return SearchText.Length == 0
            || torrent.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || torrent.TrackerUrl.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnFilterChanged(TorrentFilter value) => TorrentsView.Refresh();

    partial void OnSearchTextChanged(string value) => TorrentsView.Refresh();

    /// <summary>Called by the view when the grid selection changes.</summary>
    public void SetSelection(IEnumerable<TorrentItemViewModel> selection)
    {
        SelectedTorrents.Clear();
        SelectedTorrents.AddRange(selection);
        OnPropertyChanged(nameof(HasSelection));
    }

    private IReadOnlyList<TorrentItemViewModel> Targets =>
        SelectedTorrents.Count > 0 ? SelectedTorrents : SelectedTorrent is { } one ? [one] : [];

    // ---- Adding torrents -------------------------------------------------

    [RelayCommand]
    private async Task AddTorrentAsync()
    {
        var paths = await _files.PickTorrentFilesAsync(_settings.LastTorrentDirectory);
        if (paths.Count > 0)
        {
            await AddTorrentsAsync(paths, forceDialog: false);
        }
    }

    /// <summary>Adds torrents from paths, showing the Add dialog unless the user turned it off.</summary>
    public async Task AddTorrentsAsync(IReadOnlyList<string> paths, bool forceDialog)
    {
        if (paths.Count == 0)
        {
            return;
        }

        RememberTorrentDirectory(paths[0]);

        if (_settings.AddTorrentsWithoutDialog && !forceDialog)
        {
            AddDirectly(paths);
            return;
        }

        var editor = new TorrentSettingsViewModel(_factory.Catalog, _factory, _settings.DefaultTorrentSettings);
        var dialog = new AddTorrentViewModel(editor, _files, _settings.DefaultTorrentSettings, _settings.StartTorrentsImmediately);
        dialog.AddFiles(paths);

        var requests = await _dialogs.ShowAsync(dialog);
        if (requests is null)
        {
            return;
        }

        foreach (var request in requests)
        {
            var item = CreateItem(request.Descriptor, request.Settings, request.DisplayName);
            Torrents.Add(item);
            if (request.StartImmediately)
            {
                item.Start();
            }
        }

        SelectedTorrent ??= Torrents.LastOrDefault();
    }

    private void AddDirectly(IReadOnlyList<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                var file = TorrentFile.Load(path);
                var item = CreateItem(TorrentDescriptor.FromFile(file), _settings.DefaultTorrentSettings, file.Name);
                Torrents.Add(item);
                if (_settings.StartTorrentsImmediately)
                {
                    item.Start();
                }
            }
            catch (TorrentFormatException ex)
            {
                _notifications.ShowWarning("Could not add torrent", $"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        SelectedTorrent ??= Torrents.LastOrDefault();
    }

    private TorrentItemViewModel CreateItem(TorrentDescriptor descriptor, TorrentSettings settings, string displayName)
    {
        var item = new TorrentItemViewModel(descriptor, settings, displayName, _factory, _dispatcher, _clipboard, _launcher, _files);
        item.RemoveRequested += async (s, _) => await RemoveAsync((TorrentItemViewModel)s!);
        return item;
    }

    private void RememberTorrentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && directory != _settings.LastTorrentDirectory)
        {
            _settings = _settings with { LastTorrentDirectory = directory };
            _settingsStore.Save(_settings);
        }
    }

    /// <summary>Handles files dropped on the window: torrents are added, sessions are offered for loading.</summary>
    public async Task HandleDroppedFilesAsync(IReadOnlyList<string> paths)
    {
        var torrents = paths.Where(p => p.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)).ToList();
        var sessions = paths.Where(p => p.EndsWith(".session", StringComparison.OrdinalIgnoreCase)).ToList();

        if (torrents.Count > 0)
        {
            await AddTorrentsAsync(torrents, forceDialog: false);
        }

        foreach (var session in sessions)
        {
            var choice = await _dialogs.ChooseAsync(
                "Load session",
                $"Load the session {Path.GetFileName(session)}?",
                [
                    new DialogButton("Load", "load", IsDefault: true),
                    new DialogButton("Load and start", "start"),
                    new DialogButton("Cancel", "cancel", IsCancel: true),
                ]);

            if (choice is "load" or "start")
            {
                await RestoreSessionAsync(session, startImmediately: choice == "start", announce: true);
            }
        }
    }

    // ---- Per-torrent commands -------------------------------------------

    [RelayCommand]
    private void StartSelected()
    {
        foreach (var torrent in Targets.Where(t => t.CanStart))
        {
            torrent.Start();
        }
    }

    [RelayCommand]
    private async Task StopSelectedAsync()
    {
        foreach (var torrent in Targets.Where(t => t.IsRunning))
        {
            await torrent.StopAsync();
        }
    }

    [RelayCommand]
    private void UpdateSelected()
    {
        foreach (var torrent in Targets.Where(t => t.IsRunning))
        {
            torrent.UpdateNow();
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var targets = Targets.ToList();
        if (targets.Count == 0 || !await ConfirmRemovalAsync(targets))
        {
            return;
        }

        foreach (var torrent in targets)
        {
            await RemoveWithoutAskingAsync(torrent);
        }
    }

    /// <summary>Removes one torrent after asking; used by the row's own Remove request.</summary>
    private async Task RemoveAsync(TorrentItemViewModel torrent)
    {
        if (await ConfirmRemovalAsync([torrent]))
        {
            await RemoveWithoutAskingAsync(torrent);
        }
    }

    /// <summary>One question for the whole selection, mentioning how many torrents will be stopped first.</summary>
    private Task<bool> ConfirmRemovalAsync(IReadOnlyList<TorrentItemViewModel> targets)
    {
        var running = targets.Count(t => t.IsRunning);
        string message;
        if (targets.Count == 1)
        {
            message = $"Remove {targets[0].DisplayName} from the list?";
            if (running > 0)
            {
                message += " It is running and will be stopped first.";
            }
        }
        else
        {
            message = $"Remove these {targets.Count} torrents from the list?";
            message += running switch
            {
                0 => string.Empty,
                1 => " One of them is running and will be stopped first.",
                _ => $" {running} of them are running and will be stopped first.",
            };
        }

        return _dialogs.ConfirmAsync(targets.Count == 1 ? "Remove torrent" : "Remove torrents", message, "Remove");
    }

    private async Task RemoveWithoutAskingAsync(TorrentItemViewModel torrent)
    {
        await torrent.StopAsync();
        await torrent.DisposeSessionAsync();
        Torrents.Remove(torrent);
        if (ReferenceEquals(SelectedTorrent, torrent))
        {
            SelectedTorrent = Torrents.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task RenameSelectedAsync()
    {
        if (SelectedTorrent is not { } torrent)
        {
            return;
        }

        var name = await _dialogs.PromptAsync("Rename torrent", "Name shown in the list:", torrent.DisplayName);
        if (!string.IsNullOrWhiteSpace(name))
        {
            torrent.DisplayName = name;
            SaveLastSession();
        }
    }

    [RelayCommand]
    private async Task CopyInfoHashAsync()
    {
        if (SelectedTorrent is { } torrent)
        {
            await _clipboard.SetTextAsync(torrent.InfoHashHex);
        }
    }

    [RelayCommand]
    private void UseSelectedAsDefaults()
    {
        if (SelectedTorrent is not { } torrent)
        {
            return;
        }

        _settings = _settings with { DefaultTorrentSettings = torrent.CurrentSettings };
        _settingsStore.Save(_settings);
        _notifications.ShowInformation("Defaults updated", "New torrents will start from these settings.");
    }

    // ---- Bulk commands ---------------------------------------------------

    [RelayCommand]
    private void StartAll()
    {
        foreach (var torrent in Torrents.Where(t => t.CanStart))
        {
            torrent.Start();
        }
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        foreach (var torrent in Torrents.Where(t => t.IsRunning))
        {
            await torrent.StopAsync();
        }
    }

    [RelayCommand]
    private void UpdateAll()
    {
        foreach (var torrent in Torrents.Where(t => t.IsRunning))
        {
            torrent.UpdateNow();
        }
    }

    [RelayCommand]
    private void ClearAllLogs()
    {
        foreach (var torrent in Torrents)
        {
            torrent.Log.ClearCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task SetUploadSpeedForAllAsync() => await SetSpeedForAllAsync(upload: true);

    [RelayCommand]
    private async Task SetDownloadSpeedForAllAsync() => await SetSpeedForAllAsync(upload: false);

    private async Task SetSpeedForAllAsync(bool upload)
    {
        var what = upload ? "upload" : "download";
        var answer = await _dialogs.PromptAsync($"Set {what} speed", $"New {what} speed for every torrent (kB/s):", "100");
        if (answer is null || !double.TryParse(answer, NumberStyles.Float, CultureInfo.CurrentCulture, out var kb) || kb < 0)
        {
            return;
        }

        foreach (var torrent in Torrents)
        {
            torrent.SetSpeeds(upload ? kb : null, upload ? null : kb);
        }

        SaveLastSession();
    }

    // ---- Sessions --------------------------------------------------------

    [RelayCommand]
    private async Task LoadSessionAsync() => await LoadSessionAsync(startImmediately: false);

    [RelayCommand]
    private async Task LoadSessionAndStartAsync() => await LoadSessionAsync(startImmediately: true);

    private async Task LoadSessionAsync(bool startImmediately)
    {
        var path = await _files.PickSessionFileAsync(_settings.LastSessionDirectory);
        if (path is not null)
        {
            await RestoreSessionAsync(path, startImmediately, announce: true);
        }
    }

    [RelayCommand]
    private async Task SaveSessionAsync() => await SaveSessionAsync(stopFirst: false);

    [RelayCommand]
    private async Task StopAllAndSaveSessionAsync() => await SaveSessionAsync(stopFirst: true);

    private async Task SaveSessionAsync(bool stopFirst)
    {
        if (stopFirst)
        {
            await StopAllAsync();
        }

        var path = await _files.SaveSessionFileAsync("ratiomaster.session", _settings.LastSessionDirectory);
        if (path is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            _settings = _settings with { LastSessionDirectory = directory };
            _settingsStore.Save(_settings);
        }

        SessionFile.Save(path, BuildSessionDocument());
        _notifications.ShowInformation("Session saved", Path.GetFileName(path));
    }

    private SessionDocument BuildSessionDocument() => new()
    {
        Torrents = Torrents.Select(t => t.ToSessionEntry()).ToList(),
    };

    private async Task RestoreSessionAsync(string path, bool startImmediately, bool announce)
    {
        SessionDocument document;
        try
        {
            document = SessionFile.Load(path);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Xml.XmlException or System.Text.Json.JsonException)
        {
            _notifications.ShowWarning("Could not load session", ex.Message);
            return;
        }

        _suspendAutosave = true;
        var missing = 0;
        try
        {
            foreach (var entry in document.Torrents)
            {
                if (entry.TorrentPath is not { Length: > 0 } torrentPath || !File.Exists(torrentPath))
                {
                    missing++;
                    continue;
                }

                try
                {
                    var file = TorrentFile.Load(torrentPath);
                    var descriptor = TorrentDescriptor.FromFile(file, entry.TrackerUrl ?? file.Announce);
                    var name = string.IsNullOrWhiteSpace(entry.Name) ? file.Name : entry.Name;
                    var item = CreateItem(descriptor, entry.Settings, name);
                    Torrents.Add(item);
                    if (startImmediately)
                    {
                        item.Start();
                    }
                }
                catch (TorrentFormatException)
                {
                    missing++;
                }
            }
        }
        finally
        {
            _suspendAutosave = false;
        }

        SaveLastSession();
        SelectedTorrent ??= Torrents.FirstOrDefault();

        if (announce)
        {
            var loaded = document.Torrents.Count - missing;
            var message = missing == 0 ? $"{loaded} torrents loaded." : $"{loaded} torrents loaded, {missing} could not be found.";
            _notifications.ShowInformation("Session loaded", message);
        }
    }

    private void SaveLastSession()
    {
        if (_suspendAutosave)
        {
            return;
        }

        try
        {
            AppPaths.EnsureConfigDirectory();
            SessionFile.Save(AppPaths.LastSessionFile, BuildSessionDocument());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Autosave is best effort.
        }
    }

    // ---- Settings, help, updates ----------------------------------------

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var editor = new TorrentSettingsViewModel(_factory.Catalog, _factory, _settings.DefaultTorrentSettings);
        var dialog = new SettingsViewModel(_settings, editor, _factory.Catalog);
        var saved = await _dialogs.ShowAsync(dialog);
        if (saved is not null)
        {
            _settings = saved;
            _settingsStore.Save(_settings);
            LogTimeConverter.Use24HourTime = _settings.Use24HourTime;
            ThemeApplier.Apply(_settings.Theme);
        }
    }

    [RelayCommand]
    private async Task ShowAboutAsync() => await _dialogs.ShowAsync(new AboutViewModel(_launcher) { Title = "About RatioMaster.NET" });

    [RelayCommand]
    private async Task OpenUrlAsync(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            await _launcher.OpenUrlAsync(url);
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync() => await CheckForUpdatesAsync(quiet: false);

    private async Task CheckForUpdatesAsync(bool quiet)
    {
        var result = await _updateChecker.CheckAsync();
        if (!result.Succeeded)
        {
            UpdateMessage = null;
            if (!quiet)
            {
                await _dialogs.ShowMessageAsync("Check for updates", "Could not reach ratiomaster.net: " + result.Error);
            }

            return;
        }

        if (!result.UpdateAvailable)
        {
            UpdateMessage = null;
            if (!quiet)
            {
                await _dialogs.ShowMessageAsync("Check for updates", "You are running the latest version.");
            }

            return;
        }

        if (quiet && result.RemoteVersion == _settings.SkippedVersion)
        {
            return;
        }

        UpdateMessage = "Update available";
        var choice = await _dialogs.ShowAsync(new NewVersionViewModel(result.RemoteVersion!, _launcher));
        if (choice == NewVersionChoice.Skip)
        {
            _settings = _settings with { SkippedVersion = result.RemoteVersion };
            _settingsStore.Save(_settings);
        }
    }

    [RelayCommand]
    private void ToggleDetailsPane() => IsDetailsPaneVisible = !IsDetailsPaneVisible;

    [RelayCommand]
    private void Restore() => RestoreRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Stops every torrent (sending the stopped announce) and persists state. Called when quitting.</summary>
    public async Task ShutdownAsync(TimeSpan timeout)
    {
        _timer.Stop();
        SaveLastSession();
        _settingsStore.Save(_settings);

        using var cts = new CancellationTokenSource(timeout);
        var stops = Torrents.Select(async t =>
        {
            try
            {
                await t.StopAsync();
                await t.DisposeSessionAsync();
            }
            catch (Exception)
            {
                // Never block quitting on a stubborn tracker.
            }
        });

        await Task.WhenAll(stops).WaitAsync(cts.Token).ConfigureAwait(false);
    }

    /// <summary>Replaces the settings (used after the settings dialog and by the window for placement).</summary>
    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(_settings);
    }
}
