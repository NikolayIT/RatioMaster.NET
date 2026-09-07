using System.Text.Json;
using System.Text.Json.Serialization;

namespace RatioMaster.Core.Clients;

/// <summary>The "peerId" object in clients.json: a fixed prefix plus a random-value spec.</summary>
internal sealed record PeerIdSpecEntry
{
    public string? Prefix { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<RandomValueKind>))]
    public RandomValueKind Type { get; set; } = RandomValueKind.Alphanumeric;

    public int Length { get; set; }

    public bool UrlEncode { get; set; }

    public bool UpperCase { get; set; }
}
