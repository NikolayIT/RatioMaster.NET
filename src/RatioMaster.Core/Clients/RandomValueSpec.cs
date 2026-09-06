using System.Text.Json.Serialization;

namespace RatioMaster.Core.Clients;

/// <summary>
/// How to generate one random client value (peer id suffix or key). Mirrors the old
/// <c>GenerateIdString(keyType, keyLength, urlencoding, upperCase)</c> helper exactly.
/// </summary>
public sealed record RandomValueSpec
{
    [JsonConstructor]
    public RandomValueSpec()
    {
    }

    [JsonConverter(typeof(JsonStringEnumConverter<RandomValueKind>))]
    public RandomValueKind Type { get; set; } = RandomValueKind.Alphanumeric;

    public int Length { get; set; }

    /// <summary>When true, the generated value is percent-encoded (non-alphanumeric bytes become %xx).</summary>
    public bool UrlEncode { get; set; }

    /// <summary>When true, the value (or its %xx escapes when url-encoded) is upper-cased.</summary>
    public bool UpperCase { get; set; }
}
