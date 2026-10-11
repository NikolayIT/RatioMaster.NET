namespace RatioMaster.Core.Tracker
{
    using System.Globalization;
    using System.Text;

    using RatioMaster.Core.Clients;

    /// <summary>
    /// Builds the raw HTTP request bytes for a tracker announce, byte-for-byte as the emulated client would.
    /// Headers are joined with CRLF and terminated by a blank line (fixing the old malformed cases).
    /// </summary>
    public static class HttpRequestWriter
    {
        private static readonly char[] PathStarts = ['/', '?', '#'];

        /// <summary>
        /// Gets the request target of an absolute http(s) URL exactly as it is written. Uri.PathAndQuery would turn
        /// %2D, %2E, %5F and %7E back into "-", ".", "_" and "~", and so change the escaping the client profile
        /// chose. A missing path becomes "/", the fragment is dropped, and only characters that cannot stand in a
        /// request line (controls, space, DEL and non-ASCII) are percent-encoded, as UTF-8.
        /// </summary>
        public static string RequestTarget(string absoluteUrl)
        {
            ArgumentException.ThrowIfNullOrEmpty(absoluteUrl);

            var authority = absoluteUrl.IndexOf("://", StringComparison.Ordinal);
            var start = absoluteUrl.IndexOfAny(PathStarts, authority < 0 ? 0 : authority + 3);
            if (start < 0)
            {
                return "/";
            }

            var fragment = absoluteUrl.IndexOf('#', start);
            var target = fragment < 0 ? absoluteUrl[start..] : absoluteUrl[start..fragment];
            if (target.Length == 0 || target[0] != '/')
            {
                target = "/" + target;
            }

            return EscapeForRequestLine(target);
        }

        /// <summary>
        /// Gets the value of the Host header: the host, with the port when it is not the scheme's default. That is
        /// what libtorrent, curl, Java, Python's httplib and HTTP itself (RFC 9110 7.2) do, and a tracker on port
        /// 6969 sees "tracker:6969" from them. A client whose own code writes the host alone (BitComet) passes
        /// <paramref name="withPort"/> false.
        /// </summary>
        public static string HostHeader(Uri uri, bool withPort = true)
        {
            ArgumentNullException.ThrowIfNull(uri);
            return uri.IsDefaultPort || !withPort ? uri.Host : uri.Host + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
        }

        public static string BuildRequestText(string pathAndQuery, string host, ClientProfile profile)
        {
            ArgumentException.ThrowIfNullOrEmpty(pathAndQuery);
            ArgumentException.ThrowIfNullOrEmpty(host);
            ArgumentNullException.ThrowIfNull(profile);

            var builder = new StringBuilder();
            builder.Append("GET ").Append(pathAndQuery).Append(' ').Append(profile.HttpProtocol).Append("\r\n");
            foreach (var header in profile.Headers)
            {
                builder.Append(header.Replace("{host}", host, StringComparison.Ordinal)).Append("\r\n");
            }

            builder.Append("\r\n");
            return builder.ToString();
        }

        public static byte[] BuildRequest(string pathAndQuery, string host, ClientProfile profile) =>
            Encoding.Latin1.GetBytes(BuildRequestText(pathAndQuery, host, profile));

        private static string EscapeForRequestLine(string target)
        {
            if (!target.Any(c => c <= ' ' || c >= '\x7f'))
            {
                return target;
            }

            var builder = new StringBuilder(target.Length + 16);
            foreach (var b in Encoding.UTF8.GetBytes(target))
            {
                if (b <= 0x20 || b >= 0x7f)
                {
                    builder.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append((char)b);
                }
            }

            return builder.ToString();
        }
    }
}
