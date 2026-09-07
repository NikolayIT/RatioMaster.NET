namespace RatioMaster.Core.Tests.Tracker
{
    using System.Text;

    using RatioMaster.Core.Tracker;

    public class HttpMessageFramingTests
    {
        [Fact]
        public void IncompleteHeadersAreNotComplete()
        {
            Assert.False(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n")));
        }

        [Fact]
        public void ContentLengthIsHonoured()
        {
            Assert.False(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nd1:")));
            Assert.True(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\nd1:ae")));
            Assert.True(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 200 OK\r\ncontent-length: 0\r\n\r\n")));
        }

        [Fact]
        public void ChunkedBodyIsCompleteAfterTheLastChunk()
        {
            const string head = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n";
            Assert.False(HttpMessageFraming.IsComplete(Bytes(head + "3\r\nabc\r\n")));
            Assert.False(HttpMessageFraming.IsComplete(Bytes(head + "3\r\nabc\r\n0\r\n")));
            Assert.True(HttpMessageFraming.IsComplete(Bytes(head + "3\r\nabc\r\n0\r\n\r\n")));
            Assert.True(HttpMessageFraming.IsComplete(Bytes(head + "3;ext=1\r\nabc\r\n0\r\nX-Trailer: 1\r\n\r\n")));
        }

        [Fact]
        public void WithoutFramingTheMessageEndsOnlyWithTheConnection()
        {
            Assert.False(HttpMessageFraming.IsComplete(Bytes("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\nd1:ae")));
        }

        [Fact]
        public void BodilessStatusesAreCompleteAtTheHeaders()
        {
            Assert.True(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 204 No Content\r\n\r\n")));
            Assert.True(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 304 Not Modified\r\nContent-Length: 10\r\n\r\n")));
        }

        [Fact]
        public void BareLineFeedsAreAcceptedAsHeaderTerminator()
        {
            Assert.True(HttpMessageFraming.IsComplete(Bytes("HTTP/1.1 200 OK\nContent-Length: 2\n\nok")));
        }

        private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);
    }
}
