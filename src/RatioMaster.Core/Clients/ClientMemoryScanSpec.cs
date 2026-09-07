namespace RatioMaster.Core.Clients
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Where to look inside a running client's memory to copy its real peer id, key, port and numwant.
    /// Present only for profiles the old app could parse (uTorrent, BitComet, Azureus, Vuze, ABC).
    /// </summary>
    public sealed record ClientMemoryScanSpec
    {
        [JsonConstructor]
        public ClientMemoryScanSpec()
        {
        }

        /// <summary>Gets or sets the OS process name to search, without extension (for example "uTorrent").</summary>
        public required string ProcessName { get; set; }

        /// <summary>Gets or sets the marker that locates the announce query in memory (for example "&amp;peer_id=-UT3320-").</summary>
        public required string SearchString { get; set; }

        public long StartOffset { get; set; }

        public long MaxOffset { get; set; }
    }
}
