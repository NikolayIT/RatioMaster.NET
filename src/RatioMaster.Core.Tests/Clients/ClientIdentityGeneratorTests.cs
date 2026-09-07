namespace RatioMaster.Core.Tests.Clients
{
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Tests.Fakes;

    public class ClientIdentityGeneratorTests
    {
        [Fact]
        public void AlphanumericPicksFromTheExpectedAlphabetByNextDouble()
        {
            // NextDouble 0.0 -> index 0 ('a'); ~0.5 -> index 31 ('F'); just under 1 -> index 61 ('9').
            var random = new ScriptedRandomSource(doubles: [0.0, 0.5, 0.99]);
            var generator = new ClientIdentityGenerator(random);

            var value = generator.GenerateValue(new RandomValueSpec { Type = RandomValueKind.Alphanumeric, Length = 3 });

            Assert.Equal("aF9", value);
        }

        [Fact]
        public void NumericAndHexUseTheirAlphabets()
        {
            var numeric = new ClientIdentityGenerator(new ScriptedRandomSource(doubles: [0.0, 0.95]))
                .GenerateValue(new RandomValueSpec { Type = RandomValueKind.Numeric, Length = 2 });
            Assert.Equal("09", numeric);

            var hex = new ClientIdentityGenerator(new ScriptedRandomSource(doubles: [0.0, 0.999]))
                .GenerateValue(new RandomValueSpec { Type = RandomValueKind.Hex, Length = 2 });
            Assert.Equal("0F", hex);
        }

        [Fact]
        public void UrlSafeKindUsesLibtorrentsAlphabetAndNeedsNoEncoding()
        {
            // libtorrent url_random: 0-9 A-Z a-z and -_.!~*(), chosen so the peer id is never escaped.
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_.!~*()";
            var generator = new ClientIdentityGenerator(new DeterministicRandomSource(4));

            var value = generator.GenerateValue(new RandomValueSpec { Type = RandomValueKind.UrlSafe, Length = 12 });

            Assert.Equal(12, value.Length);
            Assert.All(value, c => Assert.Contains(c, alphabet));

            // Never percent-encoded, so what is generated is what reaches the tracker.
            Assert.Equal(value, PercentEncoding.Encode(value).Replace("%2d", "-", StringComparison.Ordinal)
                .Replace("%5f", "_", StringComparison.Ordinal).Replace("%2e", ".", StringComparison.Ordinal)
                .Replace("%21", "!", StringComparison.Ordinal).Replace("%7e", "~", StringComparison.Ordinal)
                .Replace("%2a", "*", StringComparison.Ordinal).Replace("%28", "(", StringComparison.Ordinal)
                .Replace("%29", ")", StringComparison.Ordinal));
        }

        [Fact]
        public void QBittorrentIdentityLooksLikeTheRealClient()
        {
            var profile = ClientProfileCatalog.Load().GetByName("qBittorrent 5.2.3");
            var identity = new ClientIdentityGenerator(new DeterministicRandomSource(11)).Generate(profile);

            // 8-character fingerprint plus 12 random characters is a 20-byte peer id, as libtorrent builds it.
            Assert.Equal(20, identity.PeerId.Length);
            Assert.StartsWith("-qB5230-", identity.PeerId, StringComparison.Ordinal);
            Assert.DoesNotContain('%', identity.PeerId);
            Assert.Equal(8, identity.Key.Length);
        }

        /// <summary>
        /// tr_peerIdInit makes the base-36 values of the twelve suffix characters add up to a multiple of 36.
        /// A tracker can check that, so every peer id we generate has to satisfy it, not just most of them.
        /// </summary>
        [Fact]
        public void TransmissionSuffixAlwaysCarriesAValidCheckDigit()
        {
            const string pool = "0123456789abcdefghijklmnopqrstuvwxyz";
            var generator = new ClientIdentityGenerator(new DeterministicRandomSource(2024));
            var spec = new RandomValueSpec { Type = RandomValueKind.TransmissionChecksum, Length = 12 };

            for (var i = 0; i < 2000; i++)
            {
                var suffix = generator.GenerateValue(spec);

                Assert.Equal(12, suffix.Length);
                Assert.All(suffix, c => Assert.Contains(c, pool));
                Assert.Equal(0, suffix.Sum(c => pool.IndexOf(c, StringComparison.Ordinal)) % 36);
            }
        }

        [Fact]
        public void TransmissionProfileProducesARealLookingPeerId()
        {
            const string pool = "0123456789abcdefghijklmnopqrstuvwxyz";
            var profile = ClientProfileCatalog.Load().GetByName("Transmission 3.00");
            var identity = new ClientIdentityGenerator(new DeterministicRandomSource(9)).Generate(profile);

            Assert.Equal(20, identity.PeerId.Length);
            Assert.StartsWith("-TR3000-", identity.PeerId, StringComparison.Ordinal);

            // The prefix is not part of the sum; only the twelve generated characters are.
            var suffix = identity.PeerId["-TR3000-".Length..];
            Assert.Equal(0, suffix.Sum(c => pool.IndexOf(c, StringComparison.Ordinal)) % 36);
        }

        /// <summary>Transmission prints its key with "%x": lower case, no padding, so the length varies.</summary>
        [Fact]
        public void HexRangeKeyIsLowerCaseAndNeverZeroPadded()
        {
            var generator = new ClientIdentityGenerator(new DeterministicRandomSource(5));
            var spec = new RandomValueSpec { Type = RandomValueKind.HexRange };

            for (var i = 0; i < 500; i++)
            {
                var key = generator.GenerateValue(spec);

                Assert.InRange(key.Length, 1, 8);
                Assert.Equal(key.ToLowerInvariant(), key);
                Assert.All(key, c => Assert.Contains(c, "0123456789abcdef"));
                if (key.Length > 1)
                {
                    Assert.NotEqual('0', key[0]);
                }
            }
        }

        [Fact]
        public void RandomKindPercentEncodesNonAlphanumericBytes()
        {
            var random = new ScriptedRandomSource(ints: [(int)'A', 0x00, 0xFF, 0x20]);
            var generator = new ClientIdentityGenerator(random);

            var value = generator.GenerateValue(new RandomValueSpec
            {
                Type = RandomValueKind.Random,
                Length = 4,
                UrlEncode = true,
            });

            Assert.Equal("A%00%ff%20", value);
        }

        [Fact]
        public void RandomKindUpperCasesEscapes()
        {
            var random = new ScriptedRandomSource(ints: [0xFF, 0x20]);
            var generator = new ClientIdentityGenerator(random);

            var value = generator.GenerateValue(new RandomValueSpec
            {
                Type = RandomValueKind.Random,
                Length = 2,
                UrlEncode = true,
                UpperCase = true,
            });

            Assert.Equal("%FF%20", value);
        }

        [Fact]
        public void UpperCaseWithoutUrlEncodingUpperCasesTheValue()
        {
            var value = new ClientIdentityGenerator(new ScriptedRandomSource(doubles: [0.0, 0.0]))
                .GenerateValue(new RandomValueSpec { Type = RandomValueKind.Alphanumeric, Length = 2, UpperCase = true });
            Assert.Equal("AA", value);
        }

        [Fact]
        public void GenerateBuildsPeerIdFromPrefixAndKeyAndPortInRange()
        {
            var catalog = ClientProfileCatalog.Load();
            var profile = catalog.GetByName("uTorrent 3.6.0");
            var generator = new ClientIdentityGenerator(new DeterministicRandomSource(1234));

            var identity = generator.Generate(profile);

            Assert.StartsWith("-UT360S-%ec%b6", identity.PeerId, StringComparison.Ordinal);
            Assert.Equal(8, identity.Key.Length);
            Assert.Equal("200", identity.NumWant);
            Assert.Equal(ClientIdentitySource.Generated, identity.Source);
            var port = int.Parse(identity.Port);
            Assert.InRange(port, 1025, 65534);
        }

        [Fact]
        public void GenerationIsReproducibleForTheSameSeed()
        {
            var profile = ClientProfileCatalog.Load().GetByName("Vuze 5.7.5.0");
            var a = new ClientIdentityGenerator(new DeterministicRandomSource(7)).Generate(profile);
            var b = new ClientIdentityGenerator(new DeterministicRandomSource(7)).Generate(profile);
            Assert.Equal(a, b);
        }
    }
}
