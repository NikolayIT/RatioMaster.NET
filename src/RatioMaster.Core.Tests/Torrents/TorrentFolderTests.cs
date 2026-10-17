namespace RatioMaster.Core.Tests.Torrents
{
    using RatioMaster.Core.Torrents;

    public sealed class TorrentFolderTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "rm-folder-" + Guid.NewGuid().ToString("N"));

        public TorrentFolderTests()
        {
            Directory.CreateDirectory(Path.Combine(this.directory, "sub"));
        }

        public void Dispose() => Directory.Delete(this.directory, recursive: true);

        [Fact]
        public void FindsTheTorrentsInTheFolderItselfWhateverTheCaseOfTheExtension()
        {
            foreach (var name in new[] { "b.torrent", "A.TORRENT", "notes.txt", "c.torrent.part", Path.Combine("sub", "d.torrent") })
            {
                File.WriteAllText(Path.Combine(this.directory, name), string.Empty);
            }

            var found = TorrentFolder.FindTorrentFiles(this.directory);

            Assert.Equal(["A.TORRENT", "b.torrent"], found.Select(Path.GetFileName));
            Assert.All(found, path => Assert.Equal(this.directory, Path.GetDirectoryName(path)));
        }

        [Fact]
        public void AFolderWithoutTorrentsGivesAnEmptyList()
        {
            Assert.Empty(TorrentFolder.FindTorrentFiles(this.directory));
        }
    }
}
