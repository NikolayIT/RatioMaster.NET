using System.Text;
using RatioMaster.Core.Bencode;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Tests.Fakes;

/// <summary>A scripted <see cref="ITrackerClient"/> that records calls and answers from configurable fields.</summary>
internal sealed class FakeTrackerClient : ITrackerClient
{
    public List<(TrackerEvent Event, AnnounceValues Values)> Announces { get; } = [];

    public int ScrapeCount { get; private set; }

    public int? Interval { get; set; } = 1800;

    public int? Complete { get; set; } = 10;

    public int? Incomplete { get; set; } = 5;

    public int PeerCount { get; set; } = 1;

    public string? FailureReason { get; set; }

    /// <summary>When set, throws this on every announce (simulates no connection).</summary>
    public Exception? AnnounceError { get; set; }

    /// <summary>Optional per-event override of the response.</summary>
    public Func<TrackerEvent, AnnounceResponse>? AnnounceOverride { get; set; }

    public bool ScrapeSupported { get; set; } = true;

    public int? ScrapeComplete { get; set; }

    public int? ScrapeIncomplete { get; set; }

    public Task<TrackerAnnounceOutcome> AnnounceAsync(
        ClientProfile profile,
        string trackerUrl,
        AnnounceValues values,
        TrackerEvent trackerEvent,
        ProxySettings proxy,
        bool ignoreCertificateErrors,
        CancellationToken cancellationToken)
    {
        Announces.Add((trackerEvent, values));
        if (AnnounceError is not null)
        {
            throw AnnounceError;
        }

        var response = AnnounceOverride?.Invoke(trackerEvent) ?? BuildAnnounce();
        var exchange = new TrackerExchange
        {
            Timestamp = DateTimeOffset.UnixEpoch,
            Kind = trackerEvent.ToString().ToLowerInvariant(),
            RequestUrl = trackerUrl,
            Interval = response.Interval,
            Complete = response.Complete,
            Incomplete = response.Incomplete,
            PeerCount = response.Peers.Count,
            Error = response.FailureReason,
        };
        return Task.FromResult(new TrackerAnnounceOutcome(response, exchange));
    }

    public Task<TrackerScrapeOutcome> ScrapeAsync(
        ClientProfile profile,
        string trackerUrl,
        string infoHashEncoded,
        byte[] infoHash,
        ProxySettings proxy,
        bool ignoreCertificateErrors,
        CancellationToken cancellationToken)
    {
        ScrapeCount++;
        var exchange = new TrackerExchange { Timestamp = DateTimeOffset.UnixEpoch, Kind = "scrape", RequestUrl = trackerUrl };
        if (!ScrapeSupported)
        {
            return Task.FromResult(new TrackerScrapeOutcome(null, exchange));
        }

        var stats = new BencodeDictionary();
        if (ScrapeComplete is { } c)
        {
            stats.Set("complete", c);
        }

        if (ScrapeIncomplete is { } i)
        {
            stats.Set("incomplete", i);
        }

        var files = new BencodeDictionary().Set(Encoding.Latin1.GetString(infoHash), stats);
        var dict = new BencodeDictionary().Set("files", files);
        return Task.FromResult(new TrackerScrapeOutcome(ScrapeResponse.Parse(dict, infoHash), exchange));
    }

    private AnnounceResponse BuildAnnounce()
    {
        var dict = new BencodeDictionary();
        if (FailureReason is not null)
        {
            dict.Set("failure reason", FailureReason);
            return AnnounceResponse.Parse(dict);
        }

        if (Interval is { } interval)
        {
            dict.Set("interval", interval);
        }

        if (Complete is { } complete)
        {
            dict.Set("complete", complete);
        }

        if (Incomplete is { } incomplete)
        {
            dict.Set("incomplete", incomplete);
        }

        if (PeerCount > 0)
        {
            dict.Set("peers", new BencodeString(new byte[PeerCount * 6]));
        }

        return AnnounceResponse.Parse(dict);
    }
}
