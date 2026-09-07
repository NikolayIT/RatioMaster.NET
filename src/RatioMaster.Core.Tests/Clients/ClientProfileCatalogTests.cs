using RatioMaster.Core.Clients;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Tests.Clients;

public class ClientProfileCatalogTests
{
    private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

    [Fact]
    public void LoadsAllProfilesFromTheBuiltInCatalog()
    {
        // 41 inherited from 0.43, minus the ten obsolete uTorrent 1.x-3.2 ones, plus qBittorrent, uTorrent 3.6.0 and 3.5.5.
        Assert.Equal(34, Catalog.Profiles.Count);
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
    [InlineData("uTorrent", new[] { "3.6.0", "3.5.5", "3.3.2", "3.3.0" })]
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
        // uTorrent (4) + BitComet (6) + Azureus (6) + Vuze (1) + ABC (1) = 18; the rest cannot be read from a process.
        Assert.Equal(18, Catalog.Profiles.Count(p => p.CanScanMemory));

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
        // 0.43 sent "uTorrent/3320" with no build and no Connection header; every real uTorrent carries the
        // build (0.43's own 2.0.1 entry, the SB-Innovation 3.2.3 file, 3.4.8+ captures) and 3.x sends Connection: Close.
        Assert.Equal(["Host: {host}", "User-Agent: uTorrent/3320(30488)", "Accept-Encoding: gzip", "Connection: Close"], p.Headers);
        Assert.Equal(200, p.DefaultNumWant);
        Assert.Equal("uTorrent", p.MemoryScan!.ProcessName);
        Assert.Equal("&peer_id=-UT3320-", p.MemoryScan.SearchString);
        Assert.Equal(200_000_000, p.MemoryScan.MaxOffset);
    }

    /// <summary>
    /// uTorrent 3.x puts its build number, as a little-endian 16-bit integer, in the two bytes after the
    /// "-UTxxxx-" prefix; the ten bytes after that are random. The user agent carries the build twice:
    /// once inside the first parenthesis (a per-version constant times 65536, plus the build) and once
    /// plainly. Both facts come from real announces (build 45311 in a public capture; the SB-Innovation
    /// client files for builds 44994-46074 and 46828), so a tracker that decodes them sees a real build.
    /// </summary>
    [Theory]
    [InlineData("uTorrent 3.6.0", "-UT360S-", 46828, 1729)]
    [InlineData("uTorrent 3.5.5", "-UT355S-", 46074, 1707)]
    public void ModernUTorrentEncodesItsBuildLikeTheRealClient(string name, string prefix, int build, int versionConstant)
    {
        var p = Catalog.GetByName(name);
        Assert.Equal(name, p.Name);

        var expectedPrefix = prefix + "%" + (build & 0xFF).ToString("x2") + "%" + (build >> 8).ToString("x2");
        Assert.Equal(expectedPrefix, p.PeerIdPrefix);
        Assert.Equal(RandomValueKind.Random, p.PeerId.Type);
        Assert.Equal(10, p.PeerId.Length);
        Assert.True(p.PeerId.UrlEncode);
        Assert.False(p.PeerId.UpperCase);

        var shortVersion = prefix.Substring(3, 3);
        var userAgent = $"User-Agent: uTorrent/{shortVersion}({versionConstant * 65536 + build})({build})";
        Assert.Equal(["Host: {host}", userAgent, "Accept-Encoding: gzip", "Connection: Close"], p.Headers);

        // Same query as every uTorrent since 2.0, which the captures confirm.
        Assert.Equal(Catalog.GetByName("uTorrent 3.3.2").Query, p.Query);
        Assert.Equal(RandomValueKind.Hex, p.Key.Type);
        Assert.Equal(8, p.Key.Length);
        Assert.True(p.Key.UpperCase);
        Assert.False(p.HashUpperCase);
        Assert.Equal(200, p.DefaultNumWant);
        Assert.Equal("uTorrent", p.MemoryScan!.ProcessName);
        Assert.Equal("&peer_id=" + prefix, p.MemoryScan.SearchString);
    }

