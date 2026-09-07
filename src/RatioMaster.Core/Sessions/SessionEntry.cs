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

    /// <summary>Gets or sets the path of the .torrent file this entry was created from.</summary>
    public string? TorrentPath { get; set; }

    /// <summary>Gets or sets the tracker URL in use, which may differ from the torrent's announce.</summary>
    public string? TrackerUrl { get; set; }

    public TorrentSettings Settings { get; set; } = new();
}
