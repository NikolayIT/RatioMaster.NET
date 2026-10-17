namespace RatioMaster.Core.Tests.Networking
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    using RatioMaster.Core.Networking;

    public class PeerListenerTests
    {
        private const string PeerId = "-RM1000-abcdefghijkl";
        private const string OtherPeerId = "-UT360S-zyxwvutsrqpo";

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task RepliesToAMatchingHandshake()
        {
            var infoHash = MakeInfoHash(1);
            var port = GetFreePort();
            await using var registration = new PeerListenerPool().Join(port, infoHash, PeerId);
            Assert.NotNull(registration);

            var response = await HandshakeAsync(port, infoHash);

            Assert.Equal(19, response[0]);
            Assert.Equal("BitTorrent protocol", Encoding.ASCII.GetString(response, 1, 19));
            Assert.Equal(new byte[8], response[20..28]);
            Assert.Equal(infoHash, response[28..48]);
            Assert.Equal(PeerId, Encoding.ASCII.GetString(response, 48, 20));
        }

        /// <summary>
        /// A real client answers for all of its torrents on its one port (issue #56). Each torrent used to open a
        /// listener of its own, so a torrent on a port another one already had could not answer at all.
        /// </summary>
        [Fact]
        public async Task EveryTorrentOnAPortIsAnsweredWithItsOwnPeerId()
        {
            var pool = new PeerListenerPool();
            var port = GetFreePort();
            var first = MakeInfoHash(10);
            var second = MakeInfoHash(50);
            await using var a = pool.Join(port, first, PeerId);
            await using var b = pool.Join(port, second, OtherPeerId);
            Assert.NotNull(a);
            Assert.NotNull(b);

            var toFirst = await HandshakeAsync(port, first);
            var toSecond = await HandshakeAsync(port, second);

            Assert.Equal(first, toFirst[28..48]);
            Assert.Equal(PeerId, Encoding.ASCII.GetString(toFirst, 48, 20));
            Assert.Equal(second, toSecond[28..48]);
            Assert.Equal(OtherPeerId, Encoding.ASCII.GetString(toSecond, 48, 20));
        }

        [Fact]
        public async Task IgnoresAHandshakeForAnotherTorrent()
        {
            var port = GetFreePort();
            await using var registration = new PeerListenerPool().Join(port, MakeInfoHash(2), PeerId);
            Assert.NotNull(registration);

            await AssertNoReplyAsync(port, MakeInfoHash(200));
        }

        [Fact]
        public async Task ThePortStaysOpenUntilTheLastTorrentOnItLeaves()
        {
            var pool = new PeerListenerPool();
            var port = GetFreePort();
            var first = MakeInfoHash(20);
            var second = MakeInfoHash(60);
            var a = pool.Join(port, first, PeerId)!;
            var b = pool.Join(port, second, OtherPeerId)!;

            await a.DisposeAsync();
            Assert.Equal(second, (await HandshakeAsync(port, second))[28..48]);
            await AssertNoReplyAsync(port, first); // the torrent that left is not answered any more

            await b.DisposeAsync();
            using var probe = new TcpListener(IPAddress.Any, port);
            probe.Start(); // the port is free again
            probe.Stop();
        }

        [Fact]
        public async Task APortIsOpenedAgainAfterTheLastTorrentLeftIt()
        {
            var pool = new PeerListenerPool();
            var port = GetFreePort();
            var infoHash = MakeInfoHash(30);
            await pool.Join(port, infoHash, PeerId)!.DisposeAsync();

            await using var again = pool.Join(port, infoHash, OtherPeerId);

            Assert.NotNull(again);
            Assert.Equal(OtherPeerId, Encoding.ASCII.GetString(await HandshakeAsync(port, infoHash), 48, 20));
        }

        [Fact]
        public void JoinReturnsNullAndSaysWhyWhenThePortIsInUse()
        {
            var port = GetFreePort();
            using var blocker = new TcpListener(IPAddress.Any, port);
            blocker.Start();
            try
            {
                var messages = new List<string>();
                Assert.Null(new PeerListenerPool().Join(port, MakeInfoHash(3), PeerId, messages.Add));
                Assert.Contains(messages, m => m.Contains("could not start", StringComparison.Ordinal));
            }
            finally
            {
                blocker.Stop();
            }
        }

        [Fact]
        public async Task LogsWhenATorrentStartsAndStopsListening()
        {
            var messages = new List<string>();
            var registration = new PeerListenerPool().Join(GetFreePort(), MakeInfoHash(4), PeerId, messages.Add)!;

            await registration.DisposeAsync();
            await registration.DisposeAsync(); // leaving twice is harmless

            Assert.Single(messages, m => m.Contains("Started TCP listener", StringComparison.Ordinal));
            Assert.Single(messages, m => m.Contains("TCP listener closed", StringComparison.Ordinal));
        }

        private static async Task<byte[]> HandshakeAsync(int port, byte[] infoHash)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, Ct);
            await using var stream = client.GetStream();
            await stream.WriteAsync(BuildHandshake(infoHash, "-OTHERPEER-1234567890"), Ct);

            var response = new byte[68];
            await stream.ReadExactlyAsync(response, Ct);
            return response;
        }

        private static async Task AssertNoReplyAsync(int port, byte[] infoHash)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, Ct);
            await using var stream = client.GetStream();
            await stream.WriteAsync(BuildHandshake(infoHash, "-OTHERPEER-1234567890"), Ct);

            // The listener must not reply. The connection is closed instead, which surfaces either as a
            // zero-byte read or, on Windows, as a connection reset. Both mean no handshake was returned.
            var response = new byte[68];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                var read = await stream.ReadAsync(response, cts.Token);
                Assert.Equal(0, read);
            }
            catch (IOException)
            {
                // Connection reset by the listener closing without replying: acceptable.
            }
        }

        private static byte[] MakeInfoHash(byte seed)
        {
            var hash = new byte[20];
            for (var i = 0; i < hash.Length; i++)
            {
                hash[i] = (byte)(seed + i);
            }

            return hash;
        }

        private static byte[] BuildHandshake(byte[] infoHash, string peerId)
        {
            const string protocol = "BitTorrent protocol";
            var handshake = new List<byte> { (byte)protocol.Length };
            handshake.AddRange(Encoding.ASCII.GetBytes(protocol));
            handshake.AddRange(new byte[8]);
            handshake.AddRange(infoHash);
            handshake.AddRange(Encoding.ASCII.GetBytes(peerId));
            return handshake.ToArray();
        }

        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }
    }
}
