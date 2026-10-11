namespace RatioMaster.Core.Tests.Tracker
{
    using System.Text;

    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Tracker;

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

        [Theory]
        [InlineData("http://t/announce?info_hash=%2D%2e%5F%7e%21&x=1", "/announce?info_hash=%2D%2e%5F%7e%21&x=1")]
        [InlineData("http://t:6969/announce.php?passkey=abc", "/announce.php?passkey=abc")]
        [InlineData("http://t:6969?x=1", "/?x=1")]
        [InlineData("http://t", "/")]
        [InlineData("http://t/a?b#fragment", "/a?b")]
        [InlineData("https://user:pw@t:443/p?q", "/p?q")]
        [InlineData("http://t/a b?ü", "/a%20b?%C3%BC")]
        public void RequestTargetIsTheUrlAsWritten(string url, string expected)
        {
            // Uri.PathAndQuery would have turned %2D %2e %5F %7e into - . _ ~ and changed the client's escaping.
            Assert.Equal(expected, HttpRequestWriter.RequestTarget(url));
        }

        [Theory]
        [InlineData("http://tracker.test/announce", "tracker.test")]
        [InlineData("http://tracker.test:80/announce", "tracker.test")]
        [InlineData("https://tracker.test:443/announce", "tracker.test")]
        [InlineData("http://tracker.test:6969/announce", "tracker.test:6969")]
        [InlineData("https://tracker.test:80/announce", "tracker.test:80")]
        [InlineData("http://[2001:db8::1]:2710/announce", "[2001:db8::1]:2710")]
        public void HostHeaderCarriesThePortUnlessItIsTheSchemesDefault(string url, string expected)
        {
            Assert.Equal(expected, HttpRequestWriter.HostHeader(new Uri(url)));
        }

        [Fact]
        public void HostHeaderLeavesThePortOutForAClientThatDoesNotSendIt()
        {
            // BitComet's own HTTP code: a capture against a tracker on port 8765 said "Host: 127.0.0.1".
            Assert.Equal("tracker.test", HttpRequestWriter.HostHeader(new Uri("http://tracker.test:6969/announce"), withPort: false));
        }

        [Fact]
        public void BuildsRequestTextWithHostAndBlankLineTermination()
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var text = HttpRequestWriter.BuildRequestText("/announce?x=1", "tracker.host", profile);

            Assert.Equal(
                "GET /announce?x=1 HTTP/1.1\r\n" +
                "Host: tracker.host\r\n" +
                "User-Agent: uTorrent/360(113358572)(46828)\r\n" +
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
            var profile = Catalog.GetByName("uTorrent 3.6.0");
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
}
