namespace RatioMaster.Core.Tests.Clients
{
    using System.Text;

    using RatioMaster.Core.Clients;

    public class PercentEncodingTests
    {
        [Fact]
        public void KeepsAsciiLettersAndDigits()
        {
            Assert.Equal("abcXYZ019", PercentEncoding.Encode("abcXYZ019"));
        }

        [Fact]
        public void EscapesEverythingElse()
        {
            Assert.Equal("a%20b", PercentEncoding.Encode("a b"));
            Assert.Equal("%2d", PercentEncoding.Encode("-"));
            Assert.Equal("%2D", PercentEncoding.Encode("-", upperCase: true));
        }

        [Fact]
        public void EscapesBytesAboveAscii()
        {
            Assert.Equal("%00%ff", PercentEncoding.Encode(new byte[] { 0x00, 0xFF }));
            Assert.Equal("%00%FF", PercentEncoding.Encode(new byte[] { 0x00, 0xFF }, upperCase: true));
        }

        [Fact]
        public void InfoHashEncoderMatchesRawByteEncoding()
        {
            var hash = new byte[20];
            for (var i = 0; i < hash.Length; i++)
            {
                hash[i] = (byte)i;
            }

            var lower = InfoHashEncoder.Encode(hash, upperCase: false);
            Assert.Equal(PercentEncoding.Encode(hash), lower);
            Assert.StartsWith("%00%01%02%03%04%05%06%07%08%09", lower, StringComparison.Ordinal);

            var hex = Convert.ToHexString(hash);
            Assert.Equal(lower, InfoHashEncoder.EncodeHex(hex, upperCase: false));
        }

        [Fact]
        public void InfoHashEncoderKeepsPrintableAsciiBytesLiteral()
        {
            var hash = Encoding.ASCII.GetBytes("ABCDEFGHIJ0123456789");
            Assert.Equal("ABCDEFGHIJ0123456789", InfoHashEncoder.Encode(hash, upperCase: false));
        }
    }
}
