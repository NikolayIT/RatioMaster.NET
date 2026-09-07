namespace RatioMaster.Core.Tests.Tracker
{
    using System.Security.Authentication;

    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Tracker;

    /// <summary>
    /// The engine treats <see cref="TrackerException"/> as "this announce did not happen, retry later". Anything
    /// else escaping the client would end the session's run loop, so every failure must arrive as that type.
    /// </summary>
    public class TrackerClientTests
    {
        private static readonly ClientProfile Profile = ClientProfileCatalog.Load().GetByName("uTorrent 3.3.2");
        private static readonly byte[] InfoHash = Enumerable.Range(0, 20).Select(i => (byte)(i + 1)).ToArray();

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task ReportsARefusedCertificateAsATrackerException()
        {
            var client = ClientOver(new ThrowingTransport(new AuthenticationException("The remote certificate is invalid.")));

            var error = await Assert.ThrowsAsync<TrackerException>(async () =>
                await client.AnnounceAsync(Profile, "https://tracker.test/announce", Values(), TrackerEvent.Started, ProxySettings.None, false, Ct));

            Assert.Contains("not trusted", error.Message, StringComparison.Ordinal);
            Assert.Contains("Ignore TLS certificate errors", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WrapsUnexpectedTransportFailuresForAnnounceAndScrape()
        {
            var client = ClientOver(new ThrowingTransport(new InvalidOperationException("boom")));

            var announce = await Assert.ThrowsAsync<TrackerException>(async () =>
                await client.AnnounceAsync(Profile, "http://tracker.test/announce", Values(), TrackerEvent.None, ProxySettings.None, false, Ct));
            var scrape = await Assert.ThrowsAsync<TrackerException>(async () =>
                await client.ScrapeAsync(Profile, "http://tracker.test/announce", "%01", InfoHash, ProxySettings.None, false, Ct));

            Assert.Contains("boom", announce.Message, StringComparison.Ordinal);
            Assert.Contains("boom", scrape.Message, StringComparison.Ordinal);
            Assert.IsType<InvalidOperationException>(announce.InnerException);
        }

        [Fact]
        public async Task ReportsAnUnsupportedTrackerUrlWithoutConnecting()
        {
            var transport = new ThrowingTransport(new InvalidOperationException("must not connect"));
            var client = ClientOver(transport);

            var error = await Assert.ThrowsAsync<TrackerException>(async () =>
                await client.AnnounceAsync(Profile, "udp://tracker.test:1337/announce", Values(), TrackerEvent.Started, ProxySettings.None, false, Ct));

            Assert.Contains("udp://", error.Message, StringComparison.Ordinal);
            Assert.Equal(0, transport.Connections);
        }

        [Fact]
        public async Task LetsTheCallersOwnCancellationThrough()
        {
            using var cts = new CancellationTokenSource();
            var client = ClientOver(new HangingTransport());

            var pending = client.AnnounceAsync(Profile, "http://tracker.test/announce", Values(), TrackerEvent.None, ProxySettings.None, false, cts.Token);
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        }

        [Fact]
        public void IgnoringCertificateErrorsKeepsTheInjectedTransport()
        {
            var transport = new ThrowingTransport(new InvalidOperationException("x"));
            var http = new TrackerHttpClient(transport);

            var relaxed = http.WithIgnoredCertificateErrors();

            Assert.NotSame(http, relaxed);
            Assert.Same(relaxed, relaxed.WithIgnoredCertificateErrors());
        }

        private static AnnounceValues Values() => new()
        {
            InfoHashEncoded = InfoHashEncoder.Encode(InfoHash, upperCase: false),
            PeerId = "-UT3320-abcdefghijkl",
            Port = "50000",
            Uploaded = 0,
            Downloaded = 0,
            Left = 1000,
            Key = "KEY12345",
            NumWant = "200",
            LocalIp = "1.2.3.4",
        };

        private static TrackerClient ClientOver(ITrackerTransport transport) =>
            new(new TrackerHttpClient(transport, new TrackerHttpClientOptions { ConnectAttempts = 1 }));

        private sealed class ThrowingTransport(Exception error) : ITrackerTransport
        {
            public int Connections { get; private set; }

            public Task<Stream> ConnectAsync(string host, int port, bool useTls, bool ignoreCertificateErrors, ProxySettings proxy, CancellationToken cancellationToken)
            {
                this.Connections++;
                return Task.FromException<Stream>(error);
            }
        }

        private sealed class HangingTransport : ITrackerTransport
        {
            public async Task<Stream> ConnectAsync(string host, int port, bool useTls, bool ignoreCertificateErrors, ProxySettings proxy, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                throw new InvalidOperationException("unreachable");
            }
        }
    }
}
