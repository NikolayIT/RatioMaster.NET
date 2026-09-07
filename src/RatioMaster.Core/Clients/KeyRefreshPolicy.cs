namespace RatioMaster.Core.Clients
{
    /// <summary>
    /// How often the real client makes up a new tracker "key". Most clients pick one per torrent and keep it
    /// for as long as the torrent is loaded; uTorrent and BitTorrent roll theirs every few minutes, so a
    /// session that keeps announcing the same key for hours does not look like them.
    /// </summary>
    public enum KeyRefreshPolicy
    {
        /// <summary>One key for the whole session, which is what most clients do.</summary>
        Never,

        /// <summary>A new key every <see cref="ClientProfile.KeyRefreshMinutes"/> minutes, as uTorrent does.</summary>
        Timed,
    }
}
