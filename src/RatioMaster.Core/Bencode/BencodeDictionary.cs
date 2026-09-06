using System.Collections;
using System.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>
/// A bencode dictionary. Keys are kept as Latin-1 decoded strings and ordered ordinally, which is the
/// raw byte order bencode requires.
/// </summary>
public sealed class BencodeDictionary : BencodeValue, IReadOnlyDictionary<string, BencodeValue>
{
    private readonly SortedDictionary<string, BencodeValue> _items = new(StringComparer.Ordinal);

    public int Count => _items.Count;

    public IEnumerable<string> Keys => _items.Keys;

    public IEnumerable<BencodeValue> Values => _items.Values;

    public BencodeValue this[string key] => _items[key];

    public bool ContainsKey(string key) => _items.ContainsKey(key);

    public bool TryGetValue(string key, out BencodeValue value) => _items.TryGetValue(key, out value!);

    /// <summary>Adds or replaces a value.</summary>
    public BencodeDictionary Set(string key, BencodeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        _items[key] = value;
        return this;
    }

    public BencodeDictionary Set(string key, string utf8Text) => Set(key, new BencodeString(utf8Text));

    public BencodeDictionary Set(string key, long value) => Set(key, new BencodeInteger(value));

    public bool Remove(string key) => _items.Remove(key);

    public BencodeString? GetString(string key) => TryGetValue(key, out var v) ? v as BencodeString : null;

    public long? GetInteger(string key) => TryGetValue(key, out var v) && v is BencodeInteger i ? i.Value : null;

    public BencodeList? GetList(string key) => TryGetValue(key, out var v) ? v as BencodeList : null;

    public BencodeDictionary? GetDictionary(string key) => TryGetValue(key, out var v) ? v as BencodeDictionary : null;

    /// <summary>UTF-8 text of a string entry, or null when missing or not a string.</summary>
    public string? GetUtf8Text(string key) => GetString(key)?.Utf8Text;

    /// <summary>Latin-1 text of a string entry, or null when missing or not a string.</summary>
    public string? GetLatin1Text(string key) => GetString(key)?.Text;

    /// <summary>Text of an entry as the old app displayed response values, or null when missing.</summary>
    public string? GetDisplayText(string key) => TryGetValue(key, out var v) ? DisplayText(v) : null;

    public static string DisplayText(BencodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value switch
        {
            BencodeString s => s.Text,
            BencodeInteger i => i.ToString(),
            BencodeList l => "[" + string.Join(", ", l.Select(DisplayText)) + "]",
            BencodeDictionary d => "{" + string.Join(", ", d.Select(p => p.Key + ": " + DisplayText(p.Value))) + "}",
            _ => value.ToString() ?? string.Empty,
        };
    }

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.WriteByte((byte)'d');
        foreach (var (key, value) in _items)
        {
            new BencodeString(Encoding.Latin1.GetBytes(key)).WriteTo(stream);
            value.WriteTo(stream);
        }

        stream.WriteByte((byte)'e');
    }

    public IEnumerator<KeyValuePair<string, BencodeValue>> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
