namespace RatioMaster.Core.Tests.MemoryScan
{
    using System.Text;

    using RatioMaster.Core.Clients;
    using RatioMaster.Core.MemoryScan;
    using RatioMaster.Core.Tests.Fakes;

    public class ClientValueScannerTests
    {
        private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

        private static ClientIdentity Fallback => new()
        {
            PeerId = "-FALLBACK-",
            Key = "FALLBACK",
            Port = "1",
            NumWant = "200",
        };

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public void CopiesTheValuesOutOfTheRunningClient()
        {
            var memory = MemoryWith(
                "GET /announce?info_hash=%aa&peer_id=-UT360S-abc123&port=54321&uploaded=0&downloaded=0&key=DEADBEEF&numwant=175&compact=1 HTTP/1.1");
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

            var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback);

            Assert.NotNull(identity);
            Assert.Equal("-UT360S-abc123", identity!.PeerId);
            Assert.Equal("DEADBEEF", identity.Key);
            Assert.Equal("54321", identity.Port);
            Assert.Equal("175", identity.NumWant);
            Assert.Equal(ClientIdentitySource.CopiedFromProcess, identity.Source);
        }

        [Fact]
        public void FallsBackForFieldsThatAreNotPresent()
        {
            var memory = MemoryWith("&peer_id=-UT360S-onlypeer&compact=1 x", padding: 0);
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

            var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback);

