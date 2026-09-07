using RatioMaster.Core.Abstractions;
using RatioMaster.Core.Clients;
using RatioMaster.Core.MemoryScan;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Sessions;

/// <summary>
/// Resolves the client profile and identity for a torrent and builds a ready <see cref="TorrentSession"/>.
/// Identity comes from the settings when custom, otherwise from the running client's memory when that
/// profile supports it, otherwise from the generator.
/// </summary>
public sealed class TorrentSessionFactory
{
    private readonly ClientProfileCatalog catalog;
    private readonly ITrackerClient tracker;
    private readonly ClientIdentityGenerator generator;
    private readonly ClientValueScanner scanner;
    private readonly ILocalIpProvider localIpProvider;
    private readonly IRandomSource random;
    private readonly ISystemClock clock;

    public TorrentSessionFactory(
        ClientProfileCatalog catalog,
        ITrackerClient? tracker = null,
        ClientValueScanner? scanner = null,
        ILocalIpProvider? localIpProvider = null,
        IRandomSource? random = null,
        ISystemClock? clock = null)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.tracker = tracker ?? new TrackerClient();
        this.random = random ?? SystemRandomSource.Instance;
        this.generator = new ClientIdentityGenerator(this.random);
        this.scanner = scanner ?? new ClientValueScanner();
        this.localIpProvider = localIpProvider ?? LocalIpProvider.Instance;
        this.clock = clock ?? SystemClock.Instance;
    }

    public ClientProfileCatalog Catalog => this.catalog;

    /// <summary>Builds the identity a session would use, without creating the session (for the UI preview).</summary>
    public ClientIdentity CreateIdentity(TorrentSettings settings, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var profile = this.catalog.GetByName(settings.ClientName);
        var generated = this.generator.Generate(profile);

        if (settings.IdentityMode == IdentityMode.Custom)
        {
            return new ClientIdentity
            {
                PeerId = Or(settings.CustomPeerId, generated.PeerId),
                Key = Or(settings.CustomKey, generated.Key),
                Port = Or(settings.CustomPort, generated.Port),
                NumWant = Or(settings.CustomNumWant, generated.NumWant),
                Source = ClientIdentitySource.Custom,
            };
        }

        return this.scanner.TryScan(profile, generated, log) ?? generated;
    }

    /// <summary>Creates a session for a torrent with the given settings.</summary>
    public TorrentSession Create(TorrentDescriptor descriptor, TorrentSettings settings, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(settings);

        var profile = this.catalog.GetByName(settings.ClientName);
        var identity = this.CreateIdentity(settings, log);
        return new TorrentSession(descriptor, profile, identity, settings, this.tracker, this.localIpProvider, this.random, this.clock);
    }

    private static string Or(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
