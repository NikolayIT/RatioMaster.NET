namespace RatioMaster.Core.Sessions
{
    using System.Text.Json.Serialization;

    /// <summary>A saved session: the list of torrents with their settings.</summary>
    public sealed record SessionDocument
    {
        public const int CurrentVersion = 2;

        [JsonConstructor]
        public SessionDocument()
        {
        }

        public int Version { get; set; } = CurrentVersion;

        public List<SessionEntry> Torrents { get; set; } = [];
    }
}
