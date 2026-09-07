namespace RatioMaster.Core.Tests.Tracker
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Tracker;

    public class AnnounceUrlBuilderTests
    {
        private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

        [Fact]
        public void BuildsTheExpectedUTorrentStartedUrl()
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var url = AnnounceUrlBuilder.Build("http://tracker.test/announce", profile.Query, Values(), TrackerEvent.Started);

            Assert.Equal(
                "http://tracker.test/announce?info_hash=%aa%bb&peer_id=-UT3320-xyz&port=12345&uploaded=16384&downloaded=16&left=100&corrupt=0&key=ABCD1234&event=started&numwant=200&compact=1&no_peer_id=1",
                url);
        }

        [Fact]
        public void UpdateEventOmitsTheEventParameter()
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var url = AnnounceUrlBuilder.Build("http://tracker.test/announce", profile.Query, Values(), TrackerEvent.None);
            Assert.Contains("&key=ABCD1234&numwant=200", url, StringComparison.Ordinal);
            Assert.DoesNotContain("event=", url, StringComparison.Ordinal);
        }

        [Fact]
        public void BitCometStartedDropsNatmappedAndLocalIp()
        {
            var profile = Catalog.GetByName("BitComet 1.20");
            var started = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(), TrackerEvent.Started);
            Assert.DoesNotContain("natmapped", started, StringComparison.Ordinal);
            Assert.DoesNotContain("localip", started, StringComparison.Ordinal);

            var update = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(), TrackerEvent.None);
            Assert.Contains("natmapped=1&localip=1.2.3.4", update, StringComparison.Ordinal);
        }

        [Fact]
        public void AbcKeepsTrackerIdOnlyWhenStopping()
        {
            var profile = Catalog.GetByName("ABC 3.1");
            var stopped = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(), TrackerEvent.Stopped);
            Assert.Contains("&trackerid=48", stopped, StringComparison.Ordinal);
            Assert.Contains("event=stopped", stopped, StringComparison.Ordinal);

            var update = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(), TrackerEvent.None);
            Assert.DoesNotContain("trackerid", update, StringComparison.Ordinal);
        }

        [Fact]
        public void ZeroNumWantBecomes200ExceptWhenStopping()
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var update = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values("0"), TrackerEvent.None);
            Assert.Contains("numwant=200", update, StringComparison.Ordinal);

            var stopped = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values("0"), TrackerEvent.Stopped);
            Assert.Contains("numwant=0", stopped, StringComparison.Ordinal);
        }

        /// <summary>Every client we emulate asks for no peers when it leaves, whatever it asks for while running.</summary>
        [Theory]
        [InlineData("200")]
        [InlineData("80")]
        [InlineData("175")]
        public void StoppingAlwaysAsksForNoPeers(string numWant)
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");

            var running = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(numWant), TrackerEvent.None);
            Assert.Contains("numwant=" + numWant, running, StringComparison.Ordinal);

            var stopped = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(numWant), TrackerEvent.Stopped);
            Assert.Contains("numwant=0", stopped, StringComparison.Ordinal);
            Assert.DoesNotContain("numwant=" + numWant + "&", stopped, StringComparison.Ordinal);
        }

        [Fact]
        public void UsesAmpersandWhenTheTrackerAlreadyHasAQuery()
        {
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var url = AnnounceUrlBuilder.Build("http://t/announce?passkey=secret", profile.Query, Values(), TrackerEvent.None);
            Assert.StartsWith("http://t/announce?passkey=secret&info_hash=", url, StringComparison.Ordinal);
        }

        [Fact]
        public void EveryProfileProducesAWellFormedUrlWithNoLeftoverPlaceholders()
        {
            foreach (var profile in Catalog.Profiles)
            {
                foreach (var evt in Enum.GetValues<TrackerEvent>())
                {
                    var url = AnnounceUrlBuilder.Build("http://t/announce", profile.Query, Values(), evt);
                    Assert.DoesNotContain("{", url, StringComparison.Ordinal);
                    Assert.DoesNotContain("}", url, StringComparison.Ordinal);
                    Assert.Contains("info_hash=%aa%bb", url, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void RoundDownFloorsToTheDenominator()
        {
            Assert.Equal(16384, AnnounceMath.RoundDown(20000, AnnounceMath.UploadedDenominator));
            Assert.Equal(0, AnnounceMath.RoundDown(100, AnnounceMath.UploadedDenominator));
            Assert.Equal(48, AnnounceMath.RoundDown(63, AnnounceMath.DownloadedDenominator));
            Assert.Equal(0, AnnounceMath.RoundDown(0, AnnounceMath.UploadedDenominator));
        }

        private static AnnounceValues Values(string numWant = "200") => new()
        {
            InfoHashEncoded = "%aa%bb",
            PeerId = "-UT3320-xyz",
            Port = "12345",
            Uploaded = 16384,
            Downloaded = 16,
            Left = 100,
            Key = "ABCD1234",
            NumWant = numWant,
            LocalIp = "1.2.3.4",
        };
    }
}
