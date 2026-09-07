using System.Text.Json;

namespace RatioMaster.Core.Settings;

/// <summary>Reads and writes <see cref="AppSettings"/> as JSON, replacing the old registry storage.</summary>
public interface ISettingsStore
{
    string FilePath { get; }

    AppSettings Load();

    void Save(AppSettings settings);
}
