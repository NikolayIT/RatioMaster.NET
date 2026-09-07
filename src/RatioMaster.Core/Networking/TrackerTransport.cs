namespace RatioMaster.Core.Networking
{
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// The default transport: a dual-stack socket to the tracker (or proxy), the SOCKS/HTTP CONNECT handshake,
    /// and optional TLS. This is the managed replacement for the old BytesRoads SocketEx.
    /// </summary>
    public sealed class TrackerTransport : ITrackerTransport
    {
        public static TrackerTransport Instance { get; } = new();

        public async Task<Stream> ConnectAsync(
            string host,
            int port,
            bool useTls,
            bool ignoreCertificateErrors,
            ProxySettings proxy,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(host);
            ArgumentNullException.ThrowIfNull(proxy);

            var firstHopHost = proxy.IsDirect ? host : proxy.Host;
            var firstHopPort = proxy.IsDirect ? port : proxy.Port;

            var socket = await ConnectSocketAsync(firstHopHost, firstHopPort, cancellationToken).ConfigureAwait(false);
            Stream stream = new NetworkStream(socket, ownsSocket: true);
            try
            {
                switch (proxy.Type)
                {
                    case ProxyType.None:
                        break;
                    case ProxyType.HttpConnect:
                        await HttpConnectProxy.ConnectAsync(stream, host, port, proxy, cancellationToken).ConfigureAwait(false);
                        break;
                    case ProxyType.Socks4:
                        await Socks4Proxy.ConnectAsync(stream, host, port, socks4a: false, proxy, cancellationToken).ConfigureAwait(false);
                        break;
                    case ProxyType.Socks4a:
                        await Socks4Proxy.ConnectAsync(stream, host, port, socks4a: true, proxy, cancellationToken).ConfigureAwait(false);
                        break;
                    case ProxyType.Socks5:
                        await Socks5Proxy.ConnectAsync(stream, host, port, proxy, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new ProxyException($"Unsupported proxy type {proxy.Type}.");
                }

                if (useTls)
                {
                    stream = await AuthenticateTlsAsync(stream, host, ignoreCertificateErrors, cancellationToken).ConfigureAwait(false);
                }

                return stream;
            }
            catch
            {
                await stream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// A dual-stack socket so IPv6-only trackers work (DualMode maps IPv4 targets automatically), or a plain
        /// IPv4 socket on machines where IPv6 is switched off, where creating an IPv6 socket fails outright.
        /// </summary>
        internal static Socket CreateSocket(bool ipv6Supported)
        {
            if (!ipv6Supported)
            {
                return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            }

            return new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true,
                NoDelay = true,
            };
        }

        private static async Task<Socket> ConnectSocketAsync(string host, int port, CancellationToken cancellationToken)
        {
            var socket = CreateSocket(Socket.OSSupportsIPv6);
            try
            {
                await socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static async Task<SslStream> AuthenticateTlsAsync(Stream inner, string host, bool ignoreCertificateErrors, CancellationToken cancellationToken)
        {
            var ssl = new SslStream(inner, leaveInnerStreamOpen: false, ValidationCallback(ignoreCertificateErrors));
            try
            {
                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions { TargetHost = host },
                    cancellationToken).ConfigureAwait(false);
                return ssl;
            }
            catch
            {
                await ssl.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static RemoteCertificateValidationCallback? ValidationCallback(bool ignoreCertificateErrors) =>
            ignoreCertificateErrors ? (_, _, _, _) => true : null;
    }
}
