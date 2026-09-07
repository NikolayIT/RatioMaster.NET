namespace RatioMaster.Core.Clients
{
    /// <summary>The character set a random client value is drawn from (mirrors the old GenerateIdString keyType).</summary>
    public enum RandomValueKind
    {
        /// <summary>Letters and digits: a-z, A-Z, 0-9.</summary>
        Alphanumeric,

        /// <summary>Digits only: 0-9.</summary>
        Numeric,

        /// <summary>
        /// Arbitrary bytes 1..255, meant to be percent-encoded. The range matches the real clients, which fill
        /// the tail of the peer id from [\x01-\xff]: a NUL would end the string and 0xff has to be reachable.
        /// </summary>
        Random,

        /// <summary>Upper-case hexadecimal: 0-9, A-F.</summary>
        Hex,

        /// <summary>
        /// libtorrent's url_random alphabet: letters, digits and -_.!~*(). Every character is URL-safe,
        /// so the value is never percent-encoded. Used by qBittorrent and other libtorrent clients.
        /// </summary>
        UrlSafe,

        /// <summary>
        /// Transmission's peer id suffix: lower-case base-36 characters whose values sum to a multiple of 36,
        /// the last character being the check digit that makes them do so. Ported from tr_peerIdInit in
        /// libtransmission/session.c, so a tracker that verifies the check digit sees a valid peer id.
        /// </summary>
        TransmissionChecksum,

        /// <summary>
        /// A random 31-bit integer as lower-case hexadecimal with no leading zeros, so the length varies.
        /// Transmission's announce key: <c>tr_rand_int(INT_MAX)</c> printed with "%x" (announcer-http.c).
        /// The spec's length is ignored.
        /// </summary>
        HexRange,
    }
}
