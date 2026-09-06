using System.Text;

namespace RatioMaster.Core.Clients;

/// <summary>
/// Percent-encoding as the old app did it (RandomStringGenerator.Generate(string, upperCase)):
/// ASCII letters and digits below 127 are kept, every other byte becomes %xx.
/// </summary>
public static class PercentEncoding
{
    /// <summary>Percent-encodes the Latin-1 bytes of <paramref name="value"/>.</summary>
    public static string Encode(string value, bool upperCase = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        var builder = new StringBuilder(value.Length * 3);
        foreach (var ch in value)
        {
            AppendByte(builder, ch, upperCase);
        }

        return builder.ToString();
    }

    /// <summary>Percent-encodes raw bytes.</summary>
    public static string Encode(ReadOnlySpan<byte> bytes, bool upperCase = false)
    {
        var builder = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
        {
            AppendByte(builder, (char)b, upperCase);
        }

        return builder.ToString();
    }

    private static void AppendByte(StringBuilder builder, char ch, bool upperCase)
    {
        if (char.IsLetterOrDigit(ch) && ch < 127)
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
