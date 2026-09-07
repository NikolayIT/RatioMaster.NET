using System.Globalization;
using System.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>A bencode integer (64-bit).</summary>
public sealed class BencodeInteger : BencodeValue, IEquatable<BencodeInteger>
{
    public BencodeInteger(long value)
    {
        this.Value = value;
    }

    public long Value { get; }

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Write(Encoding.ASCII.GetBytes("i" + this.Value.ToString(CultureInfo.InvariantCulture) + "e"));
    }

    public bool Equals(BencodeInteger? other) => other is not null && other.Value == this.Value;

    public override bool Equals(object? obj) => this.Equals(obj as BencodeInteger);

    public override int GetHashCode() => this.Value.GetHashCode();

    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);
}
