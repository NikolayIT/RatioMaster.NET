namespace RatioMaster.Core.Settings
{
    using System.Text.Json.Serialization;

    using RatioMaster.Core.Sessions;

    /// <summary>Persisted position and size of the main window.</summary>
    public sealed record WindowPlacement
    {
        [JsonConstructor]
        public WindowPlacement()
        {
        }

        public double? X { get; set; }

        public double? Y { get; set; }

        public double Width { get; set; } = 1340;

        public double Height { get; set; } = 820;

        public bool IsMaximized { get; set; }
    }
}
