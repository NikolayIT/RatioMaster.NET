namespace RatioMaster.Core.Tracker
{
    /// <summary>A peer returned by the tracker.</summary>
    public sealed record TrackerPeer(string Ip, int Port, string? PeerId = null)
    {
        public override string ToString() =>
            this.PeerId is { Length: > 0 } ? $"{this.Ip}:{this.Port}(PeerID={this.PeerId})" : $"{this.Ip}:{this.Port}";
    }
}
