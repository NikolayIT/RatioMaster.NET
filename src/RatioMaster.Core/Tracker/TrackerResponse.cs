namespace RatioMaster.Core.Tracker
{
    using System.Globalization;
    using System.IO.Compression;
    using System.Text;

    using RatioMaster.Core.Bencode;

    /// <summary>A parsed raw HTTP tracker response: status, headers, decoded body and its bencode dictionary.</summary>
    public sealed class TrackerResponse
    {
        private TrackerResponse(
            int statusCode,
            string statusLine,
            string rawHeaders,
            IReadOnlyDictionary<string, string> headers,
            byte[] body,
            BencodeDictionary? dictionary)
        {
            this.StatusCode = statusCode;
            this.StatusLine = statusLine;
            this.RawHeaders = rawHeaders;
            this.Headers = headers;
            this.Body = body;
            this.Dictionary = dictionary;
        }

        public int StatusCode { get; }

        public string StatusLine { get; }

        public string RawHeaders { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Gets the decoded body (dechunked and decompressed).</summary>
        public byte[] Body { get; }

        /// <summary>Gets the bencode dictionary parsed from the body, or null when the body was not bencode.</summary>
        public BencodeDictionary? Dictionary { get; }

        public bool IsRedirect => this.StatusCode is >= 300 and < 400 && this.Location is not null;

        public string? Location => this.Headers.TryGetValue("location", out var value) ? value : null;

        public static TrackerResponse Parse(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var (headerLength, bodyStart) = FindHeaderBoundary(data);
            var rawHeaders = Encoding.Latin1.GetString(data, 0, headerLength);
            var lines = rawHeaders.Split(["\r\n", "\n"], StringSplitOptions.None);
            var statusLine = lines.Length > 0 ? lines[0] : string.Empty;
            var statusCode = ParseStatusCode(statusLine);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Skip(1))
            {
                var colon = line.IndexOf(':');
                if (colon > 0)
                {
                    headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
                }
            }

            var bodyBytes = data.AsSpan(bodyStart).ToArray();
            if (headers.TryGetValue("transfer-encoding", out var te) && te.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            {
                bodyBytes = Dechunk(bodyBytes);
            }

            if (headers.TryGetValue("content-encoding", out var ce))
            {
                bodyBytes = Decompress(bodyBytes, ce);
            }

            BencodeDictionary? dictionary = null;
            if (statusCode is < 300 or >= 400)
            {
                BencodeParser.TryParse(bodyBytes, out var parsed);
                dictionary = parsed as BencodeDictionary;
            }

            return new TrackerResponse(statusCode, statusLine, rawHeaders, headers, bodyBytes, dictionary);
        }

        private static (int HeaderLength, int BodyStart) FindHeaderBoundary(byte[] data)
        {
            for (var i = 0; i + 3 < data.Length; i++)
            {
                if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                {
                    return (i, i + 4);
                }
            }

            for (var i = 0; i + 1 < data.Length; i++)
            {
                if (data[i] == '\n' && data[i + 1] == '\n')
                {
                    return (i, i + 2);
                }
            }

            return (data.Length, data.Length);
        }

        private static int ParseStatusCode(string statusLine)
        {
            var parts = statusLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)
                ? code
                : 0;
        }

        private static byte[] Dechunk(byte[] body)
        {
            using var output = new MemoryStream();
            var position = 0;
            while (position < body.Length)
            {
                var lineEnd = IndexOfCrlf(body, position);
                if (lineEnd < 0)
                {
                    break;
                }

                var sizeText = Encoding.ASCII.GetString(body, position, lineEnd - position).Split(';')[0].Trim();
                if (!int.TryParse(sizeText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chunkSize) || chunkSize <= 0)
                {
                    break;
                }

                position = lineEnd + 2;
                if (position + chunkSize > body.Length)
                {
                    chunkSize = body.Length - position;
                }

                output.Write(body, position, chunkSize);
                position += chunkSize + 2; // skip the chunk data and its trailing CRLF
            }

            return output.ToArray();
        }

        private static int IndexOfCrlf(byte[] data, int start)
        {
            for (var i = start; i + 1 < data.Length; i++)
            {
                if (data[i] == '\r' && data[i + 1] == '\n')
                {
                    return i;
                }
            }

            return -1;
        }

        private static byte[] Decompress(byte[] body, string contentEncoding)
        {
            var encoding = contentEncoding.Trim().ToLowerInvariant();
            try
            {
                Stream Wrap(Stream source) => encoding switch
                {
                    "gzip" or "x-gzip" => new GZipStream(source, CompressionMode.Decompress),
                    "deflate" => new DeflateStream(source, CompressionMode.Decompress),
                    "br" => new BrotliStream(source, CompressionMode.Decompress),
                    _ => source,
                };

                if (encoding is not ("gzip" or "x-gzip" or "deflate" or "br"))
                {
                    return body;
                }

                using var input = new MemoryStream(body);
                using var decompressor = Wrap(input);
                using var output = new MemoryStream();
                decompressor.CopyTo(output);
                return output.ToArray();
            }
            catch (InvalidDataException)
            {
                return body;
            }
        }
    }
}
