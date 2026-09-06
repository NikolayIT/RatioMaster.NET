namespace RatioMaster.Core.Tracker;

/// <summary>The BitTorrent announce event.</summary>
public enum TrackerEvent
{
    /// <summary>A periodic update (no event parameter).</summary>
    None,

    /// <summary>The first announce for this session.</summary>
    Started,

    /// <summary>The final announce when stopping.</summary>
    Stopped,

    /// <summary>Sent once when the download reaches 100%.</summary>
    Completed,
}

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
