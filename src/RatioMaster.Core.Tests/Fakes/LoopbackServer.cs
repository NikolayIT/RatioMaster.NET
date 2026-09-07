namespace RatioMaster.Core.Tests.Fakes
{
    using System.Net;
    using System.Net.Sockets;

    /// <summary>A one-connection loopback TCP server for transport tests. Runs a handler for the first client.</summary>
    internal sealed class LoopbackServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly Task serve;
        private readonly CancellationTokenSource cts = new();

        private LoopbackServer(TcpListener listener, Func<NetworkStream, CancellationToken, Task> handler)
        {
            this.listener = listener;
            this.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            this.serve = this.ServeAsync(handler, this.cts.Token);
        }

        public int Port { get; }

        public Exception? HandlerError { get; private set; }

        public static LoopbackServer Start(Func<NetworkStream, CancellationToken, Task> handler)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new LoopbackServer(listener, handler);
        }

        /// <summary>Reads exactly <paramref name="count"/> bytes.</summary>
        public static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer;
        }

        /// <summary>Reads a single 0x00-terminated field (not including the terminator).</summary>
        public static async Task<byte[]> ReadUntilNulAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = new List<byte>();
            var one = new byte[1];
            while (true)
            {
                await stream.ReadExactlyAsync(one, cancellationToken).ConfigureAwait(false);
                if (one[0] == 0x00)
                {
                    break;
                }

                bytes.Add(one[0]);
            }

            return bytes.ToArray();
        }

        /// <summary>Reads through the end of the HTTP header block (CRLFCRLF) and returns it as text.</summary>
        public static async Task<string> ReadHttpHeadersAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = new List<byte>();
            var one = new byte[1];
            while (bytes.Count < 4 || !(bytes[^4] == '\r' && bytes[^3] == '\n' && bytes[^2] == '\r' && bytes[^1] == '\n'))
            {
                await stream.ReadExactlyAsync(one, cancellationToken).ConfigureAwait(false);
                bytes.Add(one[0]);
            }

            return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
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

        private async Task ServeAsync(Func<NetworkStream, CancellationToken, Task> handler, CancellationToken cancellationToken)
        {
            try
            {
                using var socket = await this.listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
                await using var stream = new NetworkStream(socket, ownsSocket: false);
                await handler(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this.HandlerError = ex;
            }
        }
    }
}
