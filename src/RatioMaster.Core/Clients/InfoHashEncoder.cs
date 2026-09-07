namespace RatioMaster.Core.Clients
{
    /// <summary>Percent-encodes a torrent info hash for the tracker "info_hash" parameter.</summary>
    public static class InfoHashEncoder
    {
        /// <summary>Percent-encodes the 20 raw info-hash bytes (mirrors the old RM.HashUrlEncode).</summary>
        public static string Encode(ReadOnlySpan<byte> infoHash, bool upperCase) => PercentEncoding.Encode(infoHash, upperCase);

        /// <summary>Percent-encodes an info hash given as a hex string.</summary>
        public static string EncodeHex(string infoHashHex, bool upperCase)
        {
            ArgumentNullException.ThrowIfNull(infoHashHex);
            return PercentEncoding.Encode(Convert.FromHexString(infoHashHex), upperCase);
        }
    }
}
