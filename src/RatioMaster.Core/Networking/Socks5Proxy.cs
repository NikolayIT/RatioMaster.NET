namespace RatioMaster.Core.Networking
{
    using System.Net;
    using System.Text;

    /// <summary>SOCKS5 client handshake (no-auth and RFC 1929 username/password) over an already-connected stream.</summary>
    internal static class Socks5Proxy
    {
        private const byte Version = 0x05;
        private const byte NoAuth = 0x00;
        private const byte UserPassAuth = 0x02;
        private const byte NoAcceptableMethods = 0xFF;
        private const byte CommandConnect = 0x01;
        private const byte AddressTypeIpv4 = 0x01;
        private const byte AddressTypeDomain = 0x03;
        private const byte AddressTypeIpv6 = 0x04;

        public static async Task ConnectAsync(Stream stream, string host, int port, ProxySettings proxy, CancellationToken cancellationToken)
        {
            await NegotiateAuthenticationAsync(stream, proxy, cancellationToken).ConfigureAwait(false);
            await SendConnectRequestAsync(stream, host, port, cancellationToken).ConfigureAwait(false);
            await ReadConnectReplyAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task NegotiateAuthenticationAsync(Stream stream, ProxySettings proxy, CancellationToken cancellationToken)
        {
            var offerUserPass = proxy.HasCredentials;
            var greeting = offerUserPass
                ? new byte[] { Version, 0x02, NoAuth, UserPassAuth }
                : [Version, 0x01, NoAuth];
            await stream.WriteAsync(greeting, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            var method = new byte[2];
            await stream.ReadExactlyAsync(method, cancellationToken).ConfigureAwait(false);
            if (method[0] != Version)
            {
                throw new ProxyException("SOCKS5 proxy returned an unexpected version.");
            }

            switch (method[1])
            {
                case NoAuth:
                    return;

                case UserPassAuth when offerUserPass:
                    await AuthenticateUserPassAsync(stream, proxy, cancellationToken).ConfigureAwait(false);
                    return;

                case NoAcceptableMethods:
                    throw new ProxyException("SOCKS5 proxy rejected all offered authentication methods.");

                default:
                    throw new ProxyException($"SOCKS5 proxy selected an unsupported authentication method 0x{method[1]:X2}.");
            }
        }

        private static async Task AuthenticateUserPassAsync(Stream stream, ProxySettings proxy, CancellationToken cancellationToken)
        {
            var user = Encoding.Latin1.GetBytes(proxy.Username);
            var pass = Encoding.Latin1.GetBytes(proxy.Password);
            if (user.Length > 255 || pass.Length > 255)
            {
                throw new ProxyException("SOCKS5 username or password is too long (max 255 bytes).");
            }

            var request = new byte[3 + user.Length + pass.Length];
            var i = 0;
            request[i++] = 0x01; // auth sub-negotiation version
            request[i++] = (byte)user.Length;
            Array.Copy(user, 0, request, i, user.Length);
            i += user.Length;
            request[i++] = (byte)pass.Length;
            Array.Copy(pass, 0, request, i, pass.Length);

            await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            var reply = new byte[2];
            await stream.ReadExactlyAsync(reply, cancellationToken).ConfigureAwait(false);
            if (reply[1] != 0x00)
            {
                throw new ProxyException("SOCKS5 proxy rejected the username/password.");
            }
        }

        private static async Task SendConnectRequestAsync(Stream stream, string host, int port, CancellationToken cancellationToken)
        {
            var request = new List<byte> { Version, CommandConnect, 0x00 };
            if (IPAddress.TryParse(host, out var ip))
            {
                var bytes = ip.GetAddressBytes();
                request.Add(bytes.Length == 4 ? AddressTypeIpv4 : AddressTypeIpv6);
                request.AddRange(bytes);
            }
            else
            {
                var hostBytes = Encoding.Latin1.GetBytes(host);
                if (hostBytes.Length > 255)
                {
                    throw new ProxyException("SOCKS5 host name is too long (max 255 bytes).");
                }

                request.Add(AddressTypeDomain);
                request.Add((byte)hostBytes.Length);
                request.AddRange(hostBytes);
            }

            request.Add((byte)(port >> 8));
            request.Add((byte)(port & 0xFF));

            await stream.WriteAsync(request.ToArray(), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task ReadConnectReplyAsync(Stream stream, CancellationToken cancellationToken)
        {
            var head = new byte[4];
            await stream.ReadExactlyAsync(head, cancellationToken).ConfigureAwait(false);
            if (head[0] != Version)
            {
                throw new ProxyException("SOCKS5 proxy returned an unexpected version in the reply.");
            }

            if (head[1] != 0x00)
            {
                throw new ProxyException($"SOCKS5 proxy refused the connection (status 0x{head[1]:X2}).");
            }

            var toSkip = head[3] switch
            {
                AddressTypeIpv4 => 4 + 2,
                AddressTypeIpv6 => 16 + 2,
                AddressTypeDomain => -1, // read length byte first
                _ => throw new ProxyException($"SOCKS5 proxy returned an unknown address type 0x{head[3]:X2}."),
            };

            if (toSkip < 0)
            {
                var lengthByte = new byte[1];
                await stream.ReadExactlyAsync(lengthByte, cancellationToken).ConfigureAwait(false);
                toSkip = lengthByte[0] + 2;
            }

            if (toSkip > 0)
            {
                var bound = new byte[toSkip];
                await stream.ReadExactlyAsync(bound, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
