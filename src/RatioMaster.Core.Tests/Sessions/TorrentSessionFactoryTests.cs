namespace RatioMaster.Core.Tests.Sessions
{
    using System.Globalization;
    using System.Text;

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
            var identity = CreateFactory().CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" });

            Assert.Equal(ClientIdentitySource.Generated, identity.Source);
            Assert.StartsWith("-UT360S-", identity.PeerId, StringComparison.Ordinal);
            Assert.Equal("200", identity.NumWant);
        }

        [Fact]
        public void UsesCustomValuesAndFillsBlanksFromTheGenerator()
        {
            var settings = new TorrentSettings
            {
                ClientName = "uTorrent 3.6.0",
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

        /// <summary>
        /// A real client listens on one port for all of its torrents and keeps it (issue #56), so every torrent of a
        /// client announces the same port, picked once by that client's own rules.
        /// </summary>
        [Fact]
        public void TheTorrentsOfAClientShareItsPort()
        {
            var factory = CreateFactory();
            var settings = new TorrentSettings { ClientName = "uTorrent 3.6.0" };

            var first = factory.CreateIdentity(settings);
            var second = factory.CreateIdentity(settings);

            Assert.Equal(first.Port, second.Port);
            Assert.InRange(int.Parse(first.Port, CultureInfo.InvariantCulture), 10000, 64999); // uTorrent's own range
            Assert.NotEqual(first.PeerId, second.PeerId); // the rest stays each torrent's own
        }

        [Fact]
        public void EachClientHasAPortOfItsOwn()
        {
            var factory = CreateFactory();

            var utorrent = factory.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" });
            var qbittorrent = factory.CreateIdentity(new TorrentSettings { ClientName = "qBittorrent 5.2.4" });

            Assert.Equal(["qBittorrent 5.2.4", "uTorrent 3.6.0"], factory.Ports.Snapshot().Keys.Order(StringComparer.Ordinal));
            Assert.NotEqual(utorrent.Port, qbittorrent.Port);
        }

        [Fact]
        public void AClientKeepsItsPortAfterARestart()
        {
            var factory = CreateFactory();
            factory.Ports.Load(new Dictionary<string, string> { ["uTorrent 3.6.0"] = "35443" });

            Assert.Equal("35443", factory.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" }).Port);
        }

        [Fact]
        public void APortTypedInWinsAndALeftBlankOneIsTheClients()
        {
            var factory = CreateFactory();
            factory.Ports.Load(new Dictionary<string, string> { ["uTorrent 3.6.0"] = "35443" });
            var custom = new TorrentSettings { ClientName = "uTorrent 3.6.0", IdentityMode = IdentityMode.Custom, CustomPort = "45000" };

            Assert.Equal("45000", factory.CreateIdentity(custom).Port);
            Assert.Equal("35443", factory.CreateIdentity(custom with { CustomPort = " " }).Port);
        }

        [Fact]
        public void TheBookSaysWhenAClientGetsItsPortAndDropsSavedValuesThatAreNoPort()
        {
            var factory = CreateFactory();
            factory.Ports.Load(new Dictionary<string, string> { ["uTorrent 3.6.0"] = "nope", ["Deluge 2.2.0"] = "70000" });
            var changes = 0;
            factory.Ports.Changed += (_, _) => changes++;

            factory.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" });
            factory.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" });

            Assert.Equal(1, changes);
            var ports = factory.Ports.Snapshot();
            Assert.NotEqual("nope", ports["uTorrent 3.6.0"]);
            Assert.False(ports.ContainsKey("Deluge 2.2.0"));
        }

        /// <summary>The port of the running client is the one all of its torrents announce, also after it has quit.</summary>
        [Fact]
        public void APortCopiedFromTheRunningClientBecomesTheClientsPort()
        {
            var memory = Encoding.ASCII.GetBytes(new string('.', 70_000)
                + "GET /announce?info_hash=%aa&peer_id=-UT360S-abc123&port=54321&uploaded=0&downloaded=0&key=DEADBEEF&numwant=175&compact=1 HTTP/1.1");
            var running = CreateFactory(new FakeProcessMemoryScanner(memory));
            running.Ports.Load(new Dictionary<string, string> { ["uTorrent 3.6.0"] = "35443" }); // picked before it ran
            var changes = 0;
            running.Ports.Changed += (_, _) => changes++;

            var copied = running.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" });

            Assert.Equal(ClientIdentitySource.CopiedFromProcess, copied.Source);
            Assert.Equal("54321", copied.Port);
            Assert.Equal("54321", running.Ports.Snapshot()["uTorrent 3.6.0"]);
            Assert.Equal(1, changes);

            var afterItQuit = CreateFactory();
            afterItQuit.Ports.Load(running.Ports.Snapshot());
            Assert.Equal("54321", afterItQuit.CreateIdentity(new TorrentSettings { ClientName = "uTorrent 3.6.0" }).Port);
        }

        [Fact]
        public void CreatesASessionForTheResolvedProfile()
        {
            var session = CreateFactory().Create(Descriptor, new TorrentSettings { ClientName = "Deluge 2.1.1" });

            Assert.Equal("Deluge 2.1.1", session.Profile.Name);
            Assert.Equal(TorrentSessionState.Idle, session.State);
            Assert.StartsWith("-DE211s-", session.Identity.PeerId, StringComparison.Ordinal);
        }

        [Fact]
        public void UnknownClientNameFallsBackToTheCatalogDefault()
        {
            var session = CreateFactory().Create(Descriptor, new TorrentSettings { ClientName = "Nope 1.0" });
            Assert.Equal(Catalog.DefaultName, session.Profile.Name);
        }

        private static TorrentSessionFactory CreateFactory(IProcessMemoryScanner? memory = null) => new(
            Catalog,
            new FakeTrackerClient(),
            new ClientValueScanner(memory ?? NullProcessMemoryScanner.Instance),
            new FakeLocalIpProvider(),
            new DeterministicRandomSource(99));
    }
}
