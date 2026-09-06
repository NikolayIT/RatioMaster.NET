namespace RatioMaster.Core.Tracker;

/// <summary>
/// Checks a tracker URL before anything is sent to it. The engine only speaks HTTP, so a torrent whose
/// tracker is udp:// (or has no tracker at all) must fail with a clear reason instead of retrying forever.
/// </summary>
public static class TrackerUrl
{
    /// <summary>Returns null when <paramref name="url"/> can be announced to, otherwise the reason it cannot.</summary>
    public static string? Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "The torrent has no tracker URL.";
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return $"'{trimmed}' is not a valid tracker URL.";
        }

        if (!IsHttp(uri))
        {
            return $"Only http and https trackers are supported; this one is {uri.Scheme}://. Pick another tracker from the torrent, if it has one.";
        }

        if (uri.Host.Length == 0)
        {
            return $"'{trimmed}' is not a valid tracker URL.";
        }

        return null;
    }

    public static bool IsValid(string? url) => Validate(url) is null;

    internal static bool IsHttp(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
