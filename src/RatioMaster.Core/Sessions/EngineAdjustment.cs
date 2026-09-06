namespace RatioMaster.Core.Sessions;

/// <summary>An automatic change the engine made that the UI should reflect (interval override, upload forced to 0).</summary>
public sealed record EngineAdjustment
{
    public int? IntervalSeconds { get; init; }

    public long? UploadRateBytes { get; init; }

    public required string Reason { get; init; }
}
