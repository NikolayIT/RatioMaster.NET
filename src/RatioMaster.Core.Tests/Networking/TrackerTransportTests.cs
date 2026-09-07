namespace RatioMaster.Core.Tests.Networking
{
    using System.Net;
    using System.Text;

    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Tests.Fakes;

    public class TrackerTransportTests
    {
        private static readonly TrackerTransport Transport = TrackerTransport.Instance;

        private static CancellationToken Timeout => new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

        [Fact]
        public async Task ConnectsDirectlyAndTransfersData()
        {
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                var request = await LoopbackServer.ReadExactAsync(stream, 4, ct);
                await stream.WriteAsync(request, ct);
            });

            await using var stream = await Transport.ConnectAsync("127.0.0.1", server.Port, useTls: false, ignoreCertificateErrors: false, ProxySettings.None, Timeout);
            await stream.WriteAsync("ping"u8.ToArray(), Timeout);
            var echoed = await LoopbackServer.ReadExactAsync(stream, 4, Timeout);
            Assert.Equal("ping", Encoding.ASCII.GetString(echoed));
        }

        [Fact]
        public async Task HttpConnectSendsCorrectRequestAndTunnels()
        {
            string? requestHeaders = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                requestHeaders = await LoopbackServer.ReadHttpHeadersAsync(stream, ct);
                await stream.WriteAsync("HTTP/1.1 200 Connection established\r\n\r\n"u8.ToArray(), ct);
                var payload = await LoopbackServer.ReadExactAsync(stream, 2, ct);
                await stream.WriteAsync(payload, ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.HttpConnect, Host = "127.0.0.1", Port = server.Port };
            await using var stream = await Transport.ConnectAsync("tracker.test", 6969, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);
            await stream.WriteAsync("hi"u8.ToArray(), Timeout);
            var echoed = await LoopbackServer.ReadExactAsync(stream, 2, Timeout);

            Assert.Equal("hi", Encoding.ASCII.GetString(echoed));
            Assert.NotNull(requestHeaders);
            Assert.StartsWith("CONNECT tracker.test:6969 HTTP/1.1\r\n", requestHeaders, StringComparison.Ordinal);
            Assert.Contains("Host: tracker.test:6969\r\n", requestHeaders, StringComparison.Ordinal);
        }

        [Fact]
        public async Task HttpConnectSendsBasicCredentials()
        {
            string? requestHeaders = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                requestHeaders = await LoopbackServer.ReadHttpHeadersAsync(stream, ct);
                await stream.WriteAsync("HTTP/1.1 200 OK\r\n\r\n"u8.ToArray(), ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.HttpConnect, Host = "127.0.0.1", Port = server.Port, Username = "u", Password = "p" };
            await using var stream = await Transport.ConnectAsync("t.test", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);

            var expected = "Proxy-Authorization: Basic " + Convert.ToBase64String(Encoding.Latin1.GetBytes("u:p"));
            Assert.Contains(expected, requestHeaders, StringComparison.Ordinal);
        }

        [Fact]
        public async Task HttpConnectRefusalThrows()
        {
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                await LoopbackServer.ReadHttpHeadersAsync(stream, ct);
                await stream.WriteAsync("HTTP/1.1 403 Forbidden\r\n\r\n"u8.ToArray(), ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.HttpConnect, Host = "127.0.0.1", Port = server.Port };
            await Assert.ThrowsAsync<ProxyException>(async () =>
                await Transport.ConnectAsync("t.test", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout));
        }

        [Fact]
        public async Task Socks4SendsIpv4RequestAndConnects()
        {
            byte[]? request = null;
            byte[]? userId = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                request = await LoopbackServer.ReadExactAsync(stream, 8, ct); // ver, cmd, port(2), ip(4)
                userId = await LoopbackServer.ReadUntilNulAsync(stream, ct);
                await stream.WriteAsync(new byte[] { 0x00, 0x5A, 0, 0, 0, 0, 0, 0 }, ct);
                await stream.WriteAsync("ok"u8.ToArray(), ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks4, Host = "127.0.0.1", Port = server.Port, Username = "bob" };
            await using var stream = await Transport.ConnectAsync("93.184.216.34", 6881, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);
            var payload = await LoopbackServer.ReadExactAsync(stream, 2, Timeout);

            Assert.Equal("ok", Encoding.ASCII.GetString(payload));
            Assert.NotNull(request);
            Assert.Equal(0x04, request[0]);
            Assert.Equal(0x01, request[1]);
            Assert.Equal(6881, (request[2] << 8) | request[3]);
            Assert.Equal(new byte[] { 93, 184, 216, 34 }, request[4..8]);
            Assert.Equal("bob", Encoding.Latin1.GetString(userId!));
        }

        [Fact]
        public async Task Socks4aSendsHostName()
        {
            byte[]? request = null;
            byte[]? host = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                request = await LoopbackServer.ReadExactAsync(stream, 8, ct);
                await LoopbackServer.ReadUntilNulAsync(stream, ct); // user id (empty)
                host = await LoopbackServer.ReadUntilNulAsync(stream, ct);
                await stream.WriteAsync(new byte[] { 0x00, 0x5A, 0, 0, 0, 0, 0, 0 }, ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks4a, Host = "127.0.0.1", Port = server.Port };
            await using var stream = await Transport.ConnectAsync("tracker.example.org", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);

            Assert.NotNull(request);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, request[4..8]);
            Assert.Equal("tracker.example.org", Encoding.Latin1.GetString(host!));
        }

        [Fact]
        public async Task Socks4RefusalThrows()
        {
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                await LoopbackServer.ReadExactAsync(stream, 8, ct);
                await LoopbackServer.ReadUntilNulAsync(stream, ct);
                await stream.WriteAsync(new byte[] { 0x00, 0x5B, 0, 0, 0, 0, 0, 0 }, ct); // rejected
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks4, Host = "127.0.0.1", Port = server.Port };
            await Assert.ThrowsAsync<ProxyException>(async () =>
                await Transport.ConnectAsync("1.2.3.4", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout));
        }

        [Fact]
        public async Task Socks5NoAuthConnectsWithDomainAddress()
        {
            byte[]? greeting = null;
            byte[]? connectRequest = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                greeting = await LoopbackServer.ReadExactAsync(stream, 3, ct); // ver, nmethods=1, method
                await stream.WriteAsync(new byte[] { 0x05, 0x00 }, ct);

                var head = await LoopbackServer.ReadExactAsync(stream, 5, ct); // ver, cmd, rsv, atyp, len
                var host = await LoopbackServer.ReadExactAsync(stream, head[4], ct);
                var port = await LoopbackServer.ReadExactAsync(stream, 2, ct);
                connectRequest = [.. head, .. host, .. port];
                await stream.WriteAsync(new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, ct);
                await stream.WriteAsync("yo"u8.ToArray(), ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "127.0.0.1", Port = server.Port };
            await using var stream = await Transport.ConnectAsync("host.test", 8080, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);
            var payload = await LoopbackServer.ReadExactAsync(stream, 2, Timeout);

            Assert.Equal("yo", Encoding.ASCII.GetString(payload));
            Assert.Equal(new byte[] { 0x05, 0x01, 0x00 }, greeting);
            Assert.NotNull(connectRequest);
            Assert.Equal(0x05, connectRequest[0]);
            Assert.Equal(0x01, connectRequest[1]);
            Assert.Equal(0x03, connectRequest[3]);
            Assert.Equal((byte)"host.test".Length, connectRequest[4]);
            Assert.Equal("host.test", Encoding.Latin1.GetString(connectRequest[5..^2]));
            Assert.Equal(8080, (connectRequest[^2] << 8) | connectRequest[^1]);
        }

        [Fact]
        public async Task Socks5UsernamePasswordAuthConnects()
        {
            byte[]? greeting = null;
            byte[]? auth = null;
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                greeting = await LoopbackServer.ReadExactAsync(stream, 4, ct); // ver, nmethods=2, 0x00, 0x02
                await stream.WriteAsync(new byte[] { 0x05, 0x02 }, ct); // choose user/pass

                var authHead = await LoopbackServer.ReadExactAsync(stream, 2, ct); // ver, ulen
                var user = await LoopbackServer.ReadExactAsync(stream, authHead[1], ct);
                var plen = await LoopbackServer.ReadExactAsync(stream, 1, ct);
                var pass = await LoopbackServer.ReadExactAsync(stream, plen[0], ct);
                auth = [.. authHead, .. user, .. plen, .. pass];
                await stream.WriteAsync(new byte[] { 0x01, 0x00 }, ct); // auth ok

                var head = await LoopbackServer.ReadExactAsync(stream, 5, ct);
                await LoopbackServer.ReadExactAsync(stream, head[4] + 2, ct);
                await stream.WriteAsync(new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "127.0.0.1", Port = server.Port, Username = "alice", Password = "secret" };
            await using var stream = await Transport.ConnectAsync("host.test", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout);

            Assert.Equal(new byte[] { 0x05, 0x02, 0x00, 0x02 }, greeting);
            Assert.NotNull(auth);
            Assert.Equal(0x01, auth[0]);
            Assert.Equal((byte)"alice".Length, auth[1]);
            Assert.Equal("alice", Encoding.Latin1.GetString(auth[2..7]));
            Assert.Equal("secret", Encoding.Latin1.GetString(auth[8..]));
        }

        [Fact]
        public async Task Socks5AuthFailureThrows()
        {
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                await LoopbackServer.ReadExactAsync(stream, 4, ct);
                await stream.WriteAsync(new byte[] { 0x05, 0x02 }, ct);
                var authHead = await LoopbackServer.ReadExactAsync(stream, 2, ct);
                await LoopbackServer.ReadExactAsync(stream, authHead[1], ct);
                var plen = await LoopbackServer.ReadExactAsync(stream, 1, ct);
                await LoopbackServer.ReadExactAsync(stream, plen[0], ct);
                await stream.WriteAsync(new byte[] { 0x01, 0x01 }, ct); // auth failed
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "127.0.0.1", Port = server.Port, Username = "a", Password = "b" };
            await Assert.ThrowsAsync<ProxyException>(async () =>
                await Transport.ConnectAsync("host.test", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout));
        }

        [Fact]
        public async Task Socks5RejectingAllMethodsThrows()
        {
            await using var server = LoopbackServer.Start(async (stream, ct) =>
            {
                await LoopbackServer.ReadExactAsync(stream, 3, ct);
                await stream.WriteAsync(new byte[] { 0x05, 0xFF }, ct);
            });

            var proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "127.0.0.1", Port = server.Port };
            await Assert.ThrowsAsync<ProxyException>(async () =>
                await Transport.ConnectAsync("host.test", 80, useTls: false, ignoreCertificateErrors: false, proxy, Timeout));
        }

        [Fact]
        public void UsesAPlainIpv4SocketWhenTheSystemHasNoIpv6()
        {
            // With IPv6 disabled at the OS level, creating an InterNetworkV6 socket throws, which used to make
            // every tracker unreachable.
            using var ipv4 = TrackerTransport.CreateSocket(ipv6Supported: false);
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetwork, ipv4.AddressFamily);

            using var dual = TrackerTransport.CreateSocket(ipv6Supported: true);
            Assert.Equal(System.Net.Sockets.AddressFamily.InterNetworkV6, dual.AddressFamily);
            Assert.True(dual.DualMode);
        }
    }
}
