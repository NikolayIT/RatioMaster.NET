using System.Text.Json;

namespace RatioMaster.Core.Settings;

public sealed class SettingsStore : ISettingsStore
{
    public SettingsStore(string? filePath = null)
    {
        this.FilePath = filePath ?? AppPaths.SettingsFile;
    }

    public string FilePath { get; }

    /// <summary>Loads the settings, returning defaults when the file is missing or unreadable.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(this.FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(this.FilePath);
            return JsonSerializer.Deserialize(json, CoreJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Writes the settings atomically so a crash cannot leave a truncated file.</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(this.FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, CoreJsonContext.Default.AppSettings);
        var temporary = this.FilePath + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, this.FilePath, overwrite: true);
    }
}
