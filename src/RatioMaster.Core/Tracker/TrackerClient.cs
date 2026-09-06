using System.Diagnostics;
using RatioMaster.Core.Abstractions;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tracker;

/// <summary>The production <see cref="ITrackerClient"/>: builds URLs and requests over <see cref="TrackerHttpClient"/>.</summary>
public sealed class TrackerClient : ITrackerClient
{
    private readonly TrackerHttpClient _http;
    private readonly ISystemClock _clock;

    public TrackerClient(TrackerHttpClient? http = null, ISystemClock? clock = null)
    {
        _http = http ?? new TrackerHttpClient();
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<TrackerAnnounceOutcome> AnnounceAsync(
        ClientProfile profile,
        string trackerUrl,
        AnnounceValues values,
        TrackerEvent trackerEvent,
        ProxySettings proxy,
        bool ignoreCertificateErrors,
        CancellationToken cancellationToken)
    {
        var url = AnnounceUrlBuilder.Build(trackerUrl, profile.Query, values, trackerEvent);
        var started = Stopwatch.GetTimestamp();
        var http = WithCertificatePolicy(ignoreCertificateErrors);

        try
        {
            var raw = await http.GetAsync(url, profile, proxy, cancellationToken).ConfigureAwait(false);
            var response = raw.Dictionary is not null
                ? AnnounceResponse.Parse(raw.Dictionary)
                : AnnounceResponse.Parse(new Bencode.BencodeDictionary());
            var exchange = new TrackerExchange
            {
                Timestamp = _clock.Now,
                Kind = trackerEvent == TrackerEvent.None ? "announce" : trackerEvent.ToString().ToLowerInvariant(),
                RequestUrl = url,
                ResponseHeaders = raw.RawHeaders,
                Interval = response.Interval,
                Complete = response.Complete,
                Incomplete = response.Incomplete,
                PeerCount = response.Peers.Count,
                DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                Error = response.FailureReason,
            };
            return new TrackerAnnounceOutcome(response, exchange);
        }
        catch (Exception ex) when (IsTrackerFailure(ex, cancellationToken))
        {
            throw new TrackerException($"Announce to {trackerUrl} failed: {ex.Message}", ex);
        }
    }

    public async Task<TrackerScrapeOutcome> ScrapeAsync(
        ClientProfile profile,
        string trackerUrl,
        string infoHashEncoded,
        byte[] infoHash,
        ProxySettings proxy,
        bool ignoreCertificateErrors,
        CancellationToken cancellationToken)
    {
        var url = ScrapeUrlBuilder.TryBuild(trackerUrl, infoHashEncoded);
        var timestamp = _clock.Now;
        if (url is null)
        {
            return new TrackerScrapeOutcome(null, new TrackerExchange
            {
                Timestamp = timestamp,
                Kind = "scrape",
                RequestUrl = trackerUrl,
                Error = "This tracker does not support scrape.",
            });
        }

        var started = Stopwatch.GetTimestamp();
        var http = WithCertificatePolicy(ignoreCertificateErrors);
        try
        {
            var raw = await http.GetAsync(url, profile, proxy, cancellationToken).ConfigureAwait(false);
            var response = raw.Dictionary is not null ? ScrapeResponse.Parse(raw.Dictionary, infoHash) : null;
            var exchange = new TrackerExchange
            {
                Timestamp = timestamp,
                Kind = "scrape",
                RequestUrl = url,
                ResponseHeaders = raw.RawHeaders,
                Complete = response?.Complete,
                Incomplete = response?.Incomplete,
                DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                Error = response?.FailureReason,
            };
            return new TrackerScrapeOutcome(response, exchange);
        }
        catch (Exception ex) when (IsTrackerFailure(ex, cancellationToken))
        {
            throw new TrackerException($"Scrape of {trackerUrl} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Every way a request can go wrong (refused connection, TLS, proxy, malformed reply, a bug in the
    /// transport) is one thing to the engine: this announce did not happen, try again later. Only the
    /// caller's own cancellation passes through unchanged.
    /// </summary>
    private static bool IsTrackerFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private TrackerHttpClient WithCertificatePolicy(bool ignoreCertificateErrors) => ignoreCertificateErrors
        ? _http.WithIgnoredCertificateErrors()
        : _http;
}
