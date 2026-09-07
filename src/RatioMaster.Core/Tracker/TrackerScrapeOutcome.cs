namespace RatioMaster.Core.Tracker
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;

    /// <summary>The result of one scrape, or the exchange alone when the tracker has no scrape endpoint.</summary>
    public sealed record TrackerScrapeOutcome(ScrapeResponse? Response, TrackerExchange Exchange);
}
