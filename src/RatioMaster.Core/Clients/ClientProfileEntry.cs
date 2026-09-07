namespace RatioMaster.Core.Clients
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>One client entry in clients.json. Maps to a <see cref="ClientProfile"/>.</summary>
    internal sealed record ClientProfileEntry
    {
        public string? Name { get; set; }

        public string? Family { get; set; }

        public string? Version { get; set; }

        public string? HttpProtocol { get; set; }

        public bool HashUpperCase { get; set; }

        public RandomValueSpec? Key { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter<KeyRefreshPolicy>))]
        public KeyRefreshPolicy KeyRefresh { get; set; } = KeyRefreshPolicy.Never;

        public int? KeyRefreshMinutes { get; set; }

        public PeerIdSpecEntry? PeerId { get; set; }

        public List<string>? Headers { get; set; }

        public string? Query { get; set; }

        public int? DefaultNumWant { get; set; }

        public ClientMemoryScanSpec? MemoryScan { get; set; }

        public ClientProfile ToProfile()
        {
            if (string.IsNullOrWhiteSpace(this.Name)
                || string.IsNullOrWhiteSpace(this.Family)
                || string.IsNullOrWhiteSpace(this.Version)
                || string.IsNullOrWhiteSpace(this.HttpProtocol)
                || this.Key is null
                || this.PeerId is null
                || this.Headers is null
                || this.Query is null)
            {
                throw new InvalidOperationException($"Client entry '{this.Name ?? "(unnamed)"}' is missing required fields.");
            }

            return new ClientProfile
            {
                Name = this.Name,
                Family = this.Family,
                Version = this.Version,
                HttpProtocol = this.HttpProtocol,
                HashUpperCase = this.HashUpperCase,
                Key = this.Key,
                KeyRefresh = this.KeyRefresh,
                KeyRefreshMinutes = this.KeyRefreshMinutes ?? 10,
                PeerIdPrefix = this.PeerId.Prefix ?? string.Empty,
                PeerId = new RandomValueSpec
                {
                    Type = this.PeerId.Type,
                    Length = this.PeerId.Length,
                    UrlEncode = this.PeerId.UrlEncode,
                    UpperCase = this.PeerId.UpperCase,
                },
                Headers = this.Headers,
                Query = this.Query,
                DefaultNumWant = this.DefaultNumWant ?? 200,
                MemoryScan = this.MemoryScan,
            };
        }
    }
}
