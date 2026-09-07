namespace RatioMaster.Core.Tracker
{
    /// <summary>The concrete values substituted into a client's announce query template.</summary>
    public sealed record AnnounceValues
    {
        /// <summary>Gets the info hash, already percent-encoded for this client's hash case.</summary>
        public required string InfoHashEncoded { get; init; }

        public required string PeerId { get; init; }

        public required string Port { get; init; }

        public required long Uploaded { get; init; }

        public required long Downloaded { get; init; }

        public required long Left { get; init; }

        public required string Key { get; init; }

        /// <summary>Gets the requested peer count. "0" is promoted to "200" on non-stopped announces.</summary>
        public required string NumWant { get; init; }

        public required string LocalIp { get; init; }
    }
}
