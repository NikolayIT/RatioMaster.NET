namespace RatioMaster.Core.Clients
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>The on-disk shape of clients.json.</summary>
    internal sealed record ClientCatalogDocument
    {
        public string? Default { get; set; }

        public List<ClientProfileEntry> Clients { get; set; } = [];
    }
}
