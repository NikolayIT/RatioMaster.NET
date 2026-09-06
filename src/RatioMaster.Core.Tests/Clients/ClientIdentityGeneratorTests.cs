using RatioMaster.Core.Clients;
using RatioMaster.Core.Tests.Fakes;

namespace RatioMaster.Core.Tests.Clients;

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
        var profile = catalog.GetByName("uTorrent 3.3.2");
        var generator = new ClientIdentityGenerator(new DeterministicRandomSource(1234));

        var identity = generator.Generate(profile);

        Assert.StartsWith("-UT3320-%18w", identity.PeerId, StringComparison.Ordinal);
        Assert.Equal(8, identity.Key.Length);
        Assert.Equal("200", identity.NumWant);
        Assert.Equal(ClientIdentitySource.Generated, identity.Source);
        var port = int.Parse(identity.Port);
        Assert.InRange(port, 1025, 65534);
    }

    [Fact]
    public void GenerationIsReproducibleForTheSameSeed()
    {
        var profile = ClientProfileCatalog.Load().GetByName("Azureus 3.1.1.0");
        var a = new ClientIdentityGenerator(new DeterministicRandomSource(7)).Generate(profile);
        var b = new ClientIdentityGenerator(new DeterministicRandomSource(7)).Generate(profile);
        Assert.Equal(a, b);
    }
}
