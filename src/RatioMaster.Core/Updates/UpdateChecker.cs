using System.Globalization;
using System.Net.Http;

namespace RatioMaster.Core.Updates;

/// <summary>The outcome of one update check.</summary>
public sealed record UpdateCheckResult
{
    public string? RemoteVersion { get; init; }

    public bool UpdateAvailable { get; init; }

    public string? Error { get; init; }

    public bool Succeeded => Error is null;
}

/// <summary>
/// Asks ratiomaster.net whether a newer build exists. Same contract as the 0.43 checker
/// (GET /vc.php?v=NNNN returning exactly four characters) but over HTTPS and without the Windows username.
/// </summary>
public sealed class UpdateChecker
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public UpdateChecker(HttpClient? http = null, string? baseUrl = null, TimeSpan? timeout = null)
    {
        _baseUrl = baseUrl ?? AppVersion.WebsiteUrl;
        _http = http ?? new HttpClient();
        if (_http.Timeout == TimeSpan.FromSeconds(100))
        {
            _http.Timeout = timeout ?? TimeSpan.FromSeconds(2.5);
        }
    }

    /// <summary>The User-Agent sent with the check. Deliberately carries no user name.</summary>
    public static string UserAgent { get; } = BuildUserAgent();

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl.TrimEnd('/')}/vc.php?v={AppVersion.CheckId}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
            if (body.Length != 4)
            {
                return new UpdateCheckResult { Error = "The version service returned an unexpected reply." };
            }

            return new UpdateCheckResult
            {
                RemoteVersion = body,
                UpdateAvailable = string.CompareOrdinal(body, AppVersion.CheckId) > 0,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new UpdateCheckResult { Error = ex.Message };
        }
    }

    private static string BuildUserAgent()
    {
        var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        var runtime = Environment.Version.ToString();
        var cpus = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
        return $"RatioMaster.NET/{AppVersion.CheckId} ({os}; {arch}; .NET {runtime}; {cpus})";
    }
}
