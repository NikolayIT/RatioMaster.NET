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

        /// <summary>
        /// One info hash holding every character the clients disagree on ("-_.!~*()'/"), then letters and digits, then
        /// bytes everybody escapes. Each verified family keeps exactly the characters its own code keeps.
        /// </summary>
        [Theory]
        [InlineData("qBittorrent 5.2.4", "-_.!~*()%27%2faZ09%00%20%25%26%ff%80")]
        [InlineData("Deluge 2.2.0", "-_.!~*()%27%2faZ09%00%20%25%26%ff%80")]
        [InlineData("uTorrent 3.6.0", "-_.%21~%2a%28%29%27%2faZ09%00%20%25%26%ff%80")]
        [InlineData("BitTorrent 7.11.0 (47255)", "-_.%21~%2a%28%29%27%2faZ09%00%20%25%26%ff%80")]
        [InlineData("Transmission 3.00", "-_.%21~%2a%28%29%27%2faZ09%00%20%25%26%ff%80")]
        [InlineData("rTorrent 0.16.25", "-%5F%2E%21%7E%2A%28%29%27%2FaZ09%00%20%25%26%FF%80")]
        [InlineData("Vuze 5.7.5.0", "-_.%21%7E*%28%29%27%2FaZ09%00%20%25%26%FF%80")]
        [InlineData("BitTornado 0.3.18", "-_.%21%7E%2A%28%29%27/aZ09%00%20%25%26%FF%80")]
        [InlineData("BitComet 1.20", "%2D%5F%2E%21%7E%2A%28%29%27%2FaZ09%00%20%25%26%FF%80")]
        public void EncodesTheInfoHashLikeTheRealClient(string profileName, string expected)
        {
            var hash = Encoding.Latin1.GetBytes("-_.!~*()'/aZ09\0 %&ÿ\u0080");
            Assert.Equal(20, hash.Length);

            var profile = ClientProfileCatalog.Load().GetByName(profileName);
            Assert.Equal(profileName, profile.Name);
            Assert.Equal(expected, InfoHashEncoder.Encode(hash, profile.HashUpperCase, profile.Unreserved));
        }

        [Fact]
        public void EscapesBytesAboveAscii()
        {
            Assert.Equal("%00%ff", PercentEncoding.Encode(new byte[] { 0x00, 0xFF }));
            Assert.Equal("%00%FF", PercentEncoding.Encode(new byte[] { 0x00, 0xFF }, upperCase: true));
        }

        [Fact]
        public void DecodesEscapesBackToBytes()
        {
            Assert.Equal(Encoding.Latin1.GetBytes("-UT360S-\u0096¸~"), PercentEncoding.Decode("-UT360S-%96%b8%7E"));
        }

        [Fact]
        public void DecodeKeepsAPercentThatStartsNoEscape()
        {
            Assert.Equal(Encoding.Latin1.GetBytes("%zz%4"), PercentEncoding.Decode("%zz%4"));
        }

        [Fact]
        public void DecodeUndoesEncode()
        {
            var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            Assert.Equal(bytes, PercentEncoding.Decode(PercentEncoding.Encode(bytes, upperCase: true, unreserved: "-._~")));
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
