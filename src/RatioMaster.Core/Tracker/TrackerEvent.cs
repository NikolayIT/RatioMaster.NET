namespace RatioMaster.Core.Tracker
{
    /// <summary>The BitTorrent announce event.</summary>
    public enum TrackerEvent
    {
        /// <summary>A periodic update (no event parameter).</summary>
        None,

        /// <summary>The first announce for this session.</summary>
        Started,

        /// <summary>The final announce when stopping.</summary>
        Stopped,

        /// <summary>Sent once when the download reaches 100%.</summary>
        Completed,
    }
}
