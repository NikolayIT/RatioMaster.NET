using System.Globalization;
using System.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>A bencode integer (64-bit).</summary>
public sealed class BencodeInteger : BencodeValue, IEquatable<BencodeInteger>
{
    public BencodeInteger(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Write(Encoding.ASCII.GetBytes("i" + Value.ToString(CultureInfo.InvariantCulture) + "e"));
    }

    public bool Equals(BencodeInteger? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as BencodeInteger);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
