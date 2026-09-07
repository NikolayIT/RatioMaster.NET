namespace RatioMaster.Core.Settings
{
    using System.Text.Json.Serialization;

    using RatioMaster.Core.Sessions;

    /// <summary>One column of the torrent list, as the user arranged it.</summary>
    public sealed record ColumnLayout
    {
        [JsonConstructor]
        public ColumnLayout()
        {
        }

        public required string Name { get; set; }

        public bool IsVisible { get; set; } = true;

        public double Width { get; set; }

        public int DisplayIndex { get; set; }
    }
}
