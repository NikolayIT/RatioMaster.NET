using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Settings;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>Application settings: general behaviour, tray, torrent defaults and the client catalog.</summary>
public sealed partial class SettingsViewModel : DialogViewModel<AppSettings>
{
    private readonly AppSettings _original;

    [ObservableProperty]
    private AppTheme _theme;

    [ObservableProperty]
    private bool _use24HourTime;

    [ObservableProperty]
    private bool _checkForUpdatesOnStartup;

    [ObservableProperty]
    private bool _restoreLastSessionOnStartup;

    [ObservableProperty]
    private bool _addTorrentsWithoutDialog;

    [ObservableProperty]
    private bool _confirmRemovingRunningTorrents;

    [ObservableProperty]
    private bool _startTorrentsImmediately;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _closeToTray;

    [ObservableProperty]
    private bool _showTorrentListInTrayTooltip;

    [ObservableProperty]
    private bool _startMinimized;

    public SettingsViewModel(AppSettings settings, TorrentSettingsViewModel defaults, ClientProfileCatalog catalog)
    {
        Title = "Settings";
        _original = settings;
        Defaults = defaults;
        _theme = settings.Theme;
        _use24HourTime = settings.Use24HourTime;
        _checkForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        _restoreLastSessionOnStartup = settings.RestoreLastSessionOnStartup;
        _addTorrentsWithoutDialog = settings.AddTorrentsWithoutDialog;
        _confirmRemovingRunningTorrents = settings.ConfirmRemovingRunningTorrents;
        _startTorrentsImmediately = settings.StartTorrentsImmediately;
        _minimizeToTray = settings.MinimizeToTray;
        _closeToTray = settings.CloseToTray;
        _showTorrentListInTrayTooltip = settings.ShowTorrentListInTrayTooltip;
        _startMinimized = settings.StartMinimized;

        ClientCount = catalog.Profiles.Count;
        UserClientsPath = AppPaths.UserClientsFile;
        ConfigDirectory = AppPaths.ConfigDirectory;
    }

    public TorrentSettingsViewModel Defaults { get; }

    public int ClientCount { get; }

    public string UserClientsPath { get; }

    public string ConfigDirectory { get; }

    public static IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    public string ClientSummary =>
        $"{ClientCount} client emulations are loaded. Add or override them by creating this file:";

    [RelayCommand]
    private void Save() => Close(_original with
    {
        Theme = Theme,
        Use24HourTime = Use24HourTime,
        CheckForUpdatesOnStartup = CheckForUpdatesOnStartup,
        RestoreLastSessionOnStartup = RestoreLastSessionOnStartup,
        AddTorrentsWithoutDialog = AddTorrentsWithoutDialog,
        ConfirmRemovingRunningTorrents = ConfirmRemovingRunningTorrents,
        StartTorrentsImmediately = StartTorrentsImmediately,
        MinimizeToTray = MinimizeToTray,
        CloseToTray = CloseToTray,
        ShowTorrentListInTrayTooltip = ShowTorrentListInTrayTooltip,
        StartMinimized = StartMinimized,
        DefaultTorrentSettings = Defaults.ToSettings(),
    });

    [RelayCommand]
    private void Cancel() => Close(null);
}
