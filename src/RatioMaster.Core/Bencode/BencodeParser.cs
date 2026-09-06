using System.Buffers.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>Recursive-descent bencode parser over a byte span. Records the raw byte range of every value.</summary>
public static class BencodeParser
{
    private const int MaxDepth = 512;

    /// <summary>Parses the first value in <paramref name="data"/>. Trailing bytes are ignored.</summary>
    public static BencodeValue Parse(ReadOnlySpan<byte> data) => Parse(data, out _);

    /// <summary>Parses the first value in <paramref name="data"/> and reports how many bytes it used.</summary>
    public static BencodeValue Parse(ReadOnlySpan<byte> data, out int consumed)
    {
        var position = 0;
        var value = ParseValue(data, ref position, 0);
        consumed = position;
        return value;
    }

    public static BencodeDictionary ParseDictionary(ReadOnlySpan<byte> data) =>
        Parse(data) as BencodeDictionary ?? throw new BencodeException("The root value is not a dictionary.");

    public static bool TryParse(ReadOnlySpan<byte> data, out BencodeValue? value)
    {
        try
        {
            value = Parse(data);
            return true;
        }
        catch (BencodeException)
        {
            value = null;
            return false;
        }
    }

    private static BencodeValue ParseValue(ReadOnlySpan<byte> data, ref int position, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new BencodeException("The data is nested too deeply.");
        }

        if (position >= data.Length)
        {
            throw new BencodeException($"Unexpected end of data at offset {position}.");
        }

        var start = position;
        BencodeValue value;
        switch (data[position])
        {
            case (byte)'i':
                value = ParseInteger(data, ref position);
                break;

            case (byte)'l':
            {
                position++;
                var list = new BencodeList();
                while (true)
                {
                    if (position >= data.Length)
                    {
                        throw new BencodeException($"Unterminated list starting at offset {start}.");
                    }

                    if (data[position] == (byte)'e')
                    {
                        position++;
                        break;
                    }

                    list.Add(ParseValue(data, ref position, depth + 1));
                }

                value = list;
                break;
            }

            case (byte)'d':
            {
                position++;
                var dictionary = new BencodeDictionary();
                while (true)
                {
                    if (position >= data.Length)
                    {
                        throw new BencodeException($"Unterminated dictionary starting at offset {start}.");
                    }

                    if (data[position] == (byte)'e')
                    {
                        position++;
                        break;
                    }

                    if (!IsDigit(data[position]))
                    {
                        throw new BencodeException($"Dictionary key at offset {position} is not a string.");
                    }

                    var key = ParseString(data, ref position);
                    var item = ParseValue(data, ref position, depth + 1);
                    dictionary.Set(key.Text, item);
                }

                value = dictionary;
                break;
            }

            case var b when IsDigit(b):
                value = ParseString(data, ref position);
                break;

            default:
                throw new BencodeException($"Unexpected byte 0x{data[position]:X2} at offset {position}.");
        }

        value.RawOffset = start;
        value.RawLength = position - start;
        return value;
    }

    private static BencodeInteger ParseInteger(ReadOnlySpan<byte> data, ref int position)
    {
        var start = position;
        position++;
        var end = data[position..].IndexOf((byte)'e');
        if (end < 0)
        {
            throw new BencodeException($"Unterminated integer at offset {start}.");
        }

        var digits = data.Slice(position, end);
        if (digits.Length == 0
            || !Utf8Parser.TryParse(digits, out long number, out var consumed)
            || consumed != digits.Length)
        {
            throw new BencodeException($"Invalid integer at offset {start}.");
        }

        position += end + 1;
        return new BencodeInteger(number);
    }

    private static BencodeString ParseString(ReadOnlySpan<byte> data, ref int position)
    {
        var start = position;
        var colon = data[position..].IndexOf((byte)':');
        if (colon < 0)
        {
            throw new BencodeException($"String length at offset {start} is not terminated by a colon.");
        }

        var lengthDigits = data.Slice(position, colon);
        if (lengthDigits.Length == 0
            || !Utf8Parser.TryParse(lengthDigits, out int length, out var consumed)
            || consumed != lengthDigits.Length
            || length < 0)
        {
            throw new BencodeException($"Invalid string length at offset {start}.");
        }

        position += colon + 1;
        if (position + length > data.Length)
        {
            throw new BencodeException($"String at offset {start} declares {length} bytes but only {data.Length - position} remain.");
        }

        var value = new BencodeString(data.Slice(position, length));
        position += length;
        return value;
    }

    private static bool IsDigit(byte b) => b is >= (byte)'0' and <= (byte)'9';
}
