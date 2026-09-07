using System.Text.Json.Serialization;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Settings;

/// <summary>Persisted position and size of the main window.</summary>
public sealed record WindowPlacement
{
    [JsonConstructor]
    public WindowPlacement()
    {
    }

    public double? X { get; set; }

    public double? Y { get; set; }

    public double Width { get; set; } = 1340;

    public double Height { get; set; } = 820;

    public bool IsMaximized { get; set; }
}
