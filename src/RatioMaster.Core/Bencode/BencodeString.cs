using System.Globalization;
using System.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>A bencode byte string. Bencode strings are raw bytes; text views are provided for convenience.</summary>
public sealed class BencodeString : BencodeValue, IEquatable<BencodeString>
{
    private readonly byte[] bytes;

    public BencodeString(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        this.bytes = bytes;
    }

    public BencodeString(ReadOnlySpan<byte> bytes)
        : this(bytes.ToArray())
    {
    }

    /// <summary>Creates a string from UTF-8 text.</summary>
    public BencodeString(string text)
        : this(Encoding.UTF8.GetBytes(text))
    {
    }

    public ReadOnlyMemory<byte> Bytes => this.bytes;

    public ReadOnlySpan<byte> Span => this.bytes;

    public int Length => this.bytes.Length;

    /// <summary>
    /// Gets the lossless one-byte-per-char view (ISO-8859-1). Dictionary keys and tracker responses use this view so
    /// that ordinal string comparison equals raw byte comparison.
    /// </summary>
    public string Text => Encoding.Latin1.GetString(this.bytes);

    /// <summary>Gets UTF-8 view, which is what torrent files use for names and paths.</summary>
    public string Utf8Text => Encoding.UTF8.GetString(this.bytes);

    /// <summary>Creates a string whose bytes are the Latin-1 encoding of <paramref name="text"/>.</summary>
    public static BencodeString FromLatin1(string text) => new(Encoding.Latin1.GetBytes(text));

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = Encoding.ASCII.GetBytes(this.bytes.Length.ToString(CultureInfo.InvariantCulture) + ":");
        stream.Write(header);
        stream.Write(this.bytes);
    }

    public bool Equals(BencodeString? other) => other is not null && this.bytes.AsSpan().SequenceEqual(other.bytes);

    public override bool Equals(object? obj) => this.Equals(obj as BencodeString);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.AddBytes(this.bytes);
        return hash.ToHashCode();
    }

    public override string ToString() => this.Utf8Text;
}
