namespace RatioMaster.App.Converters
{
    using RatioMaster.App.Resources;
    using RatioMaster.App.ViewModels;
    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Sessions;
    using RatioMaster.Core.Settings;

    /// <summary>The translated names of the enum values the drop-downs and the details pane show.</summary>
    public static class DisplayNames
    {
        /// <summary>Gets the name of any value the views show: a known enum by its translation, anything else as it is.</summary>
        public static string Of(object? value) => value switch
        {
            TorrentFilter filter => Of(filter),
            StopConditionType stop => Of(stop),
            ProxyType proxy => Of(proxy),
            AppTheme theme => Of(theme),
            null => string.Empty,
            _ => value.ToString() ?? string.Empty,
        };

        public static string Of(TorrentFilter filter) => filter switch
        {
            TorrentFilter.Running => Strings.FilterRunning,
            TorrentFilter.Stopped => Strings.FilterStopped,
            TorrentFilter.Downloading => Strings.FilterDownloading,
            TorrentFilter.Seeding => Strings.FilterSeeding,
            TorrentFilter.Error => Strings.FilterError,
            _ => Strings.FilterAll,
        };

        public static string Of(StopConditionType stop) => stop switch
        {
            StopConditionType.AfterSeconds => Strings.StopTypeAfterSeconds,
            StopConditionType.SeedersBelow => Strings.StopTypeSeedersBelow,
            StopConditionType.LeechersBelow => Strings.StopTypeLeechersBelow,
            StopConditionType.UploadedAboveMb => Strings.StopTypeUploadedAboveMb,
            StopConditionType.DownloadedAboveMb => Strings.StopTypeDownloadedAboveMb,
            StopConditionType.LeecherSeederRatioBelow => Strings.StopTypeLeecherSeederRatioBelow,
            _ => Strings.StopTypeNever,
        };

        /// <summary>Gets the proxy protocol name; only "None" is a word, the rest are protocol names.</summary>
        public static string Of(ProxyType proxy) => proxy switch
        {
            ProxyType.HttpConnect => "HTTP CONNECT",
            ProxyType.Socks4 => "SOCKS4",
            ProxyType.Socks4a => "SOCKS4a",
            ProxyType.Socks5 => "SOCKS5",
            _ => Strings.ProxyNone,
        };

        public static string Of(AppTheme theme) => theme switch
        {
            AppTheme.Light => Strings.ThemeLight,
            AppTheme.Dark => Strings.ThemeDark,
            _ => Strings.ThemeSystem,
        };
    }
}
