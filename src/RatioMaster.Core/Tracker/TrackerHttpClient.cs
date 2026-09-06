using System.Net.Sockets;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tracker;

/// <summary>Options for <see cref="TrackerHttpClient"/>.</summary>
public sealed record TrackerHttpClientOptions
{
    /// <summary>Connection attempts before giving up (the old app tried up to 5).</summary>
    public int ConnectAttempts { get; init; } = 5;

    /// <summary>Maximum number of redirects to follow.</summary>
    public int MaxRedirects { get; init; } = 5;

    /// <summary>Per-request timeout.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Skip TLS certificate validation for https trackers.</summary>
    public bool IgnoreCertificateErrors { get; init; }
}

/// <summary>
/// Sends a hand-built HTTP announce/scrape request over the tracker transport and parses the response,
/// following redirects and retrying the connection. This is the byte-exact replacement for RM.MakeWebRequestEx.
/// </summary>
public sealed class TrackerHttpClient
{
    private const int ReadBufferSize = 32 * 1024;

    private readonly ITrackerTransport _transport;
    private readonly TrackerHttpClientOptions _options;

    public TrackerHttpClient(ITrackerTransport? transport = null, TrackerHttpClientOptions? options = null)
    {
        _transport = transport ?? TrackerTransport.Instance;
        _options = options ?? new TrackerHttpClientOptions();
    }

    /// <summary>Sends a GET for <paramref name="url"/> using the client's headers, following redirects.</summary>
    public async Task<TrackerResponse> GetAsync(string url, ClientProfile profile, ProxySettings proxy, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(proxy);

        var uri = new Uri(url, UriKind.Absolute);
        for (var redirect = 0; ; redirect++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.Timeout);
            var response = await SendOnceAsync(uri, profile, proxy, timeoutCts.Token).ConfigureAwait(false);

            if (response.IsRedirect && redirect < _options.MaxRedirects)
            {
                uri = new Uri(uri, response.Location!);
                continue;
            }

            return response;
        }
    }

    private async Task<TrackerResponse> SendOnceAsync(Uri uri, ClientProfile profile, ProxySettings proxy, CancellationToken cancellationToken)
    {
        var useTls = string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        var stream = await ConnectWithRetryAsync(uri, useTls, proxy, cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var request = HttpRequestWriter.BuildRequest(uri.PathAndQuery, uri.Host, profile);
            await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            var body = await ReadToEndAsync(stream, cancellationToken).ConfigureAwait(false);
            if (body.Length == 0)
            {
                throw new TrackerException("The tracker response was empty.");
            }

            return TrackerResponse.Parse(body);
        }
    }

    private async Task<Stream> ConnectWithRetryAsync(Uri uri, bool useTls, ProxySettings proxy, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < _options.ConnectAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await _transport.ConnectAsync(
                    uri.Host, uri.Port, useTls, _options.IgnoreCertificateErrors, proxy, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is SocketException or IOException or ProxyException)
            {
                lastError = ex;
            }
        }

        throw new TrackerException(
            $"Could not connect to {uri.Host}:{uri.Port} after {_options.ConnectAttempts} attempts.", lastError!);
    }

    private static async Task<byte[]> ReadToEndAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        var buffer = new byte[ReadBufferSize];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }
}
