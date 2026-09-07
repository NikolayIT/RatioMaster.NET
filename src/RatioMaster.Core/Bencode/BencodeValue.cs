namespace RatioMaster.Core.Bencode;

/// <summary>Base class of the four bencode value kinds.</summary>
public abstract class BencodeValue
{
    /// <summary>Gets the offset of the raw bytes this value was parsed from, or -1 when the value was created in code.</summary>
    public int RawOffset { get; internal set; } = -1;

    /// <summary>Gets the length of the raw bytes this value was parsed from.</summary>
    public int RawLength { get; internal set; }

    public bool HasRawBytes => this.RawOffset >= 0;

    public static BencodeValue Parse(ReadOnlySpan<byte> data) => BencodeParser.Parse(data);

    public abstract void WriteTo(Stream stream);

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        this.WriteTo(stream);
        return stream.ToArray();
    }
}
