namespace RatioMaster.App.ViewModels.Dialogs
{
    using System.Collections.Generic;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Settings;

    /// <summary>Application settings: general behaviour, tray, torrent defaults and the client catalog.</summary>
    public sealed partial class SettingsViewModel : DialogViewModel<AppSettings>
    {
        private readonly AppSettings original;

        [ObservableProperty]
        private AppTheme theme;

        [ObservableProperty]
        private bool use24HourTime;

        [ObservableProperty]
        private bool checkForUpdatesOnStartup;

        [ObservableProperty]
        private bool restoreLastSessionOnStartup;

        [ObservableProperty]
        private bool addTorrentsWithoutDialog;

        [ObservableProperty]
        private bool startTorrentsImmediately;

        [ObservableProperty]
        private bool minimizeToTray;

        [ObservableProperty]
        private bool closeToTray;

        [ObservableProperty]
        private bool showTorrentListInTrayTooltip;

        [ObservableProperty]
        private bool startMinimized;

        public SettingsViewModel(AppSettings settings, TorrentSettingsViewModel defaults, ClientProfileCatalog catalog)
        {
            this.Title = "Settings";
            this.original = settings;
            this.Defaults = defaults;
            this.theme = settings.Theme;
            this.use24HourTime = settings.Use24HourTime;
            this.checkForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
            this.restoreLastSessionOnStartup = settings.RestoreLastSessionOnStartup;
            this.addTorrentsWithoutDialog = settings.AddTorrentsWithoutDialog;
            this.startTorrentsImmediately = settings.StartTorrentsImmediately;
            this.minimizeToTray = settings.MinimizeToTray;
            this.closeToTray = settings.CloseToTray;
            this.showTorrentListInTrayTooltip = settings.ShowTorrentListInTrayTooltip;
            this.startMinimized = settings.StartMinimized;

            this.ClientCount = catalog.Profiles.Count;
            this.UserClientsPath = AppPaths.UserClientsFile;
            this.ConfigDirectory = AppPaths.ConfigDirectory;
        }

        public static IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

        public TorrentSettingsViewModel Defaults { get; }

        public int ClientCount { get; }

        public string UserClientsPath { get; }

        public string ConfigDirectory { get; }

        public string ClientSummary =>
            $"{this.ClientCount} client emulations are loaded. Add or override them by creating this file:";

        [RelayCommand]
        private void Save() => this.Close(this.original with
        {
            Theme = this.Theme,
            Use24HourTime = this.Use24HourTime,
            CheckForUpdatesOnStartup = this.CheckForUpdatesOnStartup,
            RestoreLastSessionOnStartup = this.RestoreLastSessionOnStartup,
            AddTorrentsWithoutDialog = this.AddTorrentsWithoutDialog,
            StartTorrentsImmediately = this.StartTorrentsImmediately,
            MinimizeToTray = this.MinimizeToTray,
            CloseToTray = this.CloseToTray,
            ShowTorrentListInTrayTooltip = this.ShowTorrentListInTrayTooltip,
            StartMinimized = this.StartMinimized,
            DefaultTorrentSettings = this.Defaults.ToSettings(),
        });

        [RelayCommand]
        private void Cancel() => this.Close(null);
    }
}
