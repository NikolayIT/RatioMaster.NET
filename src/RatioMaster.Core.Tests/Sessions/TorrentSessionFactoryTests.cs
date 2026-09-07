namespace RatioMaster.Core.Tests.Sessions
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.MemoryScan;
    using RatioMaster.Core.Sessions;
    using RatioMaster.Core.Tests.Fakes;

    public class TorrentSessionFactoryTests
    {
        private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

        private static TorrentDescriptor Descriptor => new()
        {
            InfoHash = new byte[20],
            TotalLength = 1000,
            TrackerUrl = "http://tracker.test/announce",
            Name = "t",
        };

        [Fact]
        public void GeneratesAnIdentityForAutomaticMode()
        {
            var identity = CreateFactory().CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.3.2" });

            Assert.Equal(ClientIdentitySource.Generated, identity.Source);
            Assert.StartsWith("-UT3320-", identity.PeerId, StringComparison.Ordinal);
            Assert.Equal("200", identity.NumWant);
        }

        [Fact]
        public void UsesCustomValuesAndFillsBlanksFromTheGenerator()
        {
            var settings = new TorrentSettings
            {
                ClientName = "uTorrent 3.3.2",
                IdentityMode = IdentityMode.Custom,
                CustomPeerId = "-UT3320-mypeerid",
                CustomPort = "45000",
                CustomKey = null,
                CustomNumWant = "  ",
            };

            var identity = CreateFactory().CreateIdentity(settings);

            Assert.Equal(ClientIdentitySource.Custom, identity.Source);
            Assert.Equal("-UT3320-mypeerid", identity.PeerId);
            Assert.Equal("45000", identity.Port);
            Assert.False(string.IsNullOrWhiteSpace(identity.Key));
            Assert.Equal("200", identity.NumWant);
        }

        [Fact]
        public void CreatesASessionForTheResolvedProfile()
        {
            var session = CreateFactory().Create(Descriptor, new TorrentSettings { ClientName = "Deluge 1.2.0" });

            Assert.Equal("Deluge 1.2.0", session.Profile.Name);
            Assert.Equal(TorrentSessionState.Idle, session.State);
            Assert.StartsWith("-DE1200-", session.Identity.PeerId, StringComparison.Ordinal);
        }

        [Fact]
        public void UnknownClientNameFallsBackToTheCatalogDefault()
        {
            var session = CreateFactory().Create(Descriptor, new TorrentSettings { ClientName = "Nope 1.0" });
            Assert.Equal(Catalog.DefaultName, session.Profile.Name);
        }

        private static TorrentSessionFactory CreateFactory() => new(
            Catalog,
            new FakeTrackerClient(),
            new ClientValueScanner(NullProcessMemoryScanner.Instance),
            new FakeLocalIpProvider(),
            new DeterministicRandomSource(99));
    }
}
