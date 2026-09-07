namespace RatioMaster.Core.Clients
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true)]
    [JsonSerializable(typeof(ClientCatalogDocument))]
    internal sealed partial class ClientCatalogJsonContext : JsonSerializerContext;
}
