namespace RatioMaster.Core.Clients;

/// <summary>
/// The emulation of one BitTorrent client version: the HTTP dialect, headers and announce query
/// used to impersonate it against a tracker. Transcribed from the old TorrentClientFactory.
/// </summary>
public sealed record ClientProfile
{
    /// <summary>Full display name, for example "uTorrent 3.3.2". Unique within the catalog.</summary>
    public required string Name { get; init; }

    /// <summary>Client family, for example "uTorrent".</summary>
    public required string Family { get; init; }

    /// <summary>Version within the family, for example "3.3.2".</summary>
    public required string Version { get; init; }

    /// <summary>"HTTP/1.0" or "HTTP/1.1".</summary>
    public required string HttpProtocol { get; init; }

    /// <summary>Whether the percent-encoded info hash uses upper-case hex escapes.</summary>
    public bool HashUpperCase { get; init; }

    /// <summary>How to generate the tracker "key" value.</summary>
    public required RandomValueSpec Key { get; init; }

    /// <summary>The fixed peer id prefix (may contain literal %xx escapes).</summary>
    public required string PeerIdPrefix { get; init; }

    /// <summary>How to generate the random part appended to <see cref="PeerIdPrefix"/>.</summary>
    public required RandomValueSpec PeerId { get; init; }

    /// <summary>Request header lines in order, with "{host}" where the host goes. No CRLFs.</summary>
    public required IReadOnlyList<string> Headers { get; init; }

    /// <summary>The announce query template with {infohash} {peerid} {port} {uploaded} {downloaded} {left} {event} {numwant} {key} {localip} placeholders.</summary>
    public required string Query { get; init; }

    /// <summary>Default value for numwant.</summary>
    public int DefaultNumWant { get; init; } = 200;

    /// <summary>Memory-scan location, or null when this client cannot be parsed from a running process.</summary>
    public ClientMemoryScanSpec? MemoryScan { get; init; }

    public bool CanScanMemory => this.MemoryScan is not null;
}
