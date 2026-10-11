namespace RatioMaster.Core.Clients
{
    using System.Text;

    /// <summary>
    /// Percent-encoding as the emulated clients do it: ASCII letters and digits are kept, and so are the client's
    /// own unreserved characters (<see cref="ClientProfile.Unreserved"/>); every other byte becomes %xx. With no
    /// unreserved characters this is what the old app did (RandomStringGenerator.Generate(string, upperCase)).
    /// </summary>
    public static class PercentEncoding
    {
        /// <summary>Percent-encodes the Latin-1 bytes of <paramref name="value"/>.</summary>
        public static string Encode(string value, bool upperCase = false, string unreserved = "")
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(unreserved);
            var builder = new StringBuilder(value.Length * 3);
            foreach (var ch in value)
            {
                AppendByte(builder, ch, upperCase, unreserved);
            }

            return builder.ToString();
        }

        /// <summary>Percent-encodes raw bytes.</summary>
        public static string Encode(ReadOnlySpan<byte> bytes, bool upperCase = false, string unreserved = "")
        {
            ArgumentNullException.ThrowIfNull(unreserved);
            var builder = new StringBuilder(bytes.Length * 3);
            foreach (var b in bytes)
            {
                AppendByte(builder, (char)b, upperCase, unreserved);
            }

            return builder.ToString();
        }

        private static void AppendByte(StringBuilder builder, char ch, bool upperCase, string unreserved)
        {
            if (ch < 127 && (char.IsLetterOrDigit(ch) || unreserved.Contains(ch, StringComparison.Ordinal)))
            {
                builder.Append(ch);
                return;
            }

            builder.Append('%');
            var hex = Convert.ToString(ch, 16);
            if (upperCase)
            {
                hex = hex.ToUpperInvariant();
            }

            if (hex.Length == 1)
            {
                builder.Append('0');
            }

            builder.Append(hex);
        }
    }
}
