namespace RatioMaster.Core.Torrents
{
    /// <summary>One file inside a torrent.</summary>
    /// <param name="Path">Relative path with '/' separators.</param>
    /// <param name="Length">Size in bytes.</param>
    public sealed record TorrentFileEntry(string Path, long Length);
}
