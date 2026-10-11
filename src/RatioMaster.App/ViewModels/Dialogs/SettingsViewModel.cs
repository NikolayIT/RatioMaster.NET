namespace RatioMaster.App.ViewModels.Dialogs
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using RatioMaster.App.Resources;
    using RatioMaster.App.Services;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Settings;

    /// <summary>Application settings: general behaviour, tray, torrent defaults and the client catalog.</summary>
    public sealed partial class SettingsViewModel : DialogViewModel<AppSettings>
    {
        private readonly AppSettings original;

        [ObservableProperty]
        private AppTheme theme;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRestartNeeded))]
        private LanguageOption selectedLanguage;

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
            this.Title = Strings.Settings;
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

            var systemDefault = string.Format(CultureInfo.CurrentCulture, Strings.LanguageSystemDefault, Localization.SystemLanguage.NativeName);
            this.Languages = [new LanguageOption(null, systemDefault), .. UiLanguages.All.Select(l => new LanguageOption(l.Code, l.NativeName))];
            var saved = UiLanguages.Find(settings.Language)?.Code;
            this.selectedLanguage = this.Languages.FirstOrDefault(o => o.Code is not null && o.Code == saved) ?? this.Languages[0];

            this.ClientCount = catalog.Profiles.Count;
            this.UserClientsPath = AppPaths.UserClientsFile;
            this.ConfigDirectory = AppPaths.ConfigDirectory;
        }

        public static IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

        public TorrentSettingsViewModel Defaults { get; }

        /// <summary>Gets the language picker entries: "System default" first, then every translation by its own name.</summary>
        public IReadOnlyList<LanguageOption> Languages { get; }

        /// <summary>Gets a value indicating whether the picked language differs from the one on screen, which takes a restart.</summary>
        public bool IsRestartNeeded => UiLanguages.Resolve(this.SelectedLanguage.Code, Localization.SystemCulture) != Localization.Current;

        public int ClientCount { get; }

        public string UserClientsPath { get; }

        public string ConfigDirectory { get; }

        public string ClientSummary => string.Format(CultureInfo.CurrentCulture, Strings.ClientsSummary, this.ClientCount);

        [RelayCommand]
        private void Save() => this.Close(this.original with
        {
            Theme = this.Theme,
            Language = this.SelectedLanguage.Code,
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
