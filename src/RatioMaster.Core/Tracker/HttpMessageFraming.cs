using System.Globalization;
using System.Text;

namespace RatioMaster.Core.Tracker;

/// <summary>
/// Decides whether the bytes received so far form a complete HTTP response. Trackers behind keep-alive
/// front ends (Cloudflare, nginx) never close the connection after answering an HTTP/1.1 request, so the
/// reader must stop at the end of the message as declared by Content-Length or chunked encoding instead
/// of waiting for EOF.
/// </summary>
public static class HttpMessageFraming
{
    /// <summary>
    /// True when <paramref name="data"/> holds a whole response. False while more bytes may follow; also
    /// false for responses without length framing, which are complete only when the server closes.
    /// </summary>
    public static bool IsComplete(ReadOnlySpan<byte> data)
    {
        var bodyStart = FindBodyStart(data);
        if (bodyStart < 0)
        {
            return false;
        }

        var headerText = Encoding.Latin1.GetString(data[..bodyStart]);
        var lines = headerText.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var statusCode = ParseStatusCode(lines.Length > 0 ? lines[0] : string.Empty);
        if (statusCode is (>= 100 and < 200) or 204 or 304)
        {
            return true;
        }

        string? transferEncoding = null;
        string? contentLength = null;
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                transferEncoding = value;
            }
            else if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                contentLength = value;
            }
        }

        var body = data[bodyStart..];
        if (transferEncoding is not null && transferEncoding.Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            return IsChunkedBodyComplete(body);
        }

        if (contentLength is not null && long.TryParse(contentLength, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) && length >= 0)
        {
            return body.Length >= length;
        }

        return false;
    }

    /// <summary>The index of the first body byte, or -1 while the header block is still incomplete.</summary>
    public static int FindBodyStart(ReadOnlySpan<byte> data)
    {
        var crlf = data.IndexOf("\r\n\r\n"u8);
        if (crlf >= 0)
        {
            return crlf + 4;
        }

        var lf = data.IndexOf("\n\n"u8);
        return lf >= 0 ? lf + 2 : -1;
    }

    private static bool IsChunkedBodyComplete(ReadOnlySpan<byte> body)
    {
        var position = 0;
        while (true)
        {
            var lineEnd = body[position..].IndexOf("\r\n"u8);
            if (lineEnd < 0)
            {
                return false;
            }

            var sizeText = Encoding.ASCII.GetString(body.Slice(position, lineEnd)).Split(';')[0].Trim();
            if (!long.TryParse(sizeText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chunkSize) || chunkSize < 0)
            {
                // Malformed chunk size: let the caller read until the server closes.
                return false;
            }

            position += lineEnd + 2;
            if (chunkSize == 0)
            {
                // The last chunk is followed by optional trailers and a blank line.
                var rest = body[position..];
                return rest.StartsWith("\r\n"u8) || rest.IndexOf("\r\n\r\n"u8) >= 0;
            }

            position += (int)Math.Min(chunkSize, int.MaxValue) + 2;
            if (position > body.Length)
            {
                return false;
            }
        }
    }

    private static int ParseStatusCode(string statusLine)
    {
        var parts = statusLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
            ? code
            : 0;
    }
}
