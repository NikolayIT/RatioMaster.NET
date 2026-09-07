namespace RatioMaster.Core.Sessions
{
    using System.Text.Json.Serialization;

    /// <summary>The auto-stop trigger types (the old "Stop after" combo).</summary>
    public enum StopConditionType
    {
        Never,
        AfterSeconds,
        SeedersBelow,
        LeechersBelow,
        UploadedAboveMb,
        DownloadedAboveMb,
        LeecherSeederRatioBelow,
    }

    /// <summary>An auto-stop condition.</summary>
    public sealed record StopCondition
    {
        [JsonConstructor]
        public StopCondition()
        {
        }

        public static StopCondition Never { get; } = new() { Type = StopConditionType.Never };

        public StopConditionType Type { get; set; } = StopConditionType.Never;

        public double Value { get; set; }
    }
}
