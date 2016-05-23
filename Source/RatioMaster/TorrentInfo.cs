namespace RatioMaster_source
{
    using System;

    internal struct TorrentInfo
    {
        private Random random;

        internal TorrentInfo(long uploaded, long downloaded)
        {
            this.uploaded = uploaded;
            this.downloaded = downloaded;
            this.tracker = string.Empty;
            this.hash = string.Empty;
            this.left = 10000;
            this.totalsize = 10000;
            this.filename = string.Empty;
            this.uploadRate = 60 * 1024;
            this.downloadRate = 30 * 1024;
            this.interval = 1800;
            this.random = new Random();
            this.key = this.random.Next(1000).ToString();
            this.port = this.random.Next(1025, 65535).ToString();
            this.numberOfPeers = "200";
            this.peerID = string.Empty;
            this.trackerUri = null;
        }

        internal long downloaded { get; set; }

        internal long downloadRate { get; set; }

        internal string filename { get; set; }

        internal string hash { get; set; }

        internal int interval { get; set; }

        internal string key { get; set; }

        internal long left { get; set; }

        internal string numberOfPeers { get; set; }

        internal string peerID { get; set; }

        internal string port { get; set; }

        internal long totalsize { get; set; }

        internal string tracker { get; set; }

        internal long uploaded { get; set; }

        internal long uploadRate { get; set; }

        internal Uri trackerUri { get; set; }
    }
}
