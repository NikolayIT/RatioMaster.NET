using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tracker;

/// <summary>The result of one announce: the parsed response and a record of the exchange for the UI.</summary>
public sealed record TrackerAnnounceOutcome(AnnounceResponse Response, TrackerExchange Exchange);

/// <summary>The result of one scrape, or the exchange alone when the tracker has no scrape endpoint.</summary>
public sealed record TrackerScrapeOutcome(ScrapeResponse? Response, TrackerExchange Exchange);

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
