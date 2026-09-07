using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tracker;

/// <summary>The result of one scrape, or the exchange alone when the tracker has no scrape endpoint.</summary>
public sealed record TrackerScrapeOutcome(ScrapeResponse? Response, TrackerExchange Exchange);
