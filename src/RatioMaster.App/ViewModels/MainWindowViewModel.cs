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
    private readonly TorrentSessionFactory factory;
    private readonly ISettingsStore settingsStore;
    private readonly IDialogService dialogs;
    private readonly IFileDialogService files;
    private readonly IClipboardService clipboard;
    private readonly IUrlLauncher launcher;
    private readonly INotificationService notifications;
    private readonly IUiDispatcher dispatcher;
    private readonly ILocalIpProvider localIp;
    private readonly UpdateChecker updateChecker;
    private readonly DispatcherTimer timer;

    private AppSettings settings;
    private bool suspendAutosave;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private TorrentItemViewModel? selectedTorrent;

    [ObservableProperty]
    private TorrentFilter filter = TorrentFilter.All;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isDetailsPaneVisible = true;

    [ObservableProperty]
    private string localIpText = "…";

    [ObservableProperty]
    private string statusSummary = string.Empty;

    [ObservableProperty]
    private string aggregateSpeeds = string.Empty;

    [ObservableProperty]
    private string? updateMessage;

    [ObservableProperty]
    private string trayToolTip = "RatioMaster.NET";

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
        this.factory = factory;
        this.settingsStore = settingsStore;
        this.dialogs = dialogs;
        this.files = files;
        this.clipboard = clipboard;
        this.launcher = launcher;
        this.notifications = notifications;
        this.dispatcher = dispatcher;
        this.localIp = localIp;
        this.updateChecker = updateChecker;
        this.settings = settingsStore.Load();

        this.TorrentsView = new DataGridCollectionView(this.Torrents) { Filter = this.FilterTorrent };
        this.Torrents.CollectionChanged += this.OnTorrentsChanged;

        this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => this.Tick());
        this.UpdateStatusSummary();
    }

    /// <summary>Raised when the application should quit.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Raised when the window should be restored from the tray.</summary>
    public event EventHandler? RestoreRequested;

    public static IReadOnlyList<TorrentFilter> Filters { get; } = Enum.GetValues<TorrentFilter>();

    public ObservableCollection<TorrentItemViewModel> Torrents { get; } = [];

    public DataGridCollectionView TorrentsView { get; }

    /// <summary>Rows selected in the grid; kept in step by the view.</summary>
    public List<TorrentItemViewModel> SelectedTorrents { get; } = [];

    public bool HasSelection => this.SelectedTorrent is not null;

    public bool HasTorrents => this.Torrents.Count > 0;

    public string VersionText => "v" + AppVersion.Public;

    public AppSettings Settings => this.settings;

    private IReadOnlyList<TorrentItemViewModel> Targets =>
        this.SelectedTorrents.Count > 0 ? this.SelectedTorrents : this.SelectedTorrent is { } one ? [one] : [];

    /// <summary>Loads settings, restores the last session and checks for updates.</summary>
    public async Task InitializeAsync()
    {
        LogTimeConverter.Use24HourTime = this.settings.Use24HourTime;
        this.IsDetailsPaneVisible = true;
        this.timer.Start();

        this.LocalIpText = await this.localIp.GetLocalIpAsync();

        if (this.settings.RestoreLastSessionOnStartup && File.Exists(AppPaths.LastSessionFile))
        {
            await this.RestoreSessionAsync(AppPaths.LastSessionFile, startImmediately: false, announce: false);
        }

        if (this.settings.CheckForUpdatesOnStartup)
        {
            _ = this.CheckForUpdatesAsync(quiet: true);
        }
    }

    /// <summary>Called by the view when the grid selection changes.</summary>
    public void SetSelection(IEnumerable<TorrentItemViewModel> selection)
    {
        this.SelectedTorrents.Clear();
        this.SelectedTorrents.AddRange(selection);
        this.OnPropertyChanged(nameof(this.HasSelection));
    }

    /// <summary>Adds torrents from paths, showing the Add dialog unless the user turned it off.</summary>
    public async Task AddTorrentsAsync(IReadOnlyList<string> paths, bool forceDialog)
    {
        if (paths.Count == 0)
        {
            return;
        }

        this.RememberTorrentDirectory(paths[0]);

        if (this.settings.AddTorrentsWithoutDialog && !forceDialog)
        {
            this.AddDirectly(paths);
            return;
        }

        var editor = new TorrentSettingsViewModel(this.factory.Catalog, this.factory, this.settings.DefaultTorrentSettings);
        var dialog = new AddTorrentViewModel(editor, this.files, this.settings.DefaultTorrentSettings, this.settings.StartTorrentsImmediately);
        dialog.AddFiles(paths);

        var requests = await this.dialogs.ShowAsync(dialog);
        if (requests is null)
        {
            return;
        }

        foreach (var request in requests)
        {
            var item = this.CreateItem(request.Descriptor, request.Settings, request.DisplayName);
            this.Torrents.Add(item);
            if (request.StartImmediately)
            {
                item.Start();
            }
        }

        this.SelectedTorrent ??= this.Torrents.LastOrDefault();
    }

    /// <summary>Handles files dropped on the window: torrents are added, sessions are offered for loading.</summary>
    public async Task HandleDroppedFilesAsync(IReadOnlyList<string> paths)
    {
        var torrents = paths.Where(p => p.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)).ToList();
        var sessions = paths.Where(p => p.EndsWith(".session", StringComparison.OrdinalIgnoreCase)).ToList();

        if (torrents.Count > 0)
        {
            await this.AddTorrentsAsync(torrents, forceDialog: false);
        }

        foreach (var session in sessions)
        {
            var choice = await this.dialogs.ChooseAsync(
                "Load session",
                $"Load the session {Path.GetFileName(session)}?",
                [
                    new DialogButton("Load", "load", IsDefault: true),
                    new DialogButton("Load and start", "start"),
                    new DialogButton("Cancel", "cancel", IsCancel: true),
                ]);

            if (choice is "load" or "start")
            {
                await this.RestoreSessionAsync(session, startImmediately: choice == "start", announce: true);
            }
        }
    }

    /// <summary>Stops every torrent (sending the stopped announce) and persists state. Called when quitting.</summary>
    public async Task ShutdownAsync(TimeSpan timeout)
    {
        this.timer.Stop();
        this.SaveLastSession();
        this.settingsStore.Save(this.settings);

        using var cts = new CancellationTokenSource(timeout);
        var stops = this.Torrents.Select(async t =>
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
        this.settings = settings;
        this.settingsStore.Save(this.settings);
    }

    /// <summary>
    /// Stops torrents side by side rather than one after another: each stop waits for its stopped announce,
    /// which can take a minute on a tracker that does not answer.
    /// </summary>
    private static Task StopAllOfAsync(IEnumerable<TorrentItemViewModel> torrents) =>
        Task.WhenAll(torrents.Where(t => t.IsRunning).Select(t => t.StopAsync()));

    private void Tick()
    {
        long up = 0, down = 0;
        var running = 0;
        foreach (var torrent in this.Torrents)
        {
            torrent.Refresh();
            if (torrent.IsRunning)
            {
                running++;
                up += torrent.UploadRate;
                down += torrent.DownloadRate;
            }
        }

        this.AggregateSpeeds = $"↑ {Formatting.Speed(up)}   ↓ {Formatting.Speed(down)}";
        this.StatusSummary = $"Torrents: {this.Torrents.Count} ({running} running)";
        this.TrayToolTip = this.BuildTrayToolTip(running, up, down);
    }

    private string BuildTrayToolTip(int running, long up, long down)
    {
        var header = $"RatioMaster.NET — {running} running   ↑ {Formatting.Speed(up)} ↓ {Formatting.Speed(down)}";
        if (!this.settings.ShowTorrentListInTrayTooltip || this.Torrents.Count == 0)
        {
            return header;
        }

        var lines = this.Torrents.Take(10).Select(t => $"{t.DisplayName} — {t.StatusText}");
        return header + Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private void UpdateStatusSummary()
    {
        this.StatusSummary = $"Torrents: {this.Torrents.Count} (0 running)";
        this.AggregateSpeeds = "↑ 0 kB/s   ↓ 0 kB/s";
    }

    private void OnTorrentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        for (var i = 0; i < this.Torrents.Count; i++)
        {
            this.Torrents[i].Index = i + 1;
        }

        this.OnPropertyChanged(nameof(this.HasTorrents));
        this.TorrentsView.Refresh();
        this.SaveLastSession();
    }

    private bool FilterTorrent(object item)
    {
        if (item is not TorrentItemViewModel torrent)
        {
            return false;
        }

        var matchesFilter = this.Filter switch
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

        return this.SearchText.Length == 0
            || torrent.DisplayName.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase)
            || torrent.TrackerUrl.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnFilterChanged(TorrentFilter value) => TorrentsView.Refresh();

    partial void OnSearchTextChanged(string value) => TorrentsView.Refresh();

    // ---- Adding torrents -------------------------------------------------

    [RelayCommand]
    private async Task AddTorrentAsync()
    {
        var paths = await this.files.PickTorrentFilesAsync(this.settings.LastTorrentDirectory);
        if (paths.Count > 0)
        {
            await this.AddTorrentsAsync(paths, forceDialog: false);
        }
    }

    private void AddDirectly(IReadOnlyList<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                var file = TorrentFile.Load(path);
                var item = this.CreateItem(TorrentDescriptor.FromFile(file), this.settings.DefaultTorrentSettings, file.Name);
                this.Torrents.Add(item);
                if (this.settings.StartTorrentsImmediately)
                {
                    item.Start();
                }
            }
            catch (TorrentFormatException ex)
            {
                this.notifications.ShowWarning("Could not add torrent", $"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        this.SelectedTorrent ??= this.Torrents.LastOrDefault();
    }

    private TorrentItemViewModel CreateItem(TorrentDescriptor descriptor, TorrentSettings settings, string displayName)
    {
        var item = new TorrentItemViewModel(descriptor, settings, displayName, this.factory, this.dispatcher, this.clipboard, this.launcher, this.files);
        item.RemoveRequested += async (s, _) => await this.RemoveAsync((TorrentItemViewModel)s!);
        item.SettingsChanged += (_, _) => this.SaveLastSession();
        return item;
    }

    private void RememberTorrentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && directory != this.settings.LastTorrentDirectory)
        {
            this.settings = this.settings with { LastTorrentDirectory = directory };
            this.settingsStore.Save(this.settings);
        }
    }

    // ---- Per-torrent commands -------------------------------------------

    [RelayCommand]
    private void StartSelected()
    {
        foreach (var torrent in this.Targets.Where(t => t.CanStart))
        {
            torrent.Start();
        }
    }

    [RelayCommand]
    private async Task StopSelectedAsync() => await StopAllOfAsync(this.Targets);

    [RelayCommand]
    private void UpdateSelected()
    {
        foreach (var torrent in this.Targets.Where(t => t.IsRunning))
        {
            torrent.UpdateNow();
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var targets = this.Targets.ToList();
        if (targets.Count == 0 || !await this.ConfirmRemovalAsync(targets))
        {
            return;
        }

        await Task.WhenAll(targets.Select(this.RemoveWithoutAskingAsync));
    }

    /// <summary>Removes one torrent after asking; used by the row's own Remove request.</summary>
    private async Task RemoveAsync(TorrentItemViewModel torrent)
    {
        if (await this.ConfirmRemovalAsync([torrent]))
        {
            await this.RemoveWithoutAskingAsync(torrent);
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

        return this.dialogs.ConfirmAsync(targets.Count == 1 ? "Remove torrent" : "Remove torrents", message, "Remove");
    }

    private async Task RemoveWithoutAskingAsync(TorrentItemViewModel torrent)
    {
        await torrent.StopAsync();
        await torrent.DisposeSessionAsync();
        this.Torrents.Remove(torrent);
        if (ReferenceEquals(this.SelectedTorrent, torrent))
        {
            this.SelectedTorrent = this.Torrents.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task RenameSelectedAsync()
    {
        if (this.SelectedTorrent is not { } torrent)
        {
            return;
        }

        var name = await this.dialogs.PromptAsync("Rename torrent", "Name shown in the list:", torrent.DisplayName);
        if (!string.IsNullOrWhiteSpace(name))
        {
            torrent.DisplayName = name;
            this.SaveLastSession();
        }
    }

    [RelayCommand]
    private async Task CopyInfoHashAsync()
    {
        if (this.SelectedTorrent is { } torrent)
        {
            await this.clipboard.SetTextAsync(torrent.InfoHashHex);
        }
    }

    [RelayCommand]
    private void UseSelectedAsDefaults()
    {
        if (this.SelectedTorrent is not { } torrent)
        {
            return;
        }

        this.settings = this.settings with { DefaultTorrentSettings = torrent.CurrentSettings };
        this.settingsStore.Save(this.settings);
        this.notifications.ShowInformation("Defaults updated", "New torrents will start from these settings.");
    }

    // ---- Bulk commands ---------------------------------------------------

    [RelayCommand]
    private void StartAll()
    {
        foreach (var torrent in this.Torrents.Where(t => t.CanStart))
        {
            torrent.Start();
        }
    }

    [RelayCommand]
    private async Task StopAllAsync() => await StopAllOfAsync(this.Torrents);

    [RelayCommand]
    private void UpdateAll()
    {
        foreach (var torrent in this.Torrents.Where(t => t.IsRunning))
        {
            torrent.UpdateNow();
        }
    }

    [RelayCommand]
    private void ClearAllLogs()
    {
        foreach (var torrent in this.Torrents)
        {
            torrent.Log.ClearCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task SetUploadSpeedForAllAsync() => await this.SetSpeedForAllAsync(upload: true);

    [RelayCommand]
    private async Task SetDownloadSpeedForAllAsync() => await this.SetSpeedForAllAsync(upload: false);

    private async Task SetSpeedForAllAsync(bool upload)
    {
        var what = upload ? "upload" : "download";
        var answer = await this.dialogs.PromptAsync($"Set {what} speed", $"New {what} speed for every torrent (kB/s):", "100");
        if (answer is null || !double.TryParse(answer, NumberStyles.Float, CultureInfo.CurrentCulture, out var kb) || kb < 0)
        {
            return;
        }

        foreach (var torrent in this.Torrents)
        {
            torrent.SetSpeeds(upload ? kb : null, upload ? null : kb);
        }

        this.SaveLastSession();
    }

    // ---- Sessions --------------------------------------------------------

    [RelayCommand]
    private async Task LoadSessionAsync() => await this.LoadSessionAsync(startImmediately: false);

    [RelayCommand]
    private async Task LoadSessionAndStartAsync() => await this.LoadSessionAsync(startImmediately: true);

    private async Task LoadSessionAsync(bool startImmediately)
    {
        var path = await this.files.PickSessionFileAsync(this.settings.LastSessionDirectory);
        if (path is not null)
        {
            await this.RestoreSessionAsync(path, startImmediately, announce: true);
        }
    }

    [RelayCommand]
    private async Task SaveSessionAsync() => await this.SaveSessionAsync(stopFirst: false);

    [RelayCommand]
    private async Task StopAllAndSaveSessionAsync() => await this.SaveSessionAsync(stopFirst: true);

    private async Task SaveSessionAsync(bool stopFirst)
    {
        if (stopFirst)
        {
            await this.StopAllAsync();
        }

        var path = await this.files.SaveSessionFileAsync("ratiomaster.session", this.settings.LastSessionDirectory);
        if (path is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            this.settings = this.settings with { LastSessionDirectory = directory };
            this.settingsStore.Save(this.settings);
        }

        SessionFile.Save(path, this.BuildSessionDocument());
        this.notifications.ShowInformation("Session saved", Path.GetFileName(path));
    }

    private SessionDocument BuildSessionDocument() => new()
    {
        Torrents = this.Torrents.Select(t => t.ToSessionEntry()).ToList(),
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
            this.notifications.ShowWarning("Could not load session", ex.Message);
            return;
        }

        this.suspendAutosave = true;
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
                    var item = this.CreateItem(descriptor, entry.Settings, name);
                    this.Torrents.Add(item);
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
            this.suspendAutosave = false;
        }

        this.SaveLastSession();
        this.SelectedTorrent ??= this.Torrents.FirstOrDefault();

        if (announce)
        {
            var loaded = document.Torrents.Count - missing;
            var message = missing == 0 ? $"{loaded} torrents loaded." : $"{loaded} torrents loaded, {missing} could not be found.";
            this.notifications.ShowInformation("Session loaded", message);
        }
    }

    private void SaveLastSession()
    {
        if (this.suspendAutosave)
        {
            return;
        }

        try
        {
            AppPaths.EnsureConfigDirectory();
            SessionFile.Save(AppPaths.LastSessionFile, this.BuildSessionDocument());
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
        var editor = new TorrentSettingsViewModel(this.factory.Catalog, this.factory, this.settings.DefaultTorrentSettings);
        var dialog = new SettingsViewModel(this.settings, editor, this.factory.Catalog);
        var saved = await this.dialogs.ShowAsync(dialog);
        if (saved is not null)
        {
            this.settings = saved;
            this.settingsStore.Save(this.settings);
            LogTimeConverter.Use24HourTime = this.settings.Use24HourTime;
            ThemeApplier.Apply(this.settings.Theme);
        }
    }

    [RelayCommand]
    private async Task ShowAboutAsync() => await this.dialogs.ShowAsync(new AboutViewModel(this.launcher) { Title = "About RatioMaster.NET" });

    [RelayCommand]
    private async Task OpenUrlAsync(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            await this.launcher.OpenUrlAsync(url);
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync() => await this.CheckForUpdatesAsync(quiet: false);

    private async Task CheckForUpdatesAsync(bool quiet)
    {
        var result = await this.updateChecker.CheckAsync();
        if (!result.Succeeded)
        {
            this.UpdateMessage = null;
            if (!quiet)
            {
                await this.dialogs.ShowMessageAsync("Check for updates", "Could not reach ratiomaster.net: " + result.Error);
            }

            return;
        }

        if (!result.UpdateAvailable)
        {
            this.UpdateMessage = null;
            if (!quiet)
            {
                await this.dialogs.ShowMessageAsync("Check for updates", "You are running the latest version.");
            }

            return;
        }

        if (quiet && result.RemoteVersion == this.settings.SkippedVersion)
        {
            return;
        }

        this.UpdateMessage = "Update available";
        var choice = await this.dialogs.ShowAsync(new NewVersionViewModel(result.RemoteVersion!, this.launcher));
        if (choice == NewVersionChoice.Skip)
        {
            this.settings = this.settings with { SkippedVersion = result.RemoteVersion };
            this.settingsStore.Save(this.settings);
        }
    }

    [RelayCommand]
    private void ToggleDetailsPane() => this.IsDetailsPaneVisible = !this.IsDetailsPaneVisible;

    [RelayCommand]
    private void Restore() => this.RestoreRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => this.ExitRequested?.Invoke(this, EventArgs.Empty);
}
