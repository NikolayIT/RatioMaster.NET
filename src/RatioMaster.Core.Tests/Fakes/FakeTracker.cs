using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RatioMaster.Core.Tests.Fakes;

/// <summary>An in-process HTTP tracker: serves a queue of raw responses and records the requests it received.</summary>
internal sealed class FakeTracker : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _serve;
    private readonly CancellationTokenSource _cts = new();
    private readonly Queue<byte[]> _responses;
    private readonly ConcurrentQueue<string> _requests = new();

    private FakeTracker(TcpListener listener, IEnumerable<byte[]> responses)
    {
        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _responses = new Queue<byte[]>(responses);
        _serve = ServeAsync(_cts.Token);
    }

    public int Port { get; }

    public string BaseUrl => $"http://127.0.0.1:{Port}";

    public IReadOnlyCollection<string> Requests => _requests.ToArray();

    public static FakeTracker Start(params byte[][] responses)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new FakeTracker(listener, responses);
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

    private async Task ServeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Socket socket;
            try
            {
                socket = await _listener.AcceptSocketAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                break;
            }

            _ = HandleAsync(socket, cancellationToken);
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
                _requests.Enqueue(request);

                byte[] response;
                lock (_responses)
                {
                    response = _responses.Count > 0 ? _responses.Dequeue() : Encoding.Latin1.GetBytes("HTTP/1.1 500 No response\r\nConnection: close\r\n\r\n");
                }

                await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
        {
            // A test that stops early is fine.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        _listener.Stop();
        try
        {
            await _serve.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException)
        {
            // Expected during shutdown.
        }

        _cts.Dispose();
    }
}
