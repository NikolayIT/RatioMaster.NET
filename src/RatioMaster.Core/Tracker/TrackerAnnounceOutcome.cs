namespace RatioMaster.Core.Tracker
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;

    /// <summary>The result of one announce: the parsed response and a record of the exchange for the UI.</summary>
    public sealed record TrackerAnnounceOutcome(AnnounceResponse Response, TrackerExchange Exchange);
}
