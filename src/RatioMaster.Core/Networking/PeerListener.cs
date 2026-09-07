using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RatioMaster.Core.Networking;

/// <summary>
/// Answers incoming BitTorrent handshakes on the announced port so the torrent looks connectable to peers.
/// Managed replacement for the old RM TcpListener responder. Only meaningful without a proxy.
/// </summary>
public sealed class PeerListener : IAsyncDisposable
{
    private const string ProtocolIdentifier = "BitTorrent protocol";
    private const int HandshakeReadLength = 68;

    private readonly int port;
    private readonly byte[] infoHash;
    private readonly byte[] peerId;
    private readonly Action<string>? log;

    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private Task? acceptLoop;

    public PeerListener(int port, byte[] infoHash, string peerId, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(infoHash);
        ArgumentNullException.ThrowIfNull(peerId);
        this.port = port;
        this.infoHash = infoHash;
        this.peerId = Encoding.Latin1.GetBytes(peerId);
        this.log = log;
    }

    public bool IsListening => this.listener is not null;

    /// <summary>Starts listening. Returns false (and logs) if the port is already in use.</summary>
    public bool Start()
    {
        if (this.listener is not null)
        {
            return true;
        }

        var listener = new TcpListener(IPAddress.Any, this.port);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            this.log?.Invoke($"TCP listener could not start on port {this.port} (already in use?): {ex.Message}");
            return false;
        }

        this.listener = listener;
        this.cts = new CancellationTokenSource();
        this.acceptLoop = Task.Run(() => this.AcceptLoopAsync(this.cts.Token));
        this.log?.Invoke($"Started TCP listener on port {this.port}");
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        var wasListening = this.IsListening;
        if (this.cts is not null)
        {
            await this.cts.CancelAsync().ConfigureAwait(false);
        }

        this.listener?.Stop();

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
        if (wasListening)
        {
            this.log?.Invoke("TCP listener closed");
        }
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

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        var listener = this.listener!;
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
                if (this.IsMatchingHandshake(buffer.AsSpan(0, read)))
                {
                    var response = this.BuildHandshakeResponse();
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

    private bool IsMatchingHandshake(ReadOnlySpan<byte> data)
    {
        var protocol = Encoding.ASCII.GetBytes(ProtocolIdentifier);
        return data.IndexOf(protocol) >= 0 && data.IndexOf(this.infoHash) >= 0;
    }

    private byte[] BuildHandshakeResponse()
    {
        var response = new byte[1 + ProtocolIdentifier.Length + 8 + this.infoHash.Length + this.peerId.Length];
        var i = 0;
        response[i++] = (byte)ProtocolIdentifier.Length;
        i += Encoding.ASCII.GetBytes(ProtocolIdentifier, response.AsSpan(i));
        i += 8; // 8 reserved zero bytes
        this.infoHash.CopyTo(response, i);
        i += this.infoHash.Length;
        this.peerId.CopyTo(response, i);
        return response;
    }
}
