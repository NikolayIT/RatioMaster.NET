namespace RatioMaster.Core.Tracker
{
    using System.Net.Sockets;
    using System.Security.Authentication;

    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;

    /// <summary>
    /// Sends a hand-built HTTP announce/scrape request over the tracker transport and parses the response,
    /// following redirects and retrying the connection. This is the byte-exact replacement for RM.MakeWebRequestEx.
    /// </summary>
    public sealed class TrackerHttpClient
    {
        private const int ReadBufferSize = 32 * 1024;

        private readonly ITrackerTransport transport;
        private readonly TrackerHttpClientOptions options;

        public TrackerHttpClient(ITrackerTransport? transport = null, TrackerHttpClientOptions? options = null)
        {
            this.transport = transport ?? TrackerTransport.Instance;
            this.options = options ?? new TrackerHttpClientOptions();
        }

        /// <summary>The same client (transport, attempts, timeout) with certificate validation switched off.</summary>
        public TrackerHttpClient WithIgnoredCertificateErrors() => this.options.IgnoreCertificateErrors
            ? this
            : new TrackerHttpClient(this.transport, this.options with { IgnoreCertificateErrors = true });

        /// <summary>Sends a GET for <paramref name="url"/> using the client's headers, following redirects.</summary>
        public async Task<TrackerResponse> GetAsync(string url, ClientProfile profile, ProxySettings proxy, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(proxy);

            var uri = ParseUrl(url);
            for (var redirect = 0; ; redirect++)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(this.options.Timeout);
                TrackerResponse response;
                try
                {
                    response = await this.SendOnceAsync(uri, profile, proxy, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Our own timeout, not the caller cancelling: report it like any other tracker failure.
                    throw new TrackerException($"The tracker did not respond within {this.options.Timeout.TotalSeconds:0} seconds.");
                }

                // A redirect that cannot be followed (unparsable Location, or not http) is returned as it is.
                if (response.IsRedirect
                    && redirect < this.options.MaxRedirects
                    && Uri.TryCreate(uri, response.Location, out var next)
                    && TrackerUrl.IsHttp(next))
                {
                    uri = next;
                    continue;
                }

                return response;
            }
        }

        private static Uri ParseUrl(string url)
        {
            if (TrackerUrl.Validate(url) is { } problem)
            {
                throw new TrackerException(problem);
            }

            return new Uri(url.Trim(), UriKind.Absolute);
        }

        /// <summary>
        /// Reads one HTTP response. Stops as soon as the message is complete according to its Content-Length or
        /// chunked framing, because keep-alive servers never close the connection; responses without framing
        /// are read until the server closes (HTTP/1.0 style).
        /// </summary>
        private static async Task<byte[]> ReadResponseAsync(Stream stream, CancellationToken cancellationToken)
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
                if (HttpMessageFraming.IsComplete(output.GetBuffer().AsSpan(0, (int)output.Length)))
                {
                    break;
                }
            }

            return output.ToArray();
        }

        private async Task<TrackerResponse> SendOnceAsync(Uri uri, ClientProfile profile, ProxySettings proxy, CancellationToken cancellationToken)
        {
            var useTls = string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            var stream = await this.ConnectWithRetryAsync(uri, useTls, proxy, cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var request = HttpRequestWriter.BuildRequest(uri.PathAndQuery, uri.Host, profile);
                await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                var body = await ReadResponseAsync(stream, cancellationToken).ConfigureAwait(false);
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
            for (var attempt = 0; attempt < this.options.ConnectAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await this.transport.ConnectAsync(
                        uri.Host, uri.Port, useTls, this.options.IgnoreCertificateErrors, proxy, cancellationToken).ConfigureAwait(false);
                }
                catch (AuthenticationException ex)
                {
                    // The connection worked but the certificate was refused; another attempt cannot change that.
                    throw new TrackerException(
                        $"The TLS certificate of {uri.Host} is not trusted: {ex.Message} " +
                        "Turn on \"Ignore TLS certificate errors\" in the torrent settings if you trust this tracker.",
                        ex);
                }
                catch (Exception ex) when (ex is SocketException or IOException or ProxyException)
                {
                    lastError = ex;
                }
            }

            throw new TrackerException(
                $"Could not connect to {uri.Host}:{uri.Port} after {this.options.ConnectAttempts} attempts.", lastError!);
        }
    }
}
