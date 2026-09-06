using RatioMaster.Core.Bencode;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Tracker;
using RatioMaster.Core.Tests.Fakes;

namespace RatioMaster.Core.Tests.Tracker;

public class TrackerHttpClientTests
{
    private static readonly ClientProfile Profile = ClientProfileCatalog.Load().GetByName("uTorrent 3.3.2");

    private static CancellationToken Timeout => new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

    private static byte[] AnnounceBody() =>
        new BencodeDictionary().Set("interval", 1800).Set("complete", 5).Set("incomplete", 2).ToBytes();

    [Fact]
    public async Task SendsRequestAndParsesResponse()
    {
        await using var tracker = FakeTracker.Start(FakeTracker.BencodeResponse(AnnounceBody()));
        var client = new TrackerHttpClient();

        var response = await client.GetAsync($"{tracker.BaseUrl}/announce?info_hash=%aa", Profile, ProxySettings.None, Timeout);
        var announce = AnnounceResponse.Parse(response.Dictionary!);

        Assert.Equal(1800, announce.Interval);
        Assert.Equal(5, announce.Complete);
        Assert.Single(tracker.Requests);
        Assert.StartsWith("GET /announce?info_hash=%aa HTTP/1.1", tracker.Requests.First(), StringComparison.Ordinal);
        Assert.Contains("User-Agent: uTorrent/3320", tracker.Requests.First(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FollowsRedirects()
    {
        // A relative Location resolves against the request URI, so both hits land on this tracker.
        await using var tracker = FakeTracker.Start(
            FakeTracker.Redirect("/final"),
            FakeTracker.BencodeResponse(AnnounceBody()));
        var client = new TrackerHttpClient();

        var response = await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout);

        Assert.NotNull(response.Dictionary);
        Assert.Equal(1800, AnnounceResponse.Parse(response.Dictionary!).Interval);
        Assert.Equal(2, tracker.Requests.Count);
        Assert.StartsWith("GET /final HTTP/1.1", tracker.Requests.Last(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReturnsAsSoonAsTheMessageIsCompleteOnAKeepAliveConnection()
    {
        // Cloudflare-style front ends never close the connection after an HTTP/1.1 request; the uTorrent
        // profile does not send Connection: close, so the reader must stop at the declared length.
        await using var tracker = FakeTracker.StartKeepAlive(FakeTracker.KeepAliveResponse(AnnounceBody()));
        var client = new TrackerHttpClient(options: new TrackerHttpClientOptions { Timeout = TimeSpan.FromSeconds(20) });

        var started = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout);

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(5), $"took {started.Elapsed}");
        Assert.Equal(1800, AnnounceResponse.Parse(response.Dictionary!).Interval);
    }

    [Fact]
    public async Task ReadsAChunkedBodyOnAKeepAliveConnection()
    {
        await using var tracker = FakeTracker.StartKeepAlive(FakeTracker.ChunkedKeepAliveResponse(AnnounceBody()));
        var client = new TrackerHttpClient(options: new TrackerHttpClientOptions { Timeout = TimeSpan.FromSeconds(20) });

        var started = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout);

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(5), $"took {started.Elapsed}");
        Assert.Equal(5, AnnounceResponse.Parse(response.Dictionary!).Complete);
    }

    [Fact]
    public async Task ReportsATimeoutAsATrackerError()
    {
        // The headers promise more body than ever arrives and the server keeps the socket open.
        var truncated = FakeTracker.KeepAliveResponse(AnnounceBody(), "Content-Length: 4096\r\n");
        await using var tracker = FakeTracker.StartKeepAlive(truncated);
        var client = new TrackerHttpClient(options: new TrackerHttpClientOptions { Timeout = TimeSpan.FromSeconds(1) });

        var error = await Assert.ThrowsAsync<TrackerException>(async () =>
            await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout));

        Assert.Contains("did not respond", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThrowsOnEmptyResponse()
    {
        await using var tracker = FakeTracker.Start(Array.Empty<byte>());
        var client = new TrackerHttpClient();

        await Assert.ThrowsAsync<TrackerException>(async () =>
            await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout));
    }

    [Fact]
    public async Task ThrowsAfterExhaustingConnectAttempts()
    {
        // Nothing is listening on this port.
        var deadPort = GetClosedPort();
        var client = new TrackerHttpClient(options: new TrackerHttpClientOptions { ConnectAttempts = 2 });

        await Assert.ThrowsAsync<TrackerException>(async () =>
            await client.GetAsync($"http://127.0.0.1:{deadPort}/announce", Profile, ProxySettings.None, Timeout));
    }

    [Theory]
    [InlineData("udp://tracker.test:1337/announce", "udp://")]
    [InlineData("tracker.test/announce", "not a valid tracker URL")]
    [InlineData("http://exa mple/announce", "not a valid tracker URL")]
    public async Task RejectsUrlsItCannotAnnounceToAsATrackerError(string url, string expectedText)
    {
        // These used to escape as UriFormatException, or as a pointless TCP connect to a UDP tracker.
        var client = new TrackerHttpClient(options: new TrackerHttpClientOptions { ConnectAttempts = 1 });

        var error = await Assert.ThrowsAsync<TrackerException>(async () =>
            await client.GetAsync(url, Profile, ProxySettings.None, Timeout));

        Assert.Contains(expectedText, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReturnsARedirectItCannotFollowInsteadOfThrowing()
    {
        await using var tracker = FakeTracker.Start(
            FakeTracker.Redirect("http://[not-a-host/announce"),
            FakeTracker.BencodeResponse(AnnounceBody()));
        var client = new TrackerHttpClient();

        var response = await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout);

        Assert.Equal(302, response.StatusCode);
        Assert.Single(tracker.Requests);
    }

    [Fact]
    public async Task DoesNotFollowARedirectToANonHttpScheme()
    {
        await using var tracker = FakeTracker.Start(
            FakeTracker.Redirect("udp://tracker.test:1337/announce"),
            FakeTracker.BencodeResponse(AnnounceBody()));
        var client = new TrackerHttpClient();

        var response = await client.GetAsync($"{tracker.BaseUrl}/announce", Profile, ProxySettings.None, Timeout);

        Assert.Equal(302, response.StatusCode);
        Assert.Single(tracker.Requests);
    }

    private static int GetClosedPort()
    {
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
