using System.Text.Json;
using System.Text.Json.Serialization;

namespace RatioMaster.Core.Clients;

/// <summary>The on-disk shape of clients.json.</summary>
internal sealed record ClientCatalogDocument
{
    public string? Default { get; set; }

    public List<ClientProfileEntry> Clients { get; set; } = [];
}

/// <summary>One client entry in clients.json. Maps to a <see cref="ClientProfile"/>.</summary>
internal sealed record ClientProfileEntry
{
    public string? Name { get; set; }

    public string? Family { get; set; }

    public string? Version { get; set; }

    public string? HttpProtocol { get; set; }

    public bool HashUpperCase { get; set; }

    public RandomValueSpec? Key { get; set; }

    public PeerIdSpecEntry? PeerId { get; set; }

    public List<string>? Headers { get; set; }

    public string? Query { get; set; }

    public int? DefaultNumWant { get; set; }

    public ClientMemoryScanSpec? MemoryScan { get; set; }

    public ClientProfile ToProfile()
    {
        if (string.IsNullOrWhiteSpace(Name)
            || string.IsNullOrWhiteSpace(Family)
            || string.IsNullOrWhiteSpace(Version)
            || string.IsNullOrWhiteSpace(HttpProtocol)
            || Key is null
            || PeerId is null
            || Headers is null
            || Query is null)
        {
            throw new InvalidOperationException($"Client entry '{Name ?? "(unnamed)"}' is missing required fields.");
        }

        return new ClientProfile
        {
            Name = Name,
            Family = Family,
            Version = Version,
            HttpProtocol = HttpProtocol,
            HashUpperCase = HashUpperCase,
            Key = Key,
            PeerIdPrefix = PeerId.Prefix ?? string.Empty,
            PeerId = new RandomValueSpec
            {
                Type = PeerId.Type,
                Length = PeerId.Length,
                UrlEncode = PeerId.UrlEncode,
                UpperCase = PeerId.UpperCase,
            },
            Headers = Headers,
            Query = Query,
            DefaultNumWant = DefaultNumWant ?? 200,
            MemoryScan = MemoryScan,
        };
    }
}

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

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(ClientCatalogDocument))]
internal sealed partial class ClientCatalogJsonContext : JsonSerializerContext;
