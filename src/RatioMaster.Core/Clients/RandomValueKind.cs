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
        /// A random 31-bit integer as lower-case hexadecimal. With no length it has no leading zeros, so the
        /// length varies: Transmission's announce key, <c>tr_rand_int(INT_MAX)</c> printed with "%x"
        /// (announcer-http.c). With a length it is zero-padded to it: rTorrent's key, <c>random()</c> plus one
        /// printed with setw(8) and setfill('0'), whose first digit is 0-7 for all but one of its 2^31 values.
        /// </summary>
        HexRange,

        /// <summary>
        /// BitTornado's mapbase64 alphabet: digits, upper and lower case letters, '.' and '-'. BitTornado fills
        /// its peer id after the version prefix, and its six-character announce key, with one of these 64
        /// characters per hash byte (byte &amp; 0x3F). Every character is URL-safe, so the value is never
        /// percent-encoded.
        /// </summary>
        BitTornadoBase64,

        /// <summary>
        /// Arbitrary bytes 0..255, meant to be percent-encoded. Unlike <see cref="Random"/> the zero byte is
        /// included: rTorrent's libtorrent fills the peer id tail with whole random bytes (generate_random,
        /// ::random() truncated to a char up to 0.13.6, an 8-bit independent_bits_engine since).
        /// </summary>
        AnyByte,

        /// <summary>
        /// A random 16-bit value, 0..65535, in decimal with no leading zeros, so the length varies. BitComet's announce
        /// key: two random bytes read as a ushort when the session starts. The spec's length is ignored.
        /// </summary>
        UInt16Decimal,

        /// <summary>
        /// uTorrent's and BitTorrent's peer id tail: bytes 1..255 like <see cref="Random"/>, except the last two,
        /// which are the first two bytes of the SHA-1 of the peer id before them, prefix included. The peer id
        /// generator of uTorrent 3.6.0.47254 and BitTorrent 7.11.0.47255 builds it that way. The spec's length counts
        /// the two check bytes.
        /// </summary>
        UTorrentChecksum,
    }
}