            Assert.NotNull(identity);
            Assert.Equal("-UT360S-onlypeer", identity!.PeerId);
            Assert.Equal("FALLBACK", identity.Key);
            Assert.Equal("1", identity.Port);
            Assert.Equal("200", identity.NumWant);
        }

        [Fact]
        public void UsesTheProfileDefaultWhenNumWantIsNotANumber()
        {
            var memory = MemoryWith("&peer_id=-UT360S-abc&numwant=lots&compact=1 x", padding: 0);
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

            var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback);

            Assert.Equal("200", identity!.NumWant);
        }

        [Fact]
        public void ReturnsNullWhenTheMarkerIsNotFound()
        {
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(Encoding.ASCII.GetBytes("nothing here")));
            Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback));
        }

        [Fact]
        public void ReturnsNullWhenTheProcessIsNotRunning()
        {
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner([], canOpen: false));
            var messages = new List<string>();

            Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback, messages.Add));
            Assert.Contains(messages, m => m.Contains("No uTorrent process found", StringComparison.Ordinal));
        }

        [Fact]
        public void ReturnsNullForProfilesThatCannotBeScanned()
        {
            var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(Encoding.ASCII.GetBytes("&peer_id=-DE1200-x&")));
            Assert.Null(scanner.TryScan(Catalog.GetByName("Deluge 2.1.1"), Fallback));
        }

        [Fact]
        public void ReturnsNullWhenThePlatformDoesNotSupportScanning()
        {
            var scanner = new ClientValueScanner(NullProcessMemoryScanner.Instance);
            Assert.False(scanner.IsSupported);
            Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback));
        }

        /// <summary>Every torrent that starts looks for its client, so a thousand of them must not search a thousand times.</summary>
        [Fact]
        public void AClientThatIsNotRunningIsLookedForAgainOnlyAfterAMinute()
        {
            var clock = new FakeClock();
            var process = new FakeProcessMemoryScanner([], canOpen: false);
            var scanner = new ClientValueScanner(process, clock);
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            var messages = new List<string>();

            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Null(scanner.TryScan(profile, Fallback, messages.Add));
            clock.Advance(TimeSpan.FromSeconds(ClientValueScanner.MissWaitSeconds - 1));
            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal(1, process.Opens);
            Assert.Equal(["Not looking for the uTorrent process again yet: a search less than a minute ago found nothing."], messages);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal(2, process.Opens);
        }

        [Fact]
        public void AClientWhoseValuesAreNotFoundIsSearchedAgainOnlyAfterAMinute()
        {
            var clock = new FakeClock();
            var process = new FakeProcessMemoryScanner(Encoding.ASCII.GetBytes("nothing here"));
            var scanner = new ClientValueScanner(process, clock);
            var profile = Catalog.GetByName("uTorrent 3.6.0");

            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal(1, process.Opens);

            clock.Advance(TimeSpan.FromSeconds(ClientValueScanner.MissWaitSeconds));
            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal(2, process.Opens);
        }

        /// <summary>A running client's values are copied fresh for every torrent, as before.</summary>
        [Fact]
        public void AClientThatWasFoundIsSearchedEveryTime()
        {
            var process = new FakeProcessMemoryScanner(MemoryWith("&peer_id=-UT360S-abc&key=DEADBEEF&compact=1 x", padding: 0));
            var scanner = new ClientValueScanner(process, new FakeClock());
            var profile = Catalog.GetByName("uTorrent 3.6.0");

            Assert.NotNull(scanner.TryScan(profile, Fallback));
            Assert.NotNull(scanner.TryScan(profile, Fallback));
            Assert.Equal(2, process.Opens);
        }

        /// <summary>The user starts the client and asks for its values right away: that search cannot wait for the minute.</summary>
        [Fact]
        public void ASearchThatIgnoresTheRecentMissLooksAtOnceAndEndsTheWait()
        {
            var process = new FakeProcessMemoryScanner(MemoryWith("&peer_id=-UT360S-abc&key=DEADBEEF&compact=1 x", padding: 0), canOpen: false);
            var scanner = new ClientValueScanner(process, new FakeClock());
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            Assert.Null(scanner.TryScan(profile, Fallback));

            process.CanOpen = true;
            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal("-UT360S-abc", scanner.TryScan(profile, Fallback, ignoreRecentMiss: true)?.PeerId);
            Assert.Equal("-UT360S-abc", scanner.TryScan(profile, Fallback)?.PeerId);
            Assert.Equal(3, process.Opens);
        }

        [Fact]
        public void AClockSetBackDoesNotStretchTheWait()
        {
            var clock = new FakeClock();
            var process = new FakeProcessMemoryScanner([], canOpen: false);
            var scanner = new ClientValueScanner(process, clock);
            var profile = Catalog.GetByName("uTorrent 3.6.0");
            Assert.Null(scanner.TryScan(profile, Fallback));

            clock.Advance(TimeSpan.FromHours(-1));
            Assert.Null(scanner.TryScan(profile, Fallback));

            Assert.Equal(2, process.Opens);
        }

        [Fact]
        public void EachClientWaitsForItsOwnMiss()
        {
            var process = new FakeProcessMemoryScanner([], canOpen: false);
            var scanner = new ClientValueScanner(process, new FakeClock());

            Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.6.0"), Fallback));
            Assert.Null(scanner.TryScan(Catalog.GetByName("BitComet 1.20"), Fallback));

            Assert.Equal(2, process.Opens);
        }

        /// <summary>
        /// A start on the UI thread and Regenerate on the thread pool can search at the same time. A slow search that began
        /// first and found nothing must not bring the wait back after a newer one found the client.
        /// </summary>
        [Fact]
        public async Task AnOlderMissThatEndsLastDoesNotUndoANewerFind()
        {
            var client = new FakeProcessMemoryScanner(MemoryWith("&peer_id=-UT360S-abc&key=DEADBEEF&compact=1 x", padding: 0), canOpen: false);
            using var process = new FirstSearchWaits(client);
            var scanner = new ClientValueScanner(process, new FakeClock());
            var profile = Catalog.GetByName("uTorrent 3.6.0");

            var older = Task.Run(() => scanner.TryScan(profile, Fallback), Ct);
            Assert.True(process.Entered.Wait(TimeSpan.FromSeconds(10), Ct));
            client.CanOpen = true;
            Assert.NotNull(scanner.TryScan(profile, Fallback, ignoreRecentMiss: true));
            process.Release.Set();
            Assert.Null(await older);

            Assert.NotNull(scanner.TryScan(profile, Fallback));
            Assert.Equal(3, client.Opens);
        }

        /// <summary>The other way round: the client quit between two searches, and the older find that ends last does not end the wait.</summary>
        [Fact]
        public async Task AnOlderFindThatEndsLastDoesNotUndoANewerMiss()
        {
            var client = new FakeProcessMemoryScanner(MemoryWith("&peer_id=-UT360S-abc&key=DEADBEEF&compact=1 x", padding: 0));
            using var process = new FirstSearchWaits(client);
            var scanner = new ClientValueScanner(process, new FakeClock());
            var profile = Catalog.GetByName("uTorrent 3.6.0");

            var older = Task.Run(() => scanner.TryScan(profile, Fallback), Ct);
            Assert.True(process.Entered.Wait(TimeSpan.FromSeconds(10), Ct));
            client.CanOpen = false;
            Assert.Null(scanner.TryScan(profile, Fallback, ignoreRecentMiss: true));
            process.Release.Set();
            Assert.NotNull(await older);

            Assert.Null(scanner.TryScan(profile, Fallback));
            Assert.Equal(2, client.Opens);
        }

        private static byte[] MemoryWith(string announce, int padding = 70_000)
        {
            var text = new string('.', padding) + announce;
            return Encoding.ASCII.GetBytes(text);
        }

        /// <summary>Opens the client like <paramref name="client"/>, but the first search then waits until the test lets it go on.</summary>
        private sealed class FirstSearchWaits(IProcessMemoryScanner client) : IProcessMemoryScanner, IDisposable
        {
            private int opens;

            public bool IsSupported => true;

            public ManualResetEventSlim Entered { get; } = new();

            public ManualResetEventSlim Release { get; } = new();

            public ProcessMemorySession? Open(string processName)
            {
                var session = client.Open(processName);
                if (Interlocked.Increment(ref this.opens) == 1)
                {
                    this.Entered.Set();
                    this.Release.Wait();
                }

                return session;
            }

            public void Dispose()
            {
                this.Entered.Dispose();
                this.Release.Dispose();
            }
        }
    }
}
