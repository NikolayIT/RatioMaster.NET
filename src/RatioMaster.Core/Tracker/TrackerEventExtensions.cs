namespace RatioMaster.Core.Tracker;

public static class TrackerEventExtensions
{
    /// <summary>The substitution for the {event} placeholder, including the leading "&amp;event=" (empty for None).</summary>
    public static string ToQueryValue(this TrackerEvent value) => value switch
    {
        TrackerEvent.Started => "&event=started",
        TrackerEvent.Stopped => "&event=stopped",
        TrackerEvent.Completed => "&event=completed",
        _ => string.Empty,
    };
}
