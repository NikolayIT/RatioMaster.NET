using System.Net.Sockets;
using System.Security.Authentication;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tracker;

/// <summary>Options for <see cref="TrackerHttpClient"/>.</summary>
public sealed record TrackerHttpClientOptions
{
    /// <summary>Gets the connection attempts before giving up (the old app tried up to 5).</summary>
    public int ConnectAttempts { get; init; } = 5;

    /// <summary>Gets the maximum number of redirects to follow.</summary>
    public int MaxRedirects { get; init; } = 5;

    /// <summary>Gets the per-request timeout.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Gets a value indicating whether skip TLS certificate validation for https trackers.</summary>
    public bool IgnoreCertificateErrors { get; init; }
}
