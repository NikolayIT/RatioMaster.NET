using System.Text;

namespace RatioMaster.Core.Networking;

/// <summary>Establishes an HTTP CONNECT tunnel to the target host over an already-connected stream.</summary>
internal static class HttpConnectProxy
{
    public static async Task ConnectAsync(Stream stream, string host, int port, ProxySettings proxy, CancellationToken cancellationToken)
    {
        var target = $"{host}:{port}";
        var request = new StringBuilder();
        request.Append("CONNECT ").Append(target).Append(" HTTP/1.1\r\n");
        request.Append("Host: ").Append(target).Append("\r\n");
        if (proxy.HasCredentials)
        {
            var token = Convert.ToBase64String(Encoding.Latin1.GetBytes($"{proxy.Username}:{proxy.Password}"));
            request.Append("Proxy-Authorization: Basic ").Append(token).Append("\r\n");
        }

        request.Append("Proxy-Connection: keep-alive\r\n\r\n");

        await stream.WriteAsync(Encoding.ASCII.GetBytes(request.ToString()), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        var statusLine = await ReadHeaderTerminatedResponseAsync(stream, cancellationToken).ConfigureAwait(false);
        // Expect "HTTP/1.x 200 ...".
        var parts = statusLine.Split(' ', 3);
        if (parts.Length < 2 || parts[1] != "200")
        {
            throw new ProxyException($"HTTP CONNECT proxy refused the tunnel: {statusLine}");
        }
    }

    private static async Task<string> ReadHeaderTerminatedResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        // Read byte-by-byte until the CRLFCRLF that ends the response headers; keep only the status line.
        var buffer = new byte[1];
        var response = new StringBuilder();
        var matched = 0;
        var terminator = "\r\n\r\n";
        while (matched < terminator.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new ProxyException("HTTP CONNECT proxy closed the connection before completing the tunnel.");
            }

            var c = (char)buffer[0];
            response.Append(c);
            matched = c == terminator[matched] ? matched + 1 : (c == terminator[0] ? 1 : 0);
            if (response.Length > 8192)
            {
                throw new ProxyException("HTTP CONNECT proxy response headers were too large.");
            }
        }

        var text = response.ToString();
        var lineEnd = text.IndexOf("\r\n", StringComparison.Ordinal);
        return lineEnd >= 0 ? text[..lineEnd] : text;
    }
}
