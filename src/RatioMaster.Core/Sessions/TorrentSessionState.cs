namespace RatioMaster.Core.Sessions
{
    /// <summary>The lifecycle state of a torrent session.</summary>
    public enum TorrentSessionState
    {
        /// <summary>Never started.</summary>
        Idle,

        /// <summary>The "started" announce is in flight.</summary>
        Starting,

        /// <summary>Running, still below 100%.</summary>
        Downloading,

        /// <summary>Running at 100%.</summary>
        Seeding,

        /// <summary>An announce is in flight.</summary>
        Updating,

        /// <summary>The "stopped" announce is in flight.</summary>
        Stopping,

        /// <summary>Stopped (by the user or a stop condition).</summary>
        Stopped,

        /// <summary>Stopped because of a tracker or connection error.</summary>
        Error,
    }
}
