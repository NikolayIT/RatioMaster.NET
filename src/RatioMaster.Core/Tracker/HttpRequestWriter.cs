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
        /// <summary>
        /// Gets the value of the Host header: the host, with the port when it is not the scheme's default. That is
        /// what libtorrent, curl, Java, Python's httplib and HTTP itself (RFC 9110 7.2) do, and a tracker on port
        /// 6969 sees "tracker:6969" from every real client.
        /// </summary>
        public static string HostHeader(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);
            return uri.IsDefaultPort ? uri.Host : uri.Host + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
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
    }
}
