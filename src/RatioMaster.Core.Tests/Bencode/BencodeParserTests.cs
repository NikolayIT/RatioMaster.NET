using System.Text;
using RatioMaster.Core.Bencode;

namespace RatioMaster.Core.Tests.Bencode;

public class BencodeParserTests
{
    [Theory]
    [InlineData("i42e", 42)]
    [InlineData("i-7e", -7)]
    [InlineData("i0e", 0)]
    [InlineData("i9223372036854775807e", long.MaxValue)]
    public void ParsesIntegers(string text, long expected)
    {
        var value = Assert.IsType<BencodeInteger>(BencodeParser.Parse(Bytes(text)));
        Assert.Equal(expected, value.Value);
    }

    [Fact]
    public void ParsesStringsAsRawBytes()
    {
        var value = Assert.IsType<BencodeString>(BencodeParser.Parse(Bytes("4:spam")));
        Assert.Equal("spam", value.Text);

        var binary = new byte[] { (byte)'3', (byte)':', 0x00, 0xFF, 0x80 };
        var parsed = Assert.IsType<BencodeString>(BencodeParser.Parse(binary));
        Assert.Equal(new byte[] { 0x00, 0xFF, 0x80 }, parsed.Span.ToArray());
        Assert.Equal(new[] { (char)0x00, (char)0xFF, (char)0x80 }, parsed.Text.ToCharArray());
    }

    [Fact]
    public void ParsesEmptyString()
    {
        var value = Assert.IsType<BencodeString>(BencodeParser.Parse(Bytes("0:")));
        Assert.Equal(0, value.Length);
    }

    [Fact]
    public void ParsesLists()
    {
        var list = Assert.IsType<BencodeList>(BencodeParser.Parse(Bytes("l4:spami42ee")));
        Assert.Equal(2, list.Count);
        Assert.Equal("spam", Assert.IsType<BencodeString>(list[0]).Text);
        Assert.Equal(42, Assert.IsType<BencodeInteger>(list[1]).Value);
    }

    [Fact]
    public void ParsesDictionaries()
    {
        var dict = Assert.IsType<BencodeDictionary>(BencodeParser.Parse(Bytes("d3:cow3:moo4:spam4:eggse")));
        Assert.Equal(["cow", "spam"], dict.Keys);
        Assert.Equal("moo", dict.GetLatin1Text("cow"));
        Assert.Equal("eggs", dict.GetLatin1Text("spam"));
        Assert.Null(dict.GetInteger("cow"));
        Assert.Null(dict.GetString("missing"));
    }

    [Fact]
    public void RecordsRawByteRangesOfNestedValues()
    {
        var data = Bytes("d4:infod4:name3:abce3:zzzi1ee");
        var root = BencodeParser.ParseDictionary(data);
        var info = root.GetDictionary("info")!;
        Assert.Equal(7, info.RawOffset);
        Assert.Equal("d4:name3:abce".Length, info.RawLength);
        Assert.Equal("d4:name3:abce", Encoding.Latin1.GetString(data, info.RawOffset, info.RawLength));
        Assert.Equal(0, root.RawOffset);
        Assert.Equal(data.Length, root.RawLength);
        Assert.False(new BencodeInteger(1).HasRawBytes);
    }

    [Fact]
    public void ReportsConsumedBytesAndIgnoresTrailingData()
    {
        var value = BencodeParser.Parse(Bytes("i1eXYZ"), out var consumed);
        Assert.Equal(1, Assert.IsType<BencodeInteger>(value).Value);
        Assert.Equal(3, consumed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("i42")]
    [InlineData("ie")]
    [InlineData("i-e")]
    [InlineData("iabce")]
    [InlineData("i4 2e")]
    [InlineData("5:abc")]
    [InlineData("x")]
    [InlineData("-1:a")]
    [InlineData("l")]
    [InlineData("d")]
    [InlineData("d3:key")]
    [InlineData("di1e3:vale")]
    [InlineData("l4:spam")]
    public void RejectsMalformedData(string text)
    {
        Assert.Throws<BencodeException>(() => BencodeParser.Parse(Bytes(text)));
    }

    [Fact]
    public void RejectsExcessiveNesting()
    {
        var text = new string('l', 2000) + new string('e', 2000);
        Assert.Throws<BencodeException>(() => BencodeParser.Parse(Bytes(text)));
    }

    [Fact]
    public void ParseDictionaryRejectsOtherRoots()
    {
        Assert.Throws<BencodeException>(() => BencodeParser.ParseDictionary(Bytes("le")));
    }

    [Fact]
    public void TryParseReturnsFalseOnError()
    {
        Assert.False(BencodeParser.TryParse(Bytes("i42"), out var value));
        Assert.Null(value);
        Assert.True(BencodeParser.TryParse(Bytes("i42e"), out value));
        Assert.NotNull(value);
    }

    [Fact]
    public void LastDuplicateKeyWins()
    {
        var dict = BencodeParser.ParseDictionary(Bytes("d1:ai1e1:ai2ee"));
        Assert.Equal(2, dict.GetInteger("a"));
    }

    private static byte[] Bytes(string latin1) => Encoding.Latin1.GetBytes(latin1);
}
