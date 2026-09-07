namespace RatioMaster.Core.Tracker
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;

    /// <summary>Announce and scrape at the level the session engine needs. Fakeable for tests.</summary>
    public interface ITrackerClient
    {
        Task<TrackerAnnounceOutcome> AnnounceAsync(
            ClientProfile profile,
            string trackerUrl,
            AnnounceValues values,
            TrackerEvent trackerEvent,
            ProxySettings proxy,
            bool ignoreCertificateErrors,
            CancellationToken cancellationToken);

        Task<TrackerScrapeOutcome> ScrapeAsync(
            ClientProfile profile,
            string trackerUrl,
            string infoHashEncoded,
            byte[] infoHash,
            ProxySettings proxy,
            bool ignoreCertificateErrors,
            CancellationToken cancellationToken);
    }
}
