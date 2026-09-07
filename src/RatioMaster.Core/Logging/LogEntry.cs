namespace RatioMaster.Core.Logging
{
    /// <summary>Severity of a log line.</summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>One session log line. Timestamp formatting (12/24h) happens at render time.</summary>
    public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Text);
}
