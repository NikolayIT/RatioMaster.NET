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
    private readonly ClientProfileCatalog _catalog;
    private readonly ITrackerClient _tracker;
    private readonly ClientIdentityGenerator _generator;
    private readonly ClientValueScanner _scanner;
    private readonly ILocalIpProvider _localIpProvider;
    private readonly IRandomSource _random;
    private readonly ISystemClock _clock;

    public TorrentSessionFactory(
        ClientProfileCatalog catalog,
        ITrackerClient? tracker = null,
        ClientValueScanner? scanner = null,
        ILocalIpProvider? localIpProvider = null,
        IRandomSource? random = null,
        ISystemClock? clock = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _tracker = tracker ?? new TrackerClient();
        _random = random ?? SystemRandomSource.Instance;
        _generator = new ClientIdentityGenerator(_random);
        _scanner = scanner ?? new ClientValueScanner();
        _localIpProvider = localIpProvider ?? LocalIpProvider.Instance;
        _clock = clock ?? SystemClock.Instance;
    }

    public ClientProfileCatalog Catalog => _catalog;

    /// <summary>Builds the identity a session would use, without creating the session (for the UI preview).</summary>
    public ClientIdentity CreateIdentity(TorrentSettings settings, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var profile = _catalog.GetByName(settings.ClientName);
        var generated = _generator.Generate(profile);

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

        return _scanner.TryScan(profile, generated, log) ?? generated;
    }

    /// <summary>Creates a session for a torrent with the given settings.</summary>
    public TorrentSession Create(TorrentDescriptor descriptor, TorrentSettings settings, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(settings);

        var profile = _catalog.GetByName(settings.ClientName);
        var identity = CreateIdentity(settings, log);
        return new TorrentSession(descriptor, profile, identity, settings, _tracker, _localIpProvider, _random, _clock);
    }

    private static string Or(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
