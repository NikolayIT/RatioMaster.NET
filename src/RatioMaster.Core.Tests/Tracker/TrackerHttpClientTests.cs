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

    private static int GetClosedPort()
    {
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