    [Theory]
    [InlineData("uTorrent 3.3.2", 30488)]
    [InlineData("uTorrent 3.3.0", 29625)]
    public void OlderUTorrentUserAgentsCarryTheBuildEncodedInThePeerId(string name, int build)
    {
        var p = Catalog.GetByName(name);
        Assert.Equal(build, BuildFromPrefix(p.PeerIdPrefix));

        var digits = p.Version.Replace(".", string.Empty, StringComparison.Ordinal) + "0";
        Assert.Contains($"User-Agent: uTorrent/{digits}({build})", p.Headers);
        Assert.Equal("Connection: Close", p.Headers[^1]);
    }

    /// <summary>Decodes the two bytes after "-UTxxxx-" (percent-escaped or literal ASCII) as a little-endian build number.</summary>
    private static int BuildFromPrefix(string prefix)
    {
        var bytes = new List<byte>();
        for (var i = 8; i < prefix.Length;)
        {
            if (prefix[i] == '%')
            {
                bytes.Add(Convert.ToByte(prefix.Substring(i + 1, 2), 16));
                i += 3;
            }
            else
            {
                bytes.Add((byte)prefix[i]);
                i++;
            }
        }

        return bytes.Count == 2 ? bytes[0] | (bytes[1] << 8) : -1;
    }

    [Fact]
    public void UTorrentPeerIdsAreExactlyTwentyBytes()
    {
        foreach (var name in Catalog.Profiles.Where(p => p.Family == "uTorrent").Select(p => p.Name))
        {
            var p = Catalog.GetByName(name);
            // The prefix is ASCII with %XX escapes, so every escape is one byte on the wire.
            var prefixBytes = System.Text.RegularExpressions.Regex.Replace(p.PeerIdPrefix, "%[0-9a-fA-F]{2}", "?").Length;
            Assert.Equal(20, prefixBytes + p.PeerId.Length);
        }
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

    [Theory]
    [InlineData("Transmission 2.82 (14160)", "-TR2820-", "User-Agent: Transmission/2.82")]
    [InlineData("Transmission 2.92 (14714)", "-TR2920-", "User-Agent: Transmission/2.92")]
    public void TransmissionPeerIdMatchesItsUserAgent(string name, string prefix, string userAgent)
    {
        // 0.43 announced Transmission 2.82 with the 2.50 peer id, a mismatch a tracker can reject (issue #25).
        var p = Catalog.GetByName(name);
        Assert.Equal(prefix, p.PeerIdPrefix);
        Assert.Contains(userAgent, p.Headers);
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
            Assert.Equal(35, catalog.Profiles.Count);
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

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("""{ "clients": [ { "name": "Broken 1.0", "family": "Broken" } ] }""")]
    public void LoadFallsBackToTheBuiltInCatalogWhenTheUserFileIsBroken(string userJson)
    {
        // A broken user file used to throw out of Load and crash the application at startup.
        var path = Path.Combine(Path.GetTempPath(), "rm-clients-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, userJson);
        try
        {
            Assert.ThrowsAny<Exception>(() => ClientProfileCatalog.Load(path));

            var catalog = ClientProfileCatalog.Load(path, out var error);

            Assert.NotNull(error);
            Assert.Equal(Catalog.Profiles.Count, catalog.Profiles.Count);
            Assert.Equal("qBittorrent 5.2.3", catalog.DefaultName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadReportsNoErrorForAMissingOrValidUserFile()
    {
        var missing = ClientProfileCatalog.Load(Path.Combine(Path.GetTempPath(), "rm-missing-" + Guid.NewGuid().ToString("N") + ".json"), out var error);
        Assert.Null(error);
        Assert.Equal(Catalog.Profiles.Count, missing.Profiles.Count);

        var none = ClientProfileCatalog.Load(null, out var noFileError);
        Assert.Null(noFileError);
        Assert.Equal(Catalog.Profiles.Count, none.Profiles.Count);
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
