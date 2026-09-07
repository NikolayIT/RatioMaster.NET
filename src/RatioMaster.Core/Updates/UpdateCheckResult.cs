using System.Globalization;
using System.Net.Http;

namespace RatioMaster.Core.Updates;

/// <summary>The outcome of one update check.</summary>
public sealed record UpdateCheckResult
{
    public string? RemoteVersion { get; init; }

    public bool UpdateAvailable { get; init; }

    public string? Error { get; init; }

    public bool Succeeded => this.Error is null;
}
