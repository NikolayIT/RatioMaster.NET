using System.Collections;

namespace RatioMaster.Core.Bencode;

/// <summary>A bencode list.</summary>
public sealed class BencodeList : BencodeValue, IReadOnlyList<BencodeValue>
{
    private readonly List<BencodeValue> items = [];

    public BencodeList()
    {
    }

    public BencodeList(IEnumerable<BencodeValue> items)
    {
        this.items.AddRange(items);
    }

    public int Count => this.items.Count;

    public BencodeValue this[int index] => this.items[index];

    public void Add(BencodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.items.Add(value);
    }

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.WriteByte((byte)'l');
        foreach (var item in this.items)
        {
            item.WriteTo(stream);
        }

        stream.WriteByte((byte)'e');
    }

    public IEnumerator<BencodeValue> GetEnumerator() => this.items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
