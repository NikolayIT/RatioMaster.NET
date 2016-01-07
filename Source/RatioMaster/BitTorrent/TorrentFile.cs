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

        internal long Length
        {
            get
            {
                return this.fileInfo.Length;
            }
        }

        internal string Path
        {
            get
            {
                return this.fileInfo.FullName;
            }
        }

        internal string Name
        {
            get
            {
                return this.fileInfo.Name;
            }
        }
    }
}
