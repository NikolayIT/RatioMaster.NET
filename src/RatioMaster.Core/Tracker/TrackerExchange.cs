namespace RatioMaster.Core.Tracker;

/// <summary>A record of one announce or scrape round-trip, for the UI's Tracker tab.</summary>
public sealed record TrackerExchange
{
    public required DateTimeOffset Timestamp { get; init; }

    public required string Kind { get; init; }

    public required string RequestUrl { get; init; }

    public string? RequestText { get; init; }

    public string? ResponseHeaders { get; init; }

    public int? Interval { get; init; }

    public int? Complete { get; init; }

    public int? Incomplete { get; init; }

    public int PeerCount { get; init; }

    public double DurationMs { get; init; }

    public string? Error { get; init; }

    public string Result => this.Error ?? "OK";
}
