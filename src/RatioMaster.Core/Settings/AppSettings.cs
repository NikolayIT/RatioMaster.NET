namespace RatioMaster.Core.Settings
{
    using System.Text.Json.Serialization;

    using RatioMaster.Core.Sessions;

    /// <summary>Everything the application remembers between runs.</summary>
    public sealed record AppSettings
    {
        [JsonConstructor]
        public AppSettings()
        {
        }

        /// <summary>Gets a value indicating whether windows and macOS always have a tray (menu bar); Linux desktops often do not.</summary>
        public static bool TrayIsReliable => !OperatingSystem.IsLinux();

        public AppTheme Theme { get; set; } = AppTheme.System;

        public bool Use24HourTime { get; set; }

        public bool CheckForUpdatesOnStartup { get; set; } = true;

        /// <summary>Gets or sets a remote version the user chose to skip.</summary>
        public string? SkippedVersion { get; set; }

        public bool RestoreLastSessionOnStartup { get; set; } = true;

        public bool AddTorrentsWithoutDialog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether off by default on Linux: the tray icon needs a StatusNotifier host (KDE, or GNOME with the AppIndicator
        /// extension), and without one a hidden window cannot be brought back.
        /// </summary>
        public bool MinimizeToTray { get; set; } = TrayIsReliable;

        /// <inheritdoc cref="MinimizeToTray"/>
        public bool CloseToTray { get; set; } = TrayIsReliable;

        public bool ShowTorrentListInTrayTooltip { get; set; }

        public bool StartMinimized { get; set; }

        public string? LastTorrentDirectory { get; set; }

        public string? LastSessionDirectory { get; set; }

        public WindowPlacement Window { get; set; } = new();

        public double DetailsPaneHeight { get; set; } = 300;

        public IReadOnlyList<ColumnLayout> Columns { get; set; } = [];

        /// <summary>Gets or sets the settings a newly added torrent starts from.</summary>
        public TorrentSettings DefaultTorrentSettings { get; set; } = new();

        /// <summary>Gets or sets a value indicating whether the Add dialog's "start immediately" box is ticked by default.</summary>
        public bool StartTorrentsImmediately { get; set; } = true;
    }
}
