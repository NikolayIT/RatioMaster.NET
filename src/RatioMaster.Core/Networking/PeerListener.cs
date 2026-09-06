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

    private readonly int _port;
    private readonly byte[] _infoHash;
    private readonly byte[] _peerId;
    private readonly Action<string>? _log;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public PeerListener(int port, byte[] infoHash, string peerId, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(infoHash);
        ArgumentNullException.ThrowIfNull(peerId);
        _port = port;
        _infoHash = infoHash;
        _peerId = Encoding.Latin1.GetBytes(peerId);
        _log = log;
    }

    public bool IsListening => _listener is not null;

    /// <summary>Starts listening. Returns false (and logs) if the port is already in use.</summary>
    public bool Start()
    {
        if (_listener is not null)
        {
            return true;
        }

        var listener = new TcpListener(IPAddress.Any, _port);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            _log?.Invoke($"TCP listener could not start on port {_port} (already in use?): {ex.Message}");
            return false;
        }

        _listener = listener;
        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        _log?.Invoke($"Started TCP listener on port {_port}");
        return true;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        var listener = _listener!;
        while (!cancellationToken.IsCancellationRequested)
        {
            Socket socket;
            try
            {
                socket = await listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            _ = HandleClientAsync(socket, cancellationToken);
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
                if (IsMatchingHandshake(buffer.AsSpan(0, read)))
                {
                    var response = BuildHandshakeResponse();
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

    private bool IsMatchingHandshake(ReadOnlySpan<byte> data)
    {
        var protocol = Encoding.ASCII.GetBytes(ProtocolIdentifier);
        return data.IndexOf(protocol) >= 0 && data.IndexOf(_infoHash) >= 0;
    }

    private byte[] BuildHandshakeResponse()
    {
        var response = new byte[1 + ProtocolIdentifier.Length + 8 + _infoHash.Length + _peerId.Length];
        var i = 0;
        response[i++] = (byte)ProtocolIdentifier.Length;
        i += Encoding.ASCII.GetBytes(ProtocolIdentifier, response.AsSpan(i));
        i += 8; // 8 reserved zero bytes
        _infoHash.CopyTo(response, i);
        i += _infoHash.Length;
        _peerId.CopyTo(response, i);
        return response;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        _listener?.Stop();

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException)
            {
                // Expected during shutdown.
            }
        }

        _cts?.Dispose();
        _listener = null;
        _cts = null;
        _acceptLoop = null;
        if (IsListening)
        {
            _log?.Invoke("TCP listener closed");
        }
    }
}
