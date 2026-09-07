namespace RatioMaster.Core.Tests.Fakes
{
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>An in-process HTTP tracker: serves a queue of raw responses and records the requests it received.</summary>
    internal sealed class FakeTracker : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly Task serve;
        private readonly CancellationTokenSource cts = new();
        private readonly Queue<byte[]> responses;
        private readonly ConcurrentQueue<string> requests = new();

        private readonly bool keepAlive;

        private FakeTracker(TcpListener listener, IEnumerable<byte[]> responses, bool keepAlive)
        {
            this.listener = listener;
            this.keepAlive = keepAlive;
            this.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            this.responses = new Queue<byte[]>(responses);
            this.serve = this.ServeAsync(this.cts.Token);
        }

        public int Port { get; }

        public string BaseUrl => $"http://127.0.0.1:{this.Port}";

        public IReadOnlyCollection<string> Requests => this.requests.ToArray();

        public static FakeTracker Start(params byte[][] responses)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new FakeTracker(listener, responses, keepAlive: false);
        }

        /// <summary>
        /// Like <see cref="Start"/>, but the server keeps every connection open after answering, the way
        /// Cloudflare and nginx front ends do for HTTP/1.1 clients. The client must rely on the message framing.
        /// </summary>
        public static FakeTracker StartKeepAlive(params byte[][] responses)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new FakeTracker(listener, responses, keepAlive: true);
        }

        /// <summary>Builds a keep-alive response framed by Content-Length only.</summary>
        public static byte[] KeepAliveResponse(byte[] body, string? extraHeaders = null)
        {
            var header = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\n{extraHeaders}Connection: keep-alive\r\n\r\n";
            return [.. Encoding.Latin1.GetBytes(header), .. body];
        }

        /// <summary>Builds a keep-alive response with the body split into two chunks.</summary>
        public static byte[] ChunkedKeepAliveResponse(byte[] body)
        {
            var half = body.Length / 2;
            var first = body[..half];
            var second = body[half..];
            var header = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nTransfer-Encoding: chunked\r\nConnection: keep-alive\r\n\r\n";
            return
            [
                .. Encoding.Latin1.GetBytes(header),
                .. Encoding.ASCII.GetBytes($"{first.Length:x}\r\n"), .. first, .. "\r\n"u8.ToArray(),
                .. Encoding.ASCII.GetBytes($"{second.Length:x}\r\n"), .. second, .. "\r\n"u8.ToArray(),
                .. "0\r\n\r\n"u8.ToArray(),
            ];
        }

        /// <summary>Builds a raw HTTP response with a bencode body and Connection: close.</summary>
        public static byte[] BencodeResponse(byte[] body, string? extraHeaders = null)
        {
            var header = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\n{extraHeaders}Connection: close\r\n\r\n";
            return [.. Encoding.Latin1.GetBytes(header), .. body];
        }

        /// <summary>Builds a raw HTTP response with a plain-text body.</summary>
        public static byte[] TextResponse(string body, int statusCode = 200)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var header = $"HTTP/1.1 {statusCode} OK\r\nContent-Type: text/plain\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
            return [.. Encoding.Latin1.GetBytes(header), .. bytes];
        }

        /// <summary>Builds a raw 302 redirect response.</summary>
        public static byte[] Redirect(string location)
        {
            var text = $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
            return Encoding.Latin1.GetBytes(text);
        }

        public async ValueTask DisposeAsync()
        {
            await this.cts.CancelAsync().ConfigureAwait(false);
            this.listener.Stop();
            try
            {
                await this.serve.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException)
            {
                // Expected during shutdown.
            }

            this.cts.Dispose();
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    socket = await this.listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    break;
                }

                _ = this.HandleAsync(socket, cancellationToken);
            }
        }

        private async Task HandleAsync(Socket socket, CancellationToken cancellationToken)
        {
            try
            {
                using (socket)
                await using (var stream = new NetworkStream(socket, ownsSocket: false))
                {
                    var request = await LoopbackServer.ReadHttpHeadersAsync(stream, cancellationToken).ConfigureAwait(false);
                    this.requests.Enqueue(request);

                    byte[] response;
                    lock (this.responses)
                    {
                        response = this.responses.Count > 0 ? this.responses.Dequeue() : Encoding.Latin1.GetBytes("HTTP/1.1 500 No response\r\nConnection: close\r\n\r\n");
                    }

                    await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                    if (this.keepAlive)
                    {
                        // Hold the connection open until the tracker is disposed, like a real keep-alive server.
                        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
            {
                // A test that stops early is fine.
            }
        }
    }
}
