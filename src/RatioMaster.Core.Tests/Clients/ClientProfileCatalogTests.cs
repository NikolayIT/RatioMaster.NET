namespace RatioMaster.Core.Tests.Clients
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Sessions;

    public class ClientProfileCatalogTests
    {
        private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

        [Fact]
        public void LoadsAllProfilesFromTheBuiltInCatalog()
        {
            // 41 inherited from 0.43, minus the obsolete emulations dropped since (ten uTorrent 1.x-3.2, two
            // Transmission, three Deluge), plus qBittorrent 5.2.3, 5.1.4 and 4.6.7, uTorrent 3.6.0, Transmission 2.94
            // and 3.00, Deluge 2.1.1; then qBittorrent 5.2.4, Deluge 2.2.0, BitTornado 0.3.18, rTorrent 0.16.25,
            // Vuze 5.7.6.0, BitComet 2.22 and BitTorrent 7.11.0 for 1.2, which dropped uTorrent 3.5.5 and 3.5.4 and
            // BitTorrent 7.10.3 because their code could not be checked.
            Assert.Equal(32, Catalog.Profiles.Count);
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
            Assert.Equal("qBittorrent 5.2.4", Catalog.DefaultName);
            Assert.Equal("qBittorrent 5.2.4", Catalog.Default.Name);
            Assert.Equal(TorrentSettings.DefaultClientName, Catalog.DefaultName);
        }

        [Fact]
        public void FamiliesAreListedNewestFirst()
        {
            string[] expected =
            [
                "qBittorrent", "uTorrent", "BitComet", "Vuze", "BitTorrent", "rTorrent", "Transmission", "ABC",
                "BitLord", "BTuga", "BitTornado", "Burst", "BitTyrant", "BitSpirit", "Deluge", "KTorrent", "Gnome BT",
            ];
            Assert.Equal(expected, Catalog.Families);
        }

        [Theory]
        [InlineData("qBittorrent", new[] { "5.2.4", "5.2.3", "5.1.4", "4.6.7" })]
        [InlineData("uTorrent", new[] { "3.6.0" })]
        [InlineData("BitComet", new[] { "2.22", "1.20", "1.03", "0.98", "0.96", "0.93", "0.92" })]
        [InlineData("Vuze", new[] { "5.7.6.0", "5.7.5.0" })]
        [InlineData("BitTorrent", new[] { "7.11.0 (47255)" })]
        [InlineData("rTorrent", new[] { "0.16.25", "0.9.6" })]
        [InlineData("Transmission", new[] { "2.94", "3.00" })]
        [InlineData("BitSpirit", new[] { "3.6.0.200", "3.1.0.077" })]
        [InlineData("Deluge", new[] { "2.2.0", "2.1.1" })]
        public void VersionsPerFamilyMatchTheOldUi(string family, string[] expected)
        {
            Assert.Equal(expected, Catalog.VersionsOf(family));
        }

        [Fact]
        public void OnlyParseableClientsHaveAMemoryScanSpec()
        {
            // uTorrent (1) + BitComet (6) + Vuze (2) + ABC (1) = 10; the rest cannot be read from a process.
            Assert.Equal(10, Catalog.Profiles.Count(p => p.CanScanMemory));

            Assert.True(Catalog.GetByName("uTorrent 3.6.0").CanScanMemory);
            Assert.Equal("uTorrent", Catalog.GetByName("uTorrent 3.6.0").MemoryScan!.ProcessName);
            Assert.Equal("&peer_id=-UT360S-", Catalog.GetByName("uTorrent 3.6.0").MemoryScan!.SearchString);
            Assert.False(Catalog.GetByName("Deluge 2.1.1").CanScanMemory);
            Assert.False(Catalog.GetByName("BitTorrent 7.11.0 (47255)").CanScanMemory);
            Assert.False(Catalog.GetByName("Transmission 3.00").CanScanMemory);
        }

        /// <summary>
        /// uTorrent 3.6.0 and BitTorrent 7.11.0 as read from the code of builds 47254 and 47255, one program under
        /// two names. The peer id is the client code and version, the build as a little-endian 16-bit integer, eight
        /// random bytes and two SHA-1 check bytes. The user agent carries the build twice: inside the first
        /// parenthesis (a version constant times 65536, plus the build) and plainly. With no port set the client
        /// listens on 10000 + (GetTickCount() * 0xDEADBEEF) % 55000, and Host carries the tracker's port unless it is
        /// the scheme's default.
        /// </summary>
        [Theory]
        [InlineData("uTorrent 3.6.0", "-UT360S-", 47254, "uTorrent/360(113358998)(47254)")]
        [InlineData("BitTorrent 7.11.0 (47255)", "-BT7b0S-", 47255, "BitTorrent/7110(258062487)(47255)")]
        public void UTorrentAndBitTorrentMatchTheirCode(string name, string prefix, int build, string userAgent)
        {
            var p = Catalog.GetByName(name);
            Assert.Equal(name, p.Name);

            var expectedPrefix = prefix + "%" + (build & 0xFF).ToString("x2") + "%" + (build >> 8).ToString("x2");
            Assert.Equal(expectedPrefix, p.PeerIdPrefix);

            // Read back the other way round too, so the two bytes really do decode to the build.
            Assert.Equal(build, BuildFromPrefix(p.PeerIdPrefix));
            Assert.Equal(RandomValueKind.UTorrentChecksum, p.PeerId.Type);
            Assert.Equal(10, p.PeerId.Length);
            Assert.True(p.PeerId.UrlEncode);
            Assert.False(p.PeerId.UpperCase);

            Assert.Equal(((VersionConstant(p.Version) * 65536) + build).ToString(System.Globalization.CultureInfo.InvariantCulture), userAgent.Split('(', ')')[1]);
            Assert.Equal(["Host: {host}", "User-Agent: " + userAgent, "Accept-Encoding: gzip", "Connection: Close"], p.Headers);
            Assert.Equal(
                "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}" +
                "&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1",
                p.Query);
            Assert.Equal(RandomValueKind.Hex, p.Key.Type);
            Assert.Equal(8, p.Key.Length);
            Assert.True(p.Key.UpperCase);
            Assert.False(p.HashUpperCase);
            Assert.Equal("-._~", p.Unreserved);
            Assert.Equal(200, p.DefaultNumWant);
            Assert.Equal([new PortRange(10000, 64999)], p.PortRanges);
            Assert.True(p.HostPort);
        }

        [Fact]
        public void UTorrentPeerIdsAreExactlyTwentyBytes()
        {
            foreach (var name in Catalog.Profiles.Where(p => p.Family is "uTorrent" or "BitTorrent").Select(p => p.Name))
            {
                var p = Catalog.GetByName(name);

                // The prefix is ASCII with %XX escapes, so every escape is one byte on the wire.
                var prefixBytes = System.Text.RegularExpressions.Regex.Replace(p.PeerIdPrefix, "%[0-9a-fA-F]{2}", "?").Length;
                Assert.Equal(20, prefixBytes + p.PeerId.Length);
            }
        }

        [Theory]
        [InlineData("5.2.4", "-qB5240-")]
        [InlineData("5.2.3", "-qB5230-")]
        [InlineData("5.1.4", "-qB5140-")]
        [InlineData("4.6.7", "-qB4670-")]
        public void QBittorrentMatchesLibtorrentsAnnounce(string version, string prefix)
        {
            // Derived from libtorrent RC_2_0: http_tracker_connection.cpp builds the query,
            // http_connection.cpp the headers, generate_fingerprint the peer id prefix.
            var p = Catalog.GetByName("qBittorrent " + version);

            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Equal(RandomValueKind.UrlSafe, p.PeerId.Type);
            Assert.Equal(12, p.PeerId.Length);
            Assert.False(p.PeerId.UrlEncode);
            Assert.False(p.HashUpperCase);
            Assert.Equal(RandomValueKind.Hex, p.Key.Type);
            Assert.Equal(8, p.Key.Length);
            Assert.True(p.Key.UpperCase);
            Assert.Equal(
                ["Host: {host}", "User-Agent: qBittorrent/" + version, "Accept-Encoding: gzip", "Connection: close"],
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

        [Theory]
        [InlineData("5.7.6.0", "-AZ5760-")]
        [InlineData("5.7.5.0", "-AZ5750-")]
        public void VuzeMatchesItsSource(string version, string prefix)
        {
            // Constants.java gives the version and prefix (the only lines 5.7.6.0 changed), TRTrackerBTAnnouncerImpl the
            // query and headers, Java's HttpURLConnection appends Host and Accept. calculateNumWant asks for 3/2 of the
            // free connection slots, at most 100: a default install has 50 per torrent, 25 once seeding, so 75 and 37.
            // RandomUtils.generateRandomNetworkListenPort picks 10000-65535, skipping the magnet ports 45100-45110.
            var p = Catalog.GetByName("Vuze " + version);

            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Equal(RandomValueKind.Alphanumeric, p.PeerId.Type);
            Assert.Equal(12, p.PeerId.Length);
            Assert.Equal(RandomValueKind.Alphanumeric, p.Key.Type);
            Assert.Equal(8, p.Key.Length);
            Assert.True(p.HashUpperCase);
            Assert.Equal(".-*_", p.Unreserved);
            Assert.Equal("User-Agent: Azureus " + version + ";Windows 10;Java 1.8.0_121", p.Headers[0]);
            Assert.Equal(Catalog.GetByName("Vuze 5.7.5.0").Query, p.Query);
            Assert.StartsWith("info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}", p.Query, StringComparison.Ordinal);
            Assert.Equal(75, p.DefaultNumWant);
            Assert.Equal(37, p.SeedingNumWant);
            Assert.Equal([new PortRange(10000, 45099), new PortRange(45111, 65535)], p.PortRanges);
        }

        [Fact]
        public void BitComet222MatchesWhatTheRealClientSends()
        {
            // From live announces of the portable 2.22 against a local tracker, and from its code: the announce
            // builder and the function that fills its parameters, the key (two random bytes read as a ushort), the peer
            // id ("-BC0222-" and twelve random bytes) and the first-run port (7000 + a random ushort % 20767).
            var p = Catalog.GetByName("BitComet 2.22");

            Assert.Equal("-BC0222-", p.PeerIdPrefix);
            Assert.Equal(RandomValueKind.AnyByte, p.PeerId.Type);
            Assert.Equal(12, p.PeerId.Length);
            Assert.True(p.PeerId.UrlEncode);
            Assert.True(p.PeerId.UpperCase);
            Assert.True(p.HashUpperCase);
            Assert.Equal("-._~", p.Unreserved);
            Assert.Equal(RandomValueKind.UInt16Decimal, p.Key.Type);
            Assert.Equal(
                ["Host: {host}", "Connection: close", "Accept: */*", "Accept-Encoding: gzip, br, zstd", "User-Agent: BitComet/2.22.9.4"],
                p.Headers);
            Assert.False(p.HostPort);
            Assert.True(p.KeepNumWantOnStop);
            Assert.True(p.LocalIpOnStart);
            Assert.Equal(200, p.DefaultNumWant);
            Assert.Equal([new PortRange(7000, 27766)], p.PortRanges);
            Assert.Equal(
                "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}" +
                "&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}",
                p.Query);
        }

        [Fact]
        public void VuzeScansTheLowercaseAzureusProcess()
        {
            Assert.Equal("azureus", Catalog.GetByName("Vuze 5.7.5.0").MemoryScan!.ProcessName);
        }

        [Theory]
        [InlineData("Transmission 2.94", "-TR2940-", "User-Agent: Transmission/2.94")]
        [InlineData("Transmission 3.00", "-TR3000-", "User-Agent: Transmission/3.00")]
        public void TransmissionPeerIdMatchesItsUserAgent(string name, string prefix, string userAgent)
        {
            // 0.43 announced Transmission 2.82 with the 2.50 peer id, a mismatch a tracker can reject (issue #25),
            // so the prefix and the user agent have to keep naming the same release.
            var p = Catalog.GetByName(name);
            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Contains(userAgent, p.Headers);

            // The suffix carries a check digit, so it cannot be drawn character by character like the others.
            Assert.Equal(RandomValueKind.TransmissionChecksum, p.PeerId.Type);
            Assert.Equal(12, p.PeerId.Length);
            Assert.Equal(RandomValueKind.HexRange, p.Key.Type);
        }

        [Fact]
        public void TransmissionKeepsEachReleasesOwnAcceptEncoding()
        {
            // 3.00 dropped the q-values 2.94 sent; both are what the real client puts on the wire.
            Assert.Contains("Accept-Encoding: gzip;q=1.0, deflate, identity", Catalog.GetByName("Transmission 2.94").Headers);
            Assert.Contains("Accept-Encoding: deflate, gzip", Catalog.GetByName("Transmission 3.00").Headers);
        }

        [Fact]
        public void DelugeQueryDoesNotDuplicateTheEventParameter()
        {
            // 0.43 used "&event={event}", which produced "&event=&event=started"; real Deluge sends it once.
            var query = Catalog.GetByName("Deluge 2.1.1").Query;
            Assert.DoesNotContain("&event={event}", query, StringComparison.Ordinal);
            Assert.Contains("&key={key}{event}&numwant=", query, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(
            "0.16.25",
            "-lt1019-",
            "rtorrent/0.16.25",
            "deflate, gzip, br, zstd",
            "info_hash={infohash}&peer_id={peerid}&compact=1&key={key}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}")]
        [InlineData(
            "0.9.6",
            "-lt0D60-",
            "rtorrent/0.9.6/0.13.6",
            "deflate, gzip",
            "info_hash={infohash}&peer_id={peerid}&key={key}&compact=1&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}")]
        public void RTorrentMatchesItsSource(string version, string prefix, string userAgent, string acceptEncoding, string query)
        {
            // libtorrent's PEER_NAME plus twelve whole random bytes, escaped with upper-case hex like everything
            // but letters, digits and '-' (copy_escape_html); the key is random() as eight lower-case hex digits;
            // tracker_http.cc writes the query, and sends no numwant while trackers.numwant stays at -1. 0.16 moved
            // compact=1 before the key and dropped the libtorrent version from the user agent; the headers are
            // libcurl's, with the Accept-Encoding of the curl of its time (0.16: brotli and zstd, as distributions
            // build it now).
            var p = Catalog.GetByName("rTorrent " + version);

            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Equal(RandomValueKind.AnyByte, p.PeerId.Type);
            Assert.Equal(12, p.PeerId.Length);
            Assert.True(p.PeerId.UrlEncode);
            Assert.True(p.PeerId.UpperCase);
            Assert.True(p.HashUpperCase);
            Assert.Equal(RandomValueKind.HexRange, p.Key.Type);
            Assert.Equal(8, p.Key.Length);
            Assert.False(p.Key.UpperCase);
            Assert.Equal(query, p.Query);
            Assert.Equal(0, p.DefaultNumWant);
            Assert.Equal(["Host: {host}", "User-Agent: " + userAgent, "Accept: */*", "Accept-Encoding: " + acceptEncoding], p.Headers);
        }

        [Theory]
        [InlineData("0.3.18", "T03I-----", "&peer_id={peerid}&supportcrypto=1&port={port}&uploaded=")]
        [InlineData("0.3.17", "T03H-----", "&peer_id={peerid}&port={port}&uploaded=")]
        public void BitTornadoMatchesItsSource(string version, string prefix, string start)
        {
            // BitTornado/__init__.py builds the prefix from version_short ('T-0.3.18' gives T, 0, 3 and
            // mapbase64[18] = I) padded with dashes, then 11 mapbase64 characters; BT1/Rerequester.py builds the
            // query, adding supportcrypto=1 from 0.3.18 on, and appends the 6-character key last; zurllib.py sends
            // the user agent and Accept-Encoding through httplib, which writes Host first.
            var p = Catalog.GetByName("BitTornado " + version);

            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Equal(RandomValueKind.BitTornadoBase64, p.PeerId.Type);
            Assert.Equal(20, prefix.Length + p.PeerId.Length);
            Assert.False(p.PeerId.UrlEncode);
            Assert.Equal(RandomValueKind.BitTornadoBase64, p.Key.Type);
            Assert.Equal(6, p.Key.Length);
            Assert.True(p.HashUpperCase);
            Assert.StartsWith("info_hash={infohash}" + start, p.Query, StringComparison.Ordinal);
            Assert.EndsWith("&left={left}&no_peer_id=1&compact=1{event}&key={key}", p.Query, StringComparison.Ordinal);
            Assert.Equal(["Host: {host}", "Accept-Encoding: gzip", "User-Agent: BitTornado/T-" + version], p.Headers);
        }

        [Theory]
        [InlineData("Deluge 2.2.0", "-DE220s-", "Deluge/2.2.0 libtorrent/1.2.19.0")]
        [InlineData("Deluge 2.1.1", "-DE211s-", "Deluge/2.1.1 libtorrent/1.2.15.0")]
        public void DelugeAnnouncesThroughLibtorrent(string name, string prefix, string userAgent)
        {
            // deluge/core/core.py: libtorrent's fingerprint with "s" (stable) as its seventh character, and a user
            // agent naming the libtorrent it runs on, taken from real announces. The rest is libtorrent's request.
            var p = Catalog.GetByName(name);
            var qBittorrent = Catalog.GetByName("qBittorrent 5.2.4");

            Assert.Equal(prefix, p.PeerIdPrefix);
            Assert.Equal(["Host: {host}", "User-Agent: " + userAgent, "Accept-Encoding: gzip", "Connection: close"], p.Headers);
            Assert.Equal(qBittorrent.Query, p.Query);
            Assert.Equal(qBittorrent.PeerId, p.PeerId);
            Assert.Equal(qBittorrent.Key, p.Key);
            Assert.Equal(qBittorrent.HashUpperCase, p.HashUpperCase);
        }

        [Fact]
        public void GetByNameFallsBackToDefaultForUnknownNames()
        {
            Assert.Equal(Catalog.Default, Catalog.GetByName("does not exist"));
            Assert.False(Catalog.TryGet("does not exist", out _));
            Assert.True(Catalog.TryGet("Vuze 5.7.5.0", out var vuze));
            Assert.Equal("Vuze 5.7.5.0", vuze.Name);
        }

        [Fact]
        public void UserFileAddsAndOverridesProfilesByName()
        {
            const string userJson = """
        {
          "clients": [
            {
              "name": "uTorrent 3.6.0", "family": "uTorrent", "version": "3.3.2",
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
                Assert.Equal(33, catalog.Profiles.Count);
                Assert.Equal("-XX0000-", catalog.GetByName("uTorrent 3.6.0").PeerIdPrefix);
                Assert.Equal(99, catalog.GetByName("uTorrent 3.6.0").DefaultNumWant);
                Assert.True(catalog.Contains("MyClient 1.0"));
                Assert.Contains("MyClient", catalog.Families);

                // Default still resolves.
                Assert.Equal("qBittorrent 5.2.4", catalog.DefaultName);
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
                Assert.Equal("qBittorrent 5.2.4", catalog.DefaultName);
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
        public void UnreservedCharactersThatWouldBreakTheQueryAreIgnored()
        {
            const string json = """
        {
          "clients": [
            { "name": "A", "family": "A", "version": "1", "httpProtocol": "HTTP/1.1", "unreserved": "-&=%#~+",
              "key": {"type":"hex","length":1}, "peerId": {"prefix":"x","type":"hex","length":1}, "headers": ["Host: {host}"], "query": "q" }
          ]
        }
        """;
            Assert.Equal("-~", ClientProfileCatalog.Parse(json).GetByName("A").Unreserved);
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

        /// <summary>
        /// The constant uTorrent and BitTorrent multiply by 65536 in their user agent: major, minor and patch as
        /// nibbles, doubled, plus one for a stable build (0x4de48b in uTorrent 3.6.0.47254).
        /// </summary>
        private static int VersionConstant(string version)
        {
            var parts = version.Split(' ')[0].Split('.').Select(part => int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return (((((parts[0] << 4) | parts[1]) << 4) | parts[2]) * 2) | 1;
        }
    }
}
