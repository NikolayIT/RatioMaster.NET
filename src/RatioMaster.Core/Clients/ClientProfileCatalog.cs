using System.Text.Json;

namespace RatioMaster.Core.Clients;

/// <summary>
/// The set of client emulation profiles. Loads the built-in clients.json and, optionally, a
/// user clients.json that adds or overrides profiles by name.
/// </summary>
public sealed class ClientProfileCatalog
{
    private const string EmbeddedResourceName = "RatioMaster.Core.Clients.clients.json";

    private readonly Dictionary<string, ClientProfile> byName;
    private readonly List<ClientProfile> profiles;

    private ClientProfileCatalog(List<ClientProfile> profiles, string defaultName)
    {
        this.profiles = profiles;
        this.byName = profiles.ToDictionary(p => p.Name, StringComparer.Ordinal);
        this.DefaultName = defaultName;
        this.Families = profiles.Select(p => p.Family).Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>All profiles in catalog order (family order, newest version first per family).</summary>
    public IReadOnlyList<ClientProfile> Profiles => this.profiles;

    /// <summary>Distinct family names in catalog order.</summary>
    public IReadOnlyList<string> Families { get; }

    /// <summary>The name of the default profile.</summary>
    public string DefaultName { get; }

    public ClientProfile Default => this.byName[this.DefaultName];

    /// <summary>Loads the built-in catalog, then merges a user file when present. Throws when the user file is unusable.</summary>
    public static ClientProfileCatalog Load(string? userFilePath = null)
    {
        var document = ReadEmbedded();
        if (userFilePath is not null && File.Exists(userFilePath))
        {
            var userDocument = ReadFile(userFilePath);
            document = Merge(document, userDocument);
        }

        return FromDocument(document);
    }

    /// <summary>
    /// Like <see cref="Load(string?)"/>, but a broken user file never prevents the catalog from loading: the
    /// built-in profiles are returned and <paramref name="userFileError"/> says what was wrong with the file.
    /// </summary>
    public static ClientProfileCatalog Load(string? userFilePath, out string? userFileError)
    {
        try
        {
            userFileError = null;
            return Load(userFilePath);
        }
        catch (Exception ex) when (userFilePath is not null && ex is InvalidOperationException or JsonException)
        {
            userFileError = ex.Message;
            return Load();
        }
    }

    /// <summary>Parses a catalog from JSON text (used by tests and the user-file loader).</summary>
    public static ClientProfileCatalog Parse(string json)
    {
        var document = Deserialize(json) ?? throw new InvalidOperationException("The client catalog is empty.");
        return FromDocument(document);
    }

    public bool Contains(string name) => this.byName.ContainsKey(name);

    public ClientProfile GetByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return this.byName.TryGetValue(name, out var profile) ? profile : this.Default;
    }

    public bool TryGet(string name, out ClientProfile profile) => this.byName.TryGetValue(name, out profile!);

    /// <summary>Versions available for a family, in catalog order.</summary>
    public IReadOnlyList<string> VersionsOf(string family) =>
        this.profiles.Where(p => string.Equals(p.Family, family, StringComparison.Ordinal)).Select(p => p.Version).ToArray();

    private static ClientProfileCatalog FromDocument(ClientCatalogDocument document)
    {
        if (document.Clients.Count == 0)
        {
            throw new InvalidOperationException("The client catalog has no clients.");
        }

        var profiles = document.Clients.Select(c => c.ToProfile()).ToList();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            if (!names.Add(profile.Name))
            {
                throw new InvalidOperationException($"Duplicate client name '{profile.Name}' in the catalog.");
            }
        }

        var defaultName = document.Default is { Length: > 0 } d && names.Contains(d) ? d : profiles[0].Name;
        return new ClientProfileCatalog(profiles, defaultName);
    }

    private static ClientCatalogDocument Merge(ClientCatalogDocument builtIn, ClientCatalogDocument user)
    {
        var merged = new List<ClientProfileEntry>(builtIn.Clients);
        foreach (var entry in user.Clients)
        {
            var index = merged.FindIndex(c => string.Equals(c.Name, entry.Name, StringComparison.Ordinal));
            if (index >= 0)
            {
                merged[index] = entry;
            }
            else
            {
                merged.Add(entry);
            }
        }

        return new ClientCatalogDocument
        {
            Default = user.Default ?? builtIn.Default,
            Clients = merged,
        };
    }

    private static ClientCatalogDocument ReadEmbedded()
    {
        using var stream = typeof(ClientProfileCatalog).Assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return Deserialize(reader.ReadToEnd()) ?? throw new InvalidOperationException("The built-in client catalog is empty.");
    }

    private static ClientCatalogDocument ReadFile(string path)
    {
        try
        {
            return Deserialize(File.ReadAllText(path)) ?? throw new InvalidOperationException("The user client catalog is empty.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException($"Cannot read the user client catalog '{path}': {ex.Message}", ex);
        }
    }

    private static ClientCatalogDocument? Deserialize(string json) =>
        JsonSerializer.Deserialize(json, ClientCatalogJsonContext.Default.ClientCatalogDocument);
}
