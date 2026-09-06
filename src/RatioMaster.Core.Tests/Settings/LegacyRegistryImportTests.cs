using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;
using RatioMaster.Core.Settings;

namespace RatioMaster.Core.Tests.Settings;

public class LegacyRegistryImportTests
{
    private sealed class FakeRegistry : ILegacyRegistryReader
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public bool Exists { get; set; } = true;

        public FakeRegistry Set(string name, string value)
        {
            _values[name] = value;
            return this;
        }

        public FakeRegistry Set(string name, int value)
        {
            _values[name] = value;
            return this;
        }

        public string? GetString(string name) => _values.TryGetValue(name, out var v) ? v.ToString() : null;

        public int? GetInt(string name) => _values.TryGetValue(name, out var v) && v is int i ? i : null;
    }

    private static FakeRegistry TypicalOldInstall() => new FakeRegistry()
        .Set("Version", "0.43")
        .Set("NewValues", 0)
        .Set("BallonTip", 1)
        .Set("MinimizeToTray", 0)
        .Set("CloseToTray", 1)
        .Set("Client", "Azureus")
        .Set("ClientVersion", "3.1.1.0")
        .Set("UploadRate", "150")
        .Set("DownloadRate", "45")
        .Set("Interval", "900")
        .Set("fileSize", "12,5")
        .Set("Directory", @"D:\torrents")
        .Set("TCPlistener", 0)
        .Set("ScrapeInfo", 1)
        .Set("EnableLog", 0)
        .Set("GetRandUp", 1)
        .Set("MinRandUp", "3")
        .Set("MaxRandUp", "9")
        .Set("GetRandDown", 0)
        .Set("MinRandDown", "2")
        .Set("MaxRandDown", "6")
        .Set("CustomKey", "ABCD1234")
        .Set("CustomPeerID", "-AZ3110-oldpeerid")
        .Set("CustomPort", "42000")
        .Set("CustomPeers", "50")
        .Set("StopWhen", "When leechers <")
        .Set("StopAfter", "4")
        .Set("ProxyType", "SOCKS4a")
        .Set("ProxyAdress", "proxy.old")
        .Set("ProxyPort", "9050")
        .Set("ProxyUser", "olduser")
        .Set("ProxyPass", "oldpass")
        .Set("GetRandUpNext", 1)
        .Set("MinRandUpNext", "20")
        .Set("MaxRandUpNext", "70")
        .Set("GetRandDownNext", 0)
        .Set("IgnoreFailureReason", 1);

    [Fact]
    public void ImportsTheOldRegistrySettings()
    {
        var imported = LegacyRegistrySettingsImporter.TryImport(TypicalOldInstall());

        Assert.NotNull(imported);
        Assert.True(imported!.ShowTorrentListInTrayTooltip);
        Assert.False(imported.MinimizeToTray);
        Assert.True(imported.CloseToTray);
        Assert.Equal(@"D:\torrents", imported.LastTorrentDirectory);

        var d = imported.DefaultTorrentSettings;
        Assert.Equal("Azureus 3.1.1.0", d.ClientName);
        Assert.Equal(150 * 1024, d.UploadRateBytes);
        Assert.Equal(45 * 1024, d.DownloadRateBytes);
        Assert.Equal(900, d.IntervalSeconds);
        Assert.Equal(12.5, d.FinishedPercent);
        Assert.False(d.UseTcpListener);
        Assert.True(d.RequestScrape);
        Assert.False(d.EnableLog);
        Assert.True(d.IgnoreFailureReason);
        Assert.True(d.UploadRandomEnabled);
        Assert.Equal(3, d.UploadRandomMinKb);
        Assert.Equal(9, d.UploadRandomMaxKb);
        Assert.False(d.DownloadRandomEnabled);
        Assert.True(d.NextUpdateRandomUpload);
        Assert.Equal(20, d.NextUpdateUploadMinKb);
        Assert.Equal(70, d.NextUpdateUploadMaxKb);
        Assert.False(d.NextUpdateRandomDownload);

        Assert.Equal(IdentityMode.Custom, d.IdentityMode);
        Assert.Equal("ABCD1234", d.CustomKey);
        Assert.Equal("-AZ3110-oldpeerid", d.CustomPeerId);
        Assert.Equal("42000", d.CustomPort);
        Assert.Equal("50", d.CustomNumWant);

        Assert.Equal(StopConditionType.LeechersBelow, d.Stop.Type);
        Assert.Equal(4, d.Stop.Value);

        Assert.Equal(ProxyType.Socks4a, d.Proxy.Type);
        Assert.Equal("proxy.old", d.Proxy.Host);
        Assert.Equal(9050, d.Proxy.Port);
        Assert.Equal("olduser", d.Proxy.Username);
        Assert.Equal("oldpass", d.Proxy.Password);
    }

    [Fact]
    public void AlwaysGetNewValuesMapsToAutomaticIdentity()
    {
        var registry = TypicalOldInstall().Set("NewValues", 1);
        var imported = LegacyRegistrySettingsImporter.TryImport(registry);
        Assert.Equal(IdentityMode.Automatic, imported!.DefaultTorrentSettings.IdentityMode);
    }

    [Fact]
    public void ReturnsNullWhenThereIsNothingToImport()
    {
        Assert.Null(LegacyRegistrySettingsImporter.TryImport(new FakeRegistry { Exists = false }));
        Assert.Null(LegacyRegistrySettingsImporter.TryImport(new FakeRegistry())); // key exists but never saved
    }

    [Fact]
    public void KeepsBaselineValuesForKeysTheOldAppNeverWrote()
    {
        var baseline = new AppSettings { Theme = AppTheme.Dark, CheckForUpdatesOnStartup = false };
        var imported = LegacyRegistrySettingsImporter.TryImport(TypicalOldInstall(), baseline);

        Assert.Equal(AppTheme.Dark, imported!.Theme);
        Assert.False(imported.CheckForUpdatesOnStartup);
    }
}
