using System.Text;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Tests.Tracker;

public class ScrapeAndRequestTests
{
    private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

    [Theory]
    [InlineData("http://t/announce", "http://t/scrape?info_hash=%aa")]
    [InlineData("http://t/announce?passkey=x", "http://t/scrape?passkey=x&info_hash=%aa")]
    [InlineData("http://t.org/tracker/announce.php", "http://t.org/tracker/scrape.php?info_hash=%aa")]
    public void DerivesScrapeUrlFromAnnounce(string announce, string expected)
    {
        Assert.Equal(expected, ScrapeUrlBuilder.TryBuild(announce, "%aa"));
    }

    [Theory]
    [InlineData("http://t/track")]
    [InlineData("http://t/ann")]
    [InlineData("http://t/")]
    public void ReturnsNullWhenPathDoesNotStartWithAnnounce(string announce)
    {
        Assert.Null(ScrapeUrlBuilder.TryBuild(announce, "%aa"));
    }

    [Fact]
    public void BuildsRequestTextWithHostAndBlankLineTermination()
    {
        var profile = Catalog.GetByName("uTorrent 3.3.2");
        var text = HttpRequestWriter.BuildRequestText("/announce?x=1", "tracker.host", profile);

        Assert.Equal(
            "GET /announce?x=1 HTTP/1.1\r\n" +
            "Host: tracker.host\r\n" +
            "User-Agent: uTorrent/3320(30488)\r\n" +
            "Accept-Encoding: gzip\r\n" +
            "Connection: Close\r\n" +
            "\r\n",
            text);
    }

    [Fact]
    public void NormalizesTheKTorrentHeadersThatUsedBackwardsLineEndings()
    {
        var profile = Catalog.GetByName("KTorrent 2.2.1");

        // The stored header lines carry no embedded line breaks, so the old "\n\r" cannot appear.
        Assert.All(profile.Headers, header => Assert.DoesNotContain('\r', header));
        Assert.All(profile.Headers, header => Assert.DoesNotContain('\n', header));

        var text = HttpRequestWriter.BuildRequestText("/announce", "h", profile);
        Assert.EndsWith("Connection: Keep-Alive\r\n\r\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildsRequestBytesAsLatin1()
    {
        var profile = Catalog.GetByName("uTorrent 3.3.2");
        var bytes = HttpRequestWriter.BuildRequest("/announce", "h", profile);
        Assert.Equal(HttpRequestWriter.BuildRequestText("/announce", "h", profile), Encoding.Latin1.GetString(bytes));
    }

    [Fact]
    public void ScrapeResponseReadsTheStatsForTheInfoHash()
    {
        var hash = new byte[] { 0x01, 0x02, 0x03 };
        var key = Encoding.Latin1.GetString(hash);
        var body = Encoding.Latin1.GetBytes(
            $"d5:filesd{key.Length}:{key}d8:completei10e10:downloadedi55e10:incompletei3eeee");

        var dict = RatioMaster.Core.Bencode.BencodeParser.ParseDictionary(body);
        var scrape = ScrapeResponse.Parse(dict, hash);

        Assert.False(scrape.HasFailure);
        Assert.Equal(10, scrape.Complete);
        Assert.Equal(55, scrape.Downloaded);
        Assert.Equal(3, scrape.Incomplete);
    }

    [Fact]
    public void ScrapeResponseSurfacesFailureReason()
    {
        var body = Encoding.Latin1.GetBytes("d14:failure reason9:not foundee");
        var dict = RatioMaster.Core.Bencode.BencodeParser.ParseDictionary(body);
        var scrape = ScrapeResponse.Parse(dict, [0x01]);
        Assert.True(scrape.HasFailure);
        Assert.Equal("not found", scrape.FailureReason);
    }
}
