namespace RatioMaster.Core.Settings;

/// <summary>Where the application keeps its configuration on each operating system.</summary>
public static class AppPaths
{
    public const string FolderName = "RatioMaster.NET";

    /// <summary>
    /// Gets %APPDATA%\RatioMaster.NET on Windows, ~/Library/Application Support/RatioMaster.NET on macOS,
    /// $XDG_CONFIG_HOME (or ~/.config)/RatioMaster.NET elsewhere.
    /// </summary>
    public static string ConfigDirectory { get; } = ResolveConfigDirectory();

    public static string SettingsFile => Path.Combine(ConfigDirectory, "settings.json");

    public static string LastSessionFile => Path.Combine(ConfigDirectory, "last-session.json");

    /// <summary>Gets the optional user-supplied client profiles that extend or override the built-in ones.</summary>
    public static string UserClientsFile => Path.Combine(ConfigDirectory, "clients.json");

    public static void EnsureConfigDirectory() => Directory.CreateDirectory(ConfigDirectory);

    private static string ResolveConfigDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", FolderName);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(appData))
        {
            appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(appData, FolderName);
    }
}
