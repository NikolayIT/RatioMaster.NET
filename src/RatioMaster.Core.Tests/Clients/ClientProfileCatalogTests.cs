using RatioMaster.Core.Clients;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Tests.Clients;

public class ClientProfileCatalogTests
{
    private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

    [Fact]
    public void LoadsAllProfilesFromTheBuiltInCatalog()
    {
        // 41 inherited from 0.43 plus qBittorrent.
        Assert.Equal(42, Catalog.Profiles.Count);
    }

    [Fact]
    public void ProfileNamesAreUnique()
    {
        var names = Catalog.Profiles.Select(p => p.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DefaultIsTheCurrentQBittorrent()
    {
        Assert.Equal("qBittorrent 5.2.3", Catalog.DefaultName);
        Assert.Equal("qBittorrent 5.2.3", Catalog.Default.Name);
        Assert.Equal(TorrentSettings.DefaultClientName, Catalog.DefaultName);
    }

    [Fact]
    public void FamiliesAreListedNewestFirst()
    {
        string[] expected =
        [
            "qBittorrent", "uTorrent", "BitComet", "Azureus", "Vuze", "BitTorrent", "Transmission", "ABC",
            "BitLord", "BTuga", "BitTornado", "Burst", "BitTyrant", "BitSpirit", "Deluge", "KTorrent", "Gnome BT",
        ];
        Assert.Equal(expected, Catalog.Families);
    }

    [Theory]
    [InlineData("uTorrent", new[] { "3.3.2", "3.3.0", "3.2.0", "2.0.1 (build 19078)", "1.8.5 (build 17414)", "1.8.1-beta(11903)", "1.8.0", "1.7.7", "1.7.6", "1.7.5", "1.6.1", "1.6" })]
    [InlineData("BitComet", new[] { "1.20", "1.03", "0.98", "0.96", "0.93", "0.92" })]
    [InlineData("Azureus", new[] { "3.1.1.0", "3.0.5.0", "3.0.4.2", "3.0.3.4", "3.0.2.2", "2.5.0.4" })]
    [InlineData("Vuze", new[] { "4.2.0.8" })]
    [InlineData("BitTorrent", new[] { "6.0.3 (8642)" })]
    [InlineData("Transmission", new[] { "2.82 (14160)", "2.92 (14714)" })]
    [InlineData("BitSpirit", new[] { "3.6.0.200", "3.1.0.077" })]
    [InlineData("Deluge", new[] { "1.2.0", "0.5.8.7", "0.5.8.6" })]
    public void VersionsPerFamilyMatchTheOldUi(string family, string[] expected)
    {
        Assert.Equal(expected, Catalog.VersionsOf(family));
    }

    [Fact]
    public void OnlyParseableClientsHaveAMemoryScanSpec()
    {
        // uTorrent (12) + BitComet (6) + Azureus (6) + Vuze (1) + ABC (1) = 26; the rest cannot be read from a process.
        Assert.Equal(26, Catalog.Profiles.Count(p => p.CanScanMemory));

        Assert.True(Catalog.GetByName("uTorrent 3.3.2").CanScanMemory);
        Assert.False(Catalog.GetByName("Deluge 1.2.0").CanScanMemory);
        Assert.False(Catalog.GetByName("BitTorrent 6.0.3 (8642)").CanScanMemory);
        Assert.False(Catalog.GetByName("Transmission 2.92 (14714)").CanScanMemory);
    }

    [Fact]
    public void UTorrent332ProfileHasTheExpectedEmulation()
    {
        var p = Catalog.GetByName("uTorrent 3.3.2");
        Assert.Equal("uTorrent", p.Family);
        Assert.Equal("3.3.2", p.Version);
        Assert.Equal("HTTP/1.1", p.HttpProtocol);
        Assert.False(p.HashUpperCase);
        Assert.Equal(RandomValueKind.Hex, p.Key.Type);
        Assert.Equal(8, p.Key.Length);
        Assert.True(p.Key.UpperCase);
        Assert.Equal("-UT3320-%18w", p.PeerIdPrefix);
        Assert.Equal(RandomValueKind.Random, p.PeerId.Type);
        Assert.Equal(10, p.PeerId.Length);
        Assert.True(p.PeerId.UrlEncode);
        Assert.Equal(["Host: {host}", "User-Agent: uTorrent/3320", "Accept-Encoding: gzip"], p.Headers);
        Assert.Equal(200, p.DefaultNumWant);
        Assert.Equal("uTorrent", p.MemoryScan!.ProcessName);
        Assert.Equal("&peer_id=-UT3320-", p.MemoryScan.SearchString);
        Assert.Equal(200_000_000, p.MemoryScan.MaxOffset);
    }

    [Fact]
    public void QBittorrentMatchesLibtorrentsAnnounce()
    {
        // Derived from libtorrent RC_2_0: http_tracker_connection.cpp builds the query,
        // http_connection.cpp the headers, generate_fingerprint the peer id prefix.
        var p = Catalog.GetByName("qBittorrent 5.2.3");

        Assert.Equal("-qB5230-", p.PeerIdPrefix);
        Assert.Equal(RandomValueKind.UrlSafe, p.PeerId.Type);
        Assert.Equal(12, p.PeerId.Length);
        Assert.False(p.PeerId.UrlEncode);
        Assert.False(p.HashUpperCase);
        Assert.Equal(RandomValueKind.Hex, p.Key.Type);
        Assert.Equal(8, p.Key.Length);
        Assert.True(p.Key.UpperCase);
        Assert.Equal(
            ["Host: {host}", "User-Agent: qBittorrent/5.2.3", "Accept-Encoding: gzip", "Connection: close"],
            p.Headers);
        Assert.EndsWith("&compact=1&no_peer_id=1&supportcrypto=1&redundant=0", p.Query, StringComparison.Ordinal);
        Assert.Null(p.MemoryScan);
    }

    [Fact]
    public void AbcUsesTheClassDefaultScanOffsets()
    {
        var p = Catalog.GetByName("ABC 3.1");
        Assert.Equal("abc", p.MemoryScan!.ProcessName);
        Assert.Equal(10_000_000, p.MemoryScan.StartOffset);
        Assert.Equal(25_000_000, p.MemoryScan.MaxOffset);
    }

    [Fact]
    public void VuzeScansTheLowercaseAzureusProcess()
    {
        Assert.Equal("azureus", Catalog.GetByName("Vuze 4.2.0.8").MemoryScan!.ProcessName);
    }

    [Fact]
    public void DelugeQueryDoesNotDuplicateTheEventParameter()
    {
        // 0.43 used "&event={event}", which produced "&event=&event=started"; real Deluge sends it once.
        var query = Catalog.GetByName("Deluge 1.2.0").Query;
        Assert.DoesNotContain("&event={event}", query, StringComparison.Ordinal);
        Assert.Contains("{left}{event}&key=", query, StringComparison.Ordinal);
    }

    [Fact]
    public void GetByNameFallsBackToDefaultForUnknownNames()
    {
        Assert.Equal(Catalog.Default, Catalog.GetByName("does not exist"));
        Assert.False(Catalog.TryGet("does not exist", out _));
        Assert.True(Catalog.TryGet("Vuze 4.2.0.8", out var vuze));
        Assert.Equal("Vuze 4.2.0.8", vuze.Name);
    }

    [Fact]
    public void UserFileAddsAndOverridesProfilesByName()
    {
        const string userJson = """
        {
          "clients": [
            {
              "name": "uTorrent 3.3.2", "family": "uTorrent", "version": "3.3.2",
              "httpProtocol": "HTTP/1.1", "hashUpperCase": false,
              "key": { "type": "hex", "length": 8 },
              "peerId": { "prefix": "-XX0000-", "type": "random", "length": 10, "urlEncode": true },
              "headers": ["Host: {host}"],
              "query": "info_hash={infohash}", "defaultNumWant": 99
            },
            {
              "name": "MyClient 1.0", "family": "MyClient", "version": "1.0",
              "httpProtocol": "HTTP/1.1", "hashUpperCase": true,
              "key": { "type": "numeric", "length": 4 },
              "peerId": { "prefix": "-MY100-", "type": "alphanumeric", "length": 13 },
              "headers": ["Host: {host}", "User-Agent: MyClient/1.0"],
              "query": "info_hash={infohash}&peer_id={peerid}"
            }
          ]
        }
        """;
        var path = Path.Combine(Path.GetTempPath(), "rm-clients-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, userJson);
        try
        {
            var catalog = ClientProfileCatalog.Load(path);
            Assert.Equal(43, catalog.Profiles.Count);
            Assert.Equal("-XX0000-", catalog.GetByName("uTorrent 3.3.2").PeerIdPrefix);
            Assert.Equal(99, catalog.GetByName("uTorrent 3.3.2").DefaultNumWant);
            Assert.True(catalog.Contains("MyClient 1.0"));
            Assert.Contains("MyClient", catalog.Families);
            // Default still resolves.
            Assert.Equal("qBittorrent 5.2.3", catalog.DefaultName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ParseRejectsDuplicateNames()
    {
        const string json = """
        {
          "clients": [
            { "name": "A", "family": "A", "version": "1", "httpProtocol": "HTTP/1.1", "key": {"type":"hex","length":1}, "peerId": {"prefix":"x","type":"hex","length":1}, "headers": ["Host: {host}"], "query": "q" },
            { "name": "A", "family": "A", "version": "2", "httpProtocol": "HTTP/1.1", "key": {"type":"hex","length":1}, "peerId": {"prefix":"x","type":"hex","length":1}, "headers": ["Host: {host}"], "query": "q" }
          ]
        }
        """;
        Assert.Throws<InvalidOperationException>(() => ClientProfileCatalog.Parse(json));
    }
}
