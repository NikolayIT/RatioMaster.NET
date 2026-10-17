namespace RatioMaster.Core.Networking
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// Answers incoming BitTorrent handshakes on one port for every torrent announced with it, the way a real client
    /// listens on one port for all of its torrents, so each of them looks connectable. The reply carries the torrent the
    /// peer asked for and that torrent's own peer id. Torrents join and leave through <see cref="PeerListenerPool"/>.
    /// Managed replacement for the old RM TcpListener responder. Only meaningful without a proxy.
    /// </summary>
    public sealed class PeerListener : IAsyncDisposable
    {
        private const string ProtocolIdentifier = "BitTorrent protocol";
        private const int HandshakeReadLength = 68;

        private static readonly byte[] Protocol = Encoding.ASCII.GetBytes(ProtocolIdentifier);

        private readonly Lock gate = new();
        private readonly List<PeerListenerRegistration> torrents = [];

        private TcpListener? listener;
        private CancellationTokenSource? cts;
        private Task? acceptLoop;

        public PeerListener(int port)
        {
            this.Port = port;
        }

        public int Port { get; }

        public bool IsListening => this.listener is not null;

        /// <summary>Gets a value indicating whether no torrent is left on this port.</summary>
        internal bool IsEmpty
        {
            get
            {
                lock (this.gate)
                {
                    return this.torrents.Count == 0;
                }
            }
        }

        /// <summary>Starts listening. Returns null, or the reason it could not, which is usually that the port is in use.</summary>
        public string? Start()
        {
            if (this.listener is not null)
            {
                return null;
            }

            var listener = new TcpListener(IPAddress.Any, this.Port);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                listener.Dispose();
                return ex.Message;
            }

            this.listener = listener;
            this.cts = new CancellationTokenSource();
            this.acceptLoop = Task.Run(() => this.AcceptLoopAsync(listener, this.cts.Token));
            return null;
        }

        public async ValueTask DisposeAsync()
        {
            this.StopAccepting();
            if (this.acceptLoop is not null)
            {
                try
                {
                    await this.acceptLoop.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    // Expected during shutdown.
                }
            }

            this.cts?.Dispose();
            this.listener = null;
            this.cts = null;
            this.acceptLoop = null;
        }

        internal void Add(PeerListenerRegistration torrent)
        {
            lock (this.gate)
            {
                this.torrents.Add(torrent);
            }
        }

        internal void Remove(PeerListenerRegistration torrent)
        {
            lock (this.gate)
            {
                this.torrents.Remove(torrent);
            }
        }

        /// <summary>Closes the port at once, so another listener can open it, without waiting for the accept loop.</summary>
        internal void StopAccepting()
        {
            this.cts?.Cancel();
            this.listener?.Stop();
        }

        private static async Task<int> ReadSomeAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var total = 0;
            while (total < buffer.Length)
            {
                int read;
                try
                {
                    read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total >= HandshakeReadLength)
                {
                    break;
                }
            }

            return total;
        }

        private static byte[] BuildHandshakeResponse(PeerListenerRegistration torrent)
        {
            var response = new byte[1 + ProtocolIdentifier.Length + 8 + torrent.InfoHash.Length + torrent.PeerId.Length];
            var i = 0;
            response[i++] = (byte)ProtocolIdentifier.Length;
            i += Encoding.ASCII.GetBytes(ProtocolIdentifier, response.AsSpan(i));
            i += 8; // 8 reserved zero bytes
            torrent.InfoHash.CopyTo(response, i);
            i += torrent.InfoHash.Length;
            torrent.PeerId.CopyTo(response, i);
            return response;
        }

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    socket = await listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    // Cancelled, or the listener was stopped underneath the pending accept: either way we are done.
                    break;
                }

                _ = this.HandleClientAsync(socket, cancellationToken);
            }
        }

        private async Task HandleClientAsync(Socket socket, CancellationToken cancellationToken)
        {
            try
            {
                using (socket)
                await using (var stream = new NetworkStream(socket, ownsSocket: false))
                {
                    var buffer = new byte[HandshakeReadLength];
                    using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    readCts.CancelAfter(TimeSpan.FromSeconds(5));
                    var read = await ReadSomeAsync(stream, buffer, readCts.Token).ConfigureAwait(false);
                    if (this.FindTorrent(buffer.AsSpan(0, read)) is { } torrent)
                    {
                        var response = BuildHandshakeResponse(torrent);
                        await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
                        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
            {
                // A misbehaving or disconnecting peer must never take down the listener.
            }
        }

        /// <summary>Gets the torrent a handshake asks for; when one torrent is on the port twice, the later one answers.</summary>
        private PeerListenerRegistration? FindTorrent(ReadOnlySpan<byte> data)
        {
            if (data.IndexOf(Protocol) < 0)
            {
                return null;
            }

            lock (this.gate)
            {
                for (var i = this.torrents.Count - 1; i >= 0; i--)
                {
                    if (data.IndexOf(this.torrents[i].InfoHash) >= 0)
                    {
                        return this.torrents[i];
                    }
                }
            }

            return null;
        }
    }
}
