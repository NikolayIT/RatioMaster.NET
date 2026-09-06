using System.Collections;

namespace RatioMaster.Core.Bencode;

/// <summary>A bencode list.</summary>
public sealed class BencodeList : BencodeValue, IReadOnlyList<BencodeValue>
{
    private readonly List<BencodeValue> _items = [];

    public BencodeList()
    {
    }

    public BencodeList(IEnumerable<BencodeValue> items)
    {
        _items.AddRange(items);
    }

    public int Count => _items.Count;

    public BencodeValue this[int index] => _items[index];

    public void Add(BencodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _items.Add(value);
    }

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.WriteByte((byte)'l');
        foreach (var item in _items)
        {
            item.WriteTo(stream);
        }

        stream.WriteByte((byte)'e');
    }

    public IEnumerator<BencodeValue> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
