using RatioMaster.Core.Torrents;

namespace RatioMaster.Core.Sessions;

/// <summary>The torrent facts the session engine needs, independent of the .torrent file object.</summary>
public sealed record TorrentDescriptor
{
    public required byte[] InfoHash { get; init; }

    public required long TotalLength { get; init; }

    public required string TrackerUrl { get; init; }

    public string Name { get; init; } = string.Empty;

    /// <summary>Local .torrent path, when the torrent was loaded from disk.</summary>
    public string? FilePath { get; init; }

    public static TorrentDescriptor FromFile(TorrentFile file, string? trackerUrl = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new TorrentDescriptor
        {
            InfoHash = file.GetInfoHashBytes(),
            TotalLength = file.TotalLength,
            TrackerUrl = trackerUrl ?? file.Announce,
            Name = file.Name,
            FilePath = file.Path,
        };
    }
}
