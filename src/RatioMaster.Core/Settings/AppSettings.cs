using RatioMaster.Core.Sessions;
using System.Text.Json.Serialization;

namespace RatioMaster.Core.Settings;

/// <summary>Which colour theme the window uses.</summary>
public enum AppTheme
{
    System,
    Light,
    Dark,
}

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

/// <summary>One column of the torrent list, as the user arranged it.</summary>
public sealed record ColumnLayout
{
    [JsonConstructor]
    public ColumnLayout()
    {
    }

    public required string Name { get; set; }

    public bool IsVisible { get; set; } = true;

    public double Width { get; set; }

    public int DisplayIndex { get; set; }
}

/// <summary>Everything the application remembers between runs.</summary>
public sealed record AppSettings
{
    [JsonConstructor]
    public AppSettings()
    {
    }

    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool Use24HourTime { get; set; }

    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>A remote version the user chose to skip.</summary>
    public string? SkippedVersion { get; set; }

    public bool RestoreLastSessionOnStartup { get; set; } = true;

    public bool AddTorrentsWithoutDialog { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; } = true;

    public bool ShowTorrentListInTrayTooltip { get; set; }

    public bool StartMinimized { get; set; }

    public string? LastTorrentDirectory { get; set; }

    public string? LastSessionDirectory { get; set; }

    public WindowPlacement Window { get; set; } = new();

    public double DetailsPaneHeight { get; set; } = 300;

    public IReadOnlyList<ColumnLayout> Columns { get; set; } = [];

    /// <summary>The settings a newly added torrent starts from.</summary>
    public TorrentSettings DefaultTorrentSettings { get; set; } = new();

    /// <summary>Whether the Add dialog's "start immediately" box is ticked by default.</summary>
    public bool StartTorrentsImmediately { get; set; } = true;
}
