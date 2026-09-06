namespace RatioMaster.Core.Bencode;

/// <summary>Base class of the four bencode value kinds.</summary>
public abstract class BencodeValue
{
    /// <summary>Offset of the raw bytes this value was parsed from, or -1 when the value was created in code.</summary>
    public int RawOffset { get; internal set; } = -1;

    /// <summary>Length of the raw bytes this value was parsed from.</summary>
    public int RawLength { get; internal set; }

    public bool HasRawBytes => RawOffset >= 0;

    public static BencodeValue Parse(ReadOnlySpan<byte> data) => BencodeParser.Parse(data);

    public abstract void WriteTo(Stream stream);

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        WriteTo(stream);
        return stream.ToArray();
    }
}
