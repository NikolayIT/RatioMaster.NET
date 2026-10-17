namespace RatioMaster.Core.Torrents
{
    /// <summary>
    /// Finds the torrents in a folder, for adding them all at once. A file picker is not always up to that: the LXQt one
    /// passes a multiple selection back through a text box that holds 32,767 characters, so of a thousand torrents only
    /// the first few hundred arrive (issue #57).
    /// </summary>
    public static class TorrentFolder
    {
        private static readonly EnumerationOptions TopLevelTorrents = new()
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
        };

        /// <summary>Gets the .torrent files directly in <paramref name="directory"/>, not in its subfolders, sorted by name.</summary>
        public static IReadOnlyList<string> FindTorrentFiles(string directory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            return [.. Directory.EnumerateFiles(directory, "*.torrent", TopLevelTorrents).Order(StringComparer.OrdinalIgnoreCase)];
        }
    }
}
