namespace RatioMaster.Core.Networking
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>SOCKS4 and SOCKS4a client handshake over an already-connected stream.</summary>
    internal static class Socks4Proxy
    {
        private const byte Version = 0x04;
        private const byte CommandConnect = 0x01;
        private const byte RequestGranted = 0x5A;

        public static async Task ConnectAsync(Stream stream, string host, int port, bool socks4a, ProxySettings proxy, CancellationToken cancellationToken)
        {
            var request = new List<byte> { Version, CommandConnect, (byte)(port >> 8), (byte)(port & 0xFF) };

            byte[]? hostBytes = null;
            if (socks4a && !IPAddress.TryParse(host, out _))
            {
                // SOCKS4a: use an invalid 0.0.0.x IP and send the host name after the user id.
                request.AddRange([0x00, 0x00, 0x00, 0x01]);
                hostBytes = Encoding.Latin1.GetBytes(host);
            }
            else
            {
                var address = await ResolveIpv4Async(host, cancellationToken).ConfigureAwait(false);
                request.AddRange(address.GetAddressBytes());
            }

            if (proxy.Username.Length > 0)
            {
                request.AddRange(Encoding.Latin1.GetBytes(proxy.Username));
            }

            request.Add(0x00); // user id terminator

            if (hostBytes is not null)
            {
                request.AddRange(hostBytes);
                request.Add(0x00); // host name terminator
            }

            await stream.WriteAsync(request.ToArray(), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            var reply = new byte[8];
            await stream.ReadExactlyAsync(reply, cancellationToken).ConfigureAwait(false);
            if (reply[1] != RequestGranted)
            {
                throw new ProxyException($"SOCKS4 proxy refused the connection (status 0x{reply[1]:X2}).");
            }
        }

        private static async Task<IPAddress> ResolveIpv4Async(string host, CancellationToken cancellationToken)
        {
            if (IPAddress.TryParse(host, out var parsed) && parsed.AddressFamily == AddressFamily.InterNetwork)
            {
                return parsed;
            }

            var addresses = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
            return addresses.Length > 0
                ? addresses[0]
                : throw new ProxyException($"SOCKS4 could not resolve an IPv4 address for '{host}'.");
        }
    }
}
