using System.Text;
using RatioMaster.Core.Bencode;

namespace RatioMaster.Core.Tests.Bencode;

public class BencodeWriterTests
{
    [Fact]
    public void WritesDictionaryKeysInRawByteOrder()
    {
        var dict = new BencodeDictionary()
            .Set("zebra", 1)
            .Set("apple", "x")
            .Set(BencodeString.FromLatin1("é").Text, new BencodeList());

        var expected = Encoding.Latin1.GetBytes("d5:apple1:x5:zebrai1e1:élee");
        Assert.Equal(expected, dict.ToBytes());
    }

    [Theory]
    [InlineData("i42e")]
    [InlineData("i-1e")]
    [InlineData("0:")]
    [InlineData("4:spam")]
    [InlineData("le")]
    [InlineData("de")]
    [InlineData("d3:cow3:moo4:spaml1:ai7eee")]
    [InlineData("d4:infod6:lengthi5e4:name3:abc12:piece lengthi16384e6:pieces0:ee")]
    public void RoundTripsCanonicalData(string text)
    {
        var data = Encoding.Latin1.GetBytes(text);
        Assert.Equal(data, BencodeParser.Parse(data).ToBytes());
    }

    [Fact]
    public void StringFromTextIsUtf8AndFromLatin1IsSingleByte()
    {
        Assert.Equal(2, new BencodeString("é").Length);
        Assert.Equal("é", new BencodeString("é").Utf8Text);
        Assert.Equal(1, BencodeString.FromLatin1("é").Length);
        Assert.Equal("é", BencodeString.FromLatin1("é").Text);
    }

    [Fact]
    public void StringsCompareByBytes()
    {
        Assert.Equal(new BencodeString("abc"), new BencodeString([(byte)'a', (byte)'b', (byte)'c']));
        Assert.NotEqual(new BencodeString("abc"), new BencodeString("abd"));
        Assert.Equal(new BencodeString("abc").GetHashCode(), new BencodeString("abc").GetHashCode());
        Assert.Equal(new BencodeInteger(5), new BencodeInteger(5));
    }

    [Fact]
    public void DisplayTextFlattensNestedValues()
    {
        var dict = new BencodeDictionary()
            .Set("n", 3)
            .Set("s", "text")
            .Set("l", new BencodeList([new BencodeInteger(1), new BencodeString("b")]))
            .Set("d", new BencodeDictionary().Set("k", "v"));

        Assert.Equal("3", dict.GetDisplayText("n"));
        Assert.Equal("text", dict.GetDisplayText("s"));
        Assert.Equal("[1, b]", dict.GetDisplayText("l"));
        Assert.Equal("{k: v}", dict.GetDisplayText("d"));
        Assert.Null(dict.GetDisplayText("missing"));
    }

    [Fact]
    public void RemoveDeletesEntries()
    {
        var dict = new BencodeDictionary().Set("a", 1);
        Assert.True(dict.Remove("a"));
        Assert.False(dict.ContainsKey("a"));
        Assert.False(dict.Remove("a"));
    }
}
