namespace BitTorrent
{
    using System.IO;

    internal class TorrentFile
    {
        private readonly FileInfo fileInfo;

        internal TorrentFile(long len, string path) // : this()
        {
            this.fileInfo = new FileInfo(path);
        }

        internal long Length => this.fileInfo.Length;

        internal string Path => this.fileInfo.FullName;

        internal string Name => this.fileInfo.Name;
    }
}
