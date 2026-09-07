namespace RatioMaster.Core.Sessions;

/// <summary>An immutable snapshot of a session's live counters, read once per second by the UI.</summary>
public sealed record TorrentStats
{
    public required TorrentSessionState State { get; init; }

    public string? StopReason { get; init; }

    public long Uploaded { get; init; }

    public long Downloaded { get; init; }

    public long Left { get; init; }

    public long TotalSize { get; init; }

    /// <summary>Gets the upload/download ratio, or null ("NaN") until at least 100 KB has been downloaded.</summary>
    public double? Ratio { get; init; }

    public double FinishedPercent { get; init; }

    public int? Seeders { get; init; }

    public int? Leechers { get; init; }

    /// <summary>Gets the current effective upload rate in bytes per second.</summary>
    public long UploadRateBytes { get; init; }

    /// <summary>Gets the current effective download rate in bytes per second.</summary>
    public long DownloadRateBytes { get; init; }

    public TimeSpan TotalRunningTime { get; init; }

    public TimeSpan? NextUpdateIn { get; init; }

    public TimeSpan? StopAfterRemaining { get; init; }

    public bool IsRunning => this.State is TorrentSessionState.Downloading or TorrentSessionState.Seeding
        or TorrentSessionState.Updating or TorrentSessionState.Starting;
}
