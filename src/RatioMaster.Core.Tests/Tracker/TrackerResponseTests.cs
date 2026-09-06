using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using RatioMaster.Core.Bencode;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Tests.Tracker;

public class TrackerResponseTests
{
    private static byte[] Http(string headers, byte[] body) =>
        [.. Encoding.Latin1.GetBytes("HTTP/1.1 200 OK\r\n" + headers + "\r\n"), .. body];

    private static byte[] SampleAnnounceBody()
    {
        return new BencodeDictionary()
            .Set("interval", 1800)
            .Set("complete", 12)
            .Set("incomplete", 3)
            .ToBytes();
    }

    [Fact]
    public void ParsesPlainBencodeBody()
    {
        var response = TrackerResponse.Parse(Http("Content-Type: text/plain\r\n", SampleAnnounceBody()));

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Dictionary);
        Assert.Equal(1800, response.Dictionary!.GetInteger("interval"));
        Assert.False(response.IsRedirect);
    }

    [Fact]
    public void ParsesGzipEncodedBody()
    {
        var body = SampleAnnounceBody();
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(body);
        }

        var response = TrackerResponse.Parse(Http("Content-Encoding: gzip\r\n", compressed.ToArray()));

        Assert.NotNull(response.Dictionary);
        Assert.Equal(12, response.Dictionary!.GetInteger("complete"));
    }

    [Fact]
    public void ParsesChunkedBody()
    {
        var body = SampleAnnounceBody();
        var half = body.Length / 2;
        var chunked = new StringBuilder();
        chunked.Append(half.ToString("x")).Append("\r\n").Append(Encoding.Latin1.GetString(body, 0, half)).Append("\r\n");
        chunked.Append((body.Length - half).ToString("x")).Append("\r\n").Append(Encoding.Latin1.GetString(body, half, body.Length - half)).Append("\r\n");
        chunked.Append("0\r\n\r\n");

        var response = TrackerResponse.Parse(Http("Transfer-Encoding: chunked\r\n", Encoding.Latin1.GetBytes(chunked.ToString())));

        Assert.NotNull(response.Dictionary);
        Assert.Equal(1800, response.Dictionary!.GetInteger("interval"));
    }

    [Fact]
    public void DetectsRedirect()
    {
        var data = Encoding.Latin1.GetBytes("HTTP/1.1 302 Found\r\nLocation: http://other/announce\r\nContent-Length: 0\r\n\r\n");
        var response = TrackerResponse.Parse(data);

        Assert.Equal(302, response.StatusCode);
        Assert.True(response.IsRedirect);
        Assert.Equal("http://other/announce", response.Location);
        Assert.Null(response.Dictionary);
    }

    [Fact]
    public void ParsesFailureReason()
    {
        var body = new BencodeDictionary().Set("failure reason", "torrent not registered").ToBytes();
        var response = TrackerResponse.Parse(Http(string.Empty, body));
        var announce = AnnounceResponse.Parse(response.Dictionary!);

        Assert.True(announce.HasFailure);
        Assert.Equal("torrent not registered", announce.FailureReason);
    }

    [Fact]
    public void ParsesCompactPeers()
    {
        var peers = new byte[12];
        new byte[] { 1, 2, 3, 4 }.CopyTo(peers, 0);
        BinaryPrimitives.WriteUInt16BigEndian(peers.AsSpan(4), 6881);
        new byte[] { 5, 6, 7, 8 }.CopyTo(peers, 6);
        BinaryPrimitives.WriteUInt16BigEndian(peers.AsSpan(10), 80);

        var body = new BencodeDictionary().Set("interval", 900).Set("peers", new BencodeString(peers)).ToBytes();
        var announce = AnnounceResponse.Parse(TrackerResponse.Parse(Http(string.Empty, body)).Dictionary!);

        Assert.Equal(2, announce.Peers.Count);
        Assert.Equal("1.2.3.4", announce.Peers[0].Ip);
        Assert.Equal(6881, announce.Peers[0].Port);
        Assert.Equal("5.6.7.8", announce.Peers[1].Ip);
        Assert.Equal(80, announce.Peers[1].Port);
    }

    [Fact]
    public void ParsesDictionaryPeers()
    {
        var peerList = new BencodeList(
        [
            new BencodeDictionary().Set("ip", "10.0.0.1").Set("port", 51413).Set("peer id", "-UT3320-abcdefghijkl"),
        ]);
        var body = new BencodeDictionary().Set("interval", 600).Set("peers", peerList).ToBytes();
        var announce = AnnounceResponse.Parse(TrackerResponse.Parse(Http(string.Empty, body)).Dictionary!);

        Assert.Single(announce.Peers);
        Assert.Equal("10.0.0.1", announce.Peers[0].Ip);
        Assert.Equal(51413, announce.Peers[0].Port);
        Assert.Equal("-UT3320-abcdefghijkl", announce.Peers[0].PeerId);
        Assert.Equal(600, announce.Interval);
    }

    [Fact]
    public void CollectsExtraKeysForLogging()
    {
        var body = new BencodeDictionary()
            .Set("interval", 1800)
            .Set("min interval", 900)
            .Set("tracker id", "abc")
            .ToBytes();
        var announce = AnnounceResponse.Parse(TrackerResponse.Parse(Http(string.Empty, body)).Dictionary!);

        Assert.Equal(900, announce.MinInterval);
        Assert.Equal("abc", announce.TrackerId);
        Assert.True(announce.ExtraKeys.ContainsKey("interval"));
        Assert.False(announce.ExtraKeys.ContainsKey("peers"));
    }

    [Fact]
    public void KeepsHeadersAccessible()
    {
        var response = TrackerResponse.Parse(Http("Content-Type: text/plain; charset=utf-8\r\nX-Tracker: demo\r\n", SampleAnnounceBody()));
        Assert.Equal("demo", response.Headers["x-tracker"]);
        Assert.Contains("charset=utf-8", response.Headers["content-type"], StringComparison.Ordinal);
    }
}
