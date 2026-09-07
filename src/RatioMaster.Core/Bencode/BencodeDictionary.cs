using System.Collections;
using System.Text;

namespace RatioMaster.Core.Bencode;

/// <summary>
/// A bencode dictionary. Keys are kept as Latin-1 decoded strings and ordered ordinally, which is the
/// raw byte order bencode requires.
/// </summary>
public sealed class BencodeDictionary : BencodeValue, IReadOnlyDictionary<string, BencodeValue>
{
    private readonly SortedDictionary<string, BencodeValue> items = new(StringComparer.Ordinal);

    public int Count => this.items.Count;

    public IEnumerable<string> Keys => this.items.Keys;

    public IEnumerable<BencodeValue> Values => this.items.Values;

    public BencodeValue this[string key] => this.items[key];

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

    public bool ContainsKey(string key) => this.items.ContainsKey(key);

    public bool TryGetValue(string key, out BencodeValue value) => this.items.TryGetValue(key, out value!);

    /// <summary>Adds or replaces a value.</summary>
    public BencodeDictionary Set(string key, BencodeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        this.items[key] = value;
        return this;
    }

    public BencodeDictionary Set(string key, string utf8Text) => this.Set(key, new BencodeString(utf8Text));

    public BencodeDictionary Set(string key, long value) => this.Set(key, new BencodeInteger(value));

    public bool Remove(string key) => this.items.Remove(key);

    public BencodeString? GetString(string key) => this.TryGetValue(key, out var v) ? v as BencodeString : null;

    public long? GetInteger(string key) => this.TryGetValue(key, out var v) && v is BencodeInteger i ? i.Value : null;

    public BencodeList? GetList(string key) => this.TryGetValue(key, out var v) ? v as BencodeList : null;

    public BencodeDictionary? GetDictionary(string key) => this.TryGetValue(key, out var v) ? v as BencodeDictionary : null;

    /// <summary>UTF-8 text of a string entry, or null when missing or not a string.</summary>
    public string? GetUtf8Text(string key) => this.GetString(key)?.Utf8Text;

    /// <summary>Latin-1 text of a string entry, or null when missing or not a string.</summary>
    public string? GetLatin1Text(string key) => this.GetString(key)?.Text;

    /// <summary>Text of an entry as the old app displayed response values, or null when missing.</summary>
    public string? GetDisplayText(string key) => this.TryGetValue(key, out var v) ? DisplayText(v) : null;

    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.WriteByte((byte)'d');
        foreach (var (key, value) in this.items)
        {
            new BencodeString(Encoding.Latin1.GetBytes(key)).WriteTo(stream);
            value.WriteTo(stream);
        }

        stream.WriteByte((byte)'e');
    }

    public IEnumerator<KeyValuePair<string, BencodeValue>> GetEnumerator() => this.items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
