namespace RatioMaster.Core.Tracker;

/// <summary>A peer returned by the tracker.</summary>
public sealed record TrackerPeer(string Ip, int Port, string? PeerId = null)
{
    public override string ToString() =>
        PeerId is { Length: > 0 } ? $"{Ip}:{Port}(PeerID={PeerId})" : $"{Ip}:{Port}";
}
