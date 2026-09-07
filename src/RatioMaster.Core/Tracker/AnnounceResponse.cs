using System.Buffers.Binary;
using System.Net;
using RatioMaster.Core.Bencode;

namespace RatioMaster.Core.Tracker;

/// <summary>The interpreted content of a tracker announce response.</summary>
public sealed class AnnounceResponse
{
    private AnnounceResponse(BencodeDictionary dictionary)
    {
        this.FailureReason = dictionary.GetDisplayText("failure reason");
        this.WarningMessage = dictionary.GetDisplayText("warning message");
        this.Interval = ToInt(dictionary, "interval");
        this.MinInterval = ToInt(dictionary, "min interval") ?? ToInt(dictionary, "min_interval");
        this.Complete = ToInt(dictionary, "complete");
        this.Incomplete = ToInt(dictionary, "incomplete");
        this.Downloaded = ToInt(dictionary, "downloaded");
        this.TrackerId = dictionary.GetLatin1Text("tracker id");
        this.Peers = ParsePeers(dictionary);
        this.ExtraKeys = dictionary.Keys
            .Where(k => k is not ("failure reason" or "warning message" or "peers" or "peers6"))
            .ToDictionary(k => k, k => BencodeDictionary.DisplayText(dictionary[k]), StringComparer.Ordinal);
    }

    public string? FailureReason { get; }

    public string? WarningMessage { get; }

    public int? Interval { get; }

    public int? MinInterval { get; }

    /// <summary>Seeders.</summary>
    public int? Complete { get; }

    /// <summary>Leechers.</summary>
    public int? Incomplete { get; }

    public int? Downloaded { get; }

    public string? TrackerId { get; }

    public IReadOnlyList<TrackerPeer> Peers { get; }

    public IReadOnlyDictionary<string, string> ExtraKeys { get; }

    public bool HasFailure => !string.IsNullOrEmpty(this.FailureReason);

    public static AnnounceResponse Parse(BencodeDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return new AnnounceResponse(dictionary);
    }

    private static int? ToInt(BencodeDictionary dictionary, string key) =>
        dictionary.GetInteger(key) is { } value ? (int)value : null;

    private static List<TrackerPeer> ParsePeers(BencodeDictionary dictionary)
    {
        var peers = new List<TrackerPeer>();
        if (dictionary.TryGetValue("peers", out var value))
        {
            switch (value)
            {
                case BencodeString compact:
                    AddCompactPeers(peers, compact.Span, ipv6: false);
                    break;
                case BencodeList list:
                    AddDictionaryPeers(peers, list);
                    break;
            }
        }

        if (dictionary.GetString("peers6") is { } compact6)
        {
            AddCompactPeers(peers, compact6.Span, ipv6: true);
        }

        return peers;
    }

    private static void AddCompactPeers(List<TrackerPeer> peers, ReadOnlySpan<byte> data, bool ipv6)
    {
        var stride = ipv6 ? 18 : 6;
        var addressLength = ipv6 ? 16 : 4;
        for (var i = 0; i + stride <= data.Length; i += stride)
        {
            var ip = new IPAddress(data.Slice(i, addressLength)).ToString();
            var port = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i + addressLength, 2));
            peers.Add(new TrackerPeer(ip, port));
        }
    }

    private static void AddDictionaryPeers(List<TrackerPeer> peers, BencodeList list)
    {
        foreach (var item in list)
        {
            if (item is not BencodeDictionary peer)
            {
                continue;
            }

            var ip = peer.GetLatin1Text("ip");
            var port = peer.GetInteger("port") ?? (int.TryParse(peer.GetLatin1Text("port"), out var p) ? p : (long?)null);
            if (ip is not null && port is not null)
            {
                peers.Add(new TrackerPeer(ip, (int)port.Value, peer.GetLatin1Text("peer id")));
            }
        }
    }
}
