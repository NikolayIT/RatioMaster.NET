namespace RatioMaster.Core.Torrents
{
    /// <summary>Thrown when a .torrent file is missing required data.</summary>
    public sealed class TorrentFormatException : Exception
    {
        public TorrentFormatException()
        {
        }

        public TorrentFormatException(string message)
            : base(message)
        {
        }

        public TorrentFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
