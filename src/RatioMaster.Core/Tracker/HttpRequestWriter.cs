namespace RatioMaster.Core.Tracker
{
    using System.Text;

    using RatioMaster.Core.Clients;

    /// <summary>
    /// Builds the raw HTTP request bytes for a tracker announce, byte-for-byte as the emulated client would.
    /// Headers are joined with CRLF and terminated by a blank line (fixing the old malformed cases).
    /// </summary>
    public static class HttpRequestWriter
    {
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
