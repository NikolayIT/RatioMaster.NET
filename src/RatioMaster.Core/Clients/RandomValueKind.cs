namespace RatioMaster.Core.Clients
{
    /// <summary>The character set a random client value is drawn from (mirrors the old GenerateIdString keyType).</summary>
    public enum RandomValueKind
    {
        /// <summary>Letters and digits: a-z, A-Z, 0-9.</summary>
        Alphanumeric,

        /// <summary>Digits only: 0-9.</summary>
        Numeric,

        /// <summary>Arbitrary bytes 0..254, meant to be percent-encoded.</summary>
        Random,

        /// <summary>Upper-case hexadecimal: 0-9, A-F.</summary>
        Hex,

        /// <summary>
        /// libtorrent's url_random alphabet: letters, digits and -_.!~*(). Every character is URL-safe,
        /// so the value is never percent-encoded. Used by qBittorrent and other libtorrent clients.
        /// </summary>
        UrlSafe,
    }
}
