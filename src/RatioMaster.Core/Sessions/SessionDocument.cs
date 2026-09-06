using System.Text.Json.Serialization;

namespace RatioMaster.Core.Sessions;

/// <summary>One torrent inside a saved session.</summary>
public sealed record SessionEntry
{
    [JsonConstructor]
    public SessionEntry()
    {
    }

    public string Name { get; set; } = string.Empty;

    /// <summary>Path of the .torrent file this entry was created from.</summary>
    public string? TorrentPath { get; set; }

    /// <summary>The tracker URL in use, which may differ from the torrent's announce.</summary>
    public string? TrackerUrl { get; set; }

    public TorrentSettings Settings { get; set; } = new();
}

/// <summary>A saved session: the list of torrents with their settings.</summary>
public sealed record SessionDocument
{
    [JsonConstructor]
    public SessionDocument()
    {
    }

    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public List<SessionEntry> Torrents { get; set; } = [];
}
