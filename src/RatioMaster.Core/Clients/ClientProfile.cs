namespace RatioMaster.Core.Clients
{
    /// <summary>
    /// The emulation of one BitTorrent client version: the HTTP dialect, headers and announce query
    /// used to impersonate it against a tracker. Transcribed from the old TorrentClientFactory.
    /// </summary>
    public sealed record ClientProfile
    {
        /// <summary>Gets the full display name, for example "uTorrent 3.3.2". Unique within the catalog.</summary>
        public required string Name { get; init; }

        /// <summary>Gets the client family, for example "uTorrent".</summary>
        public required string Family { get; init; }

        /// <summary>Gets the version within the family, for example "3.3.2".</summary>
        public required string Version { get; init; }

        /// <summary>Gets "HTTP/1.0" or "HTTP/1.1".</summary>
        public required string HttpProtocol { get; init; }

        /// <summary>Gets a value indicating whether the percent-encoded info hash uses upper-case hex escapes.</summary>
        public bool HashUpperCase { get; init; }

        /// <summary>
        /// Gets the characters other than ASCII letters and digits that the client leaves as they are when it
        /// percent-encodes the info hash and the peer id: "-_.!~*()" for libtorrent, "-._~" for uTorrent and
        /// Transmission, "-" for rTorrent and so on. Empty means every other byte is escaped.
        /// </summary>
        public string Unreserved { get; init; } = string.Empty;

        /// <summary>Gets the spec for the tracker "key" value.</summary>
        public required RandomValueSpec Key { get; init; }

        /// <summary>Gets how often a new key is generated while the torrent runs.</summary>
        public KeyRefreshPolicy KeyRefresh { get; init; } = KeyRefreshPolicy.Never;

        /// <summary>Gets the key lifetime in minutes, used when <see cref="KeyRefresh"/> is timed.</summary>
        public int KeyRefreshMinutes { get; init; } = 10;

        /// <summary>Gets the fixed peer id prefix (may contain literal %xx escapes).</summary>
        public required string PeerIdPrefix { get; init; }

        /// <summary>Gets the spec for the random part appended to <see cref="PeerIdPrefix"/>.</summary>
        public required RandomValueSpec PeerId { get; init; }

        /// <summary>Gets the request header lines in order, with "{host}" where the host goes. No CRLFs.</summary>
        public required IReadOnlyList<string> Headers { get; init; }

        /// <summary>Gets the announce query template with {infohash} {peerid} {port} {uploaded} {downloaded} {left} {event} {numwant} {key} {localip} placeholders.</summary>
        public required string Query { get; init; }

        /// <summary>Gets the default value for numwant.</summary>
        public int DefaultNumWant { get; init; } = 200;

        /// <summary>
        /// Gets the numwant the client sends once it seeds, when that is not <see cref="DefaultNumWant"/>. Vuze asks for
        /// half again as many peers as it has free connection slots, and its connection limit halves for a seed.
        /// </summary>
        public int? SeedingNumWant { get; init; }

        /// <summary>
        /// Gets the ranges the client draws its listen port from on a fresh install, or an empty list for the old
        /// 1025-65534. Vuze, for example, picks 10000-65535 but skips its magnet ports, 45100-45110.
        /// </summary>
        public IReadOnlyList<PortRange> PortRanges { get; init; } = [];

        /// <summary>Gets the memory-scan location, or null when this client cannot be parsed from a running process.</summary>
        public ClientMemoryScanSpec? MemoryScan { get; init; }

        public bool CanScanMemory => this.MemoryScan is not null;
    }
}
