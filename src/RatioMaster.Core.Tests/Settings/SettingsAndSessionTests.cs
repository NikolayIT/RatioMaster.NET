using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;
using RatioMaster.Core.Settings;

namespace RatioMaster.Core.Tests.Settings;

public class SettingsAndSessionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "rm-tests-" + Guid.NewGuid().ToString("N"));

    private string PathFor(string name) => Path.Combine(_directory, name);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void TrayOptionsDefaultOffOnLinuxOnly()
    {
        // Linux desktops often have no tray host, and a window hidden into a missing tray cannot be recovered.
        var expected = !OperatingSystem.IsLinux();
        var defaults = new AppSettings();

        Assert.Equal(expected, defaults.MinimizeToTray);
        Assert.Equal(expected, defaults.CloseToTray);
        Assert.Equal(expected, AppSettings.TrayIsReliable);

        // A settings file that does not mention them gets the same platform default.
        var loaded = System.Text.Json.JsonSerializer.Deserialize("""{ "theme": "Dark" }""", CoreJsonContext.Default.AppSettings);
        Assert.NotNull(loaded);
        Assert.Equal(expected, loaded.MinimizeToTray);
        Assert.Equal(expected, loaded.CloseToTray);
    }

    [Fact]
    public void SettingsRoundTrip()
    {
        var store = new SettingsStore(PathFor("settings.json"));
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            Use24HourTime = true,
            MinimizeToTray = false,
            SkippedVersion = "1100",
            DetailsPaneHeight = 321,
            Columns = [new ColumnLayout { Name = "Name", Width = 200, DisplayIndex = 1, IsVisible = true }],
            DefaultTorrentSettings = new TorrentSettings
            {
                ClientName = "Deluge 1.2.0",
                UploadRateBytes = 123 * 1024,
                Stop = new StopCondition { Type = StopConditionType.UploadedAboveMb, Value = 500 },
                Proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "p.example", Port = 1080, Username = "u" },
            },
        };

        store.Save(settings);
        var loaded = store.Load();

        // Records compare list members by reference, so compare the collection separately.
        Assert.Equal(settings with { Columns = [] }, loaded with { Columns = [] });
        Assert.Equal(settings.Columns, loaded.Columns);
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal("Deluge 1.2.0", loaded.DefaultTorrentSettings.ClientName);
        Assert.Equal(ProxyType.Socks5, loaded.DefaultTorrentSettings.Proxy.Type);
        Assert.Equal(StopConditionType.UploadedAboveMb, loaded.DefaultTorrentSettings.Stop.Type);
    }

    [Fact]
    public void SettingsFallBackToDefaultsWhenMissingOrCorrupt()
    {
        var missing = new SettingsStore(PathFor("nope.json"));
        Assert.Equal(new AppSettings(), missing.Load());

        Directory.CreateDirectory(_directory);
        var corruptPath = PathFor("corrupt.json");
        File.WriteAllText(corruptPath, "{ this is not json");
        Assert.Equal(new AppSettings(), new SettingsStore(corruptPath).Load());
    }

    [Fact]
    public void SettingsAreWrittenAtomicallyWithoutLeavingTemporaryFiles()
    {
        var path = PathFor("settings.json");
        var store = new SettingsStore(path);
        store.Save(new AppSettings());
        store.Save(new AppSettings { Use24HourTime = true });

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(path + ".tmp"));
        Assert.True(store.Load().Use24HourTime);
    }

    [Fact]
    public void SessionJsonRoundTrip()
    {
        var path = PathFor("my.session");
        var document = new SessionDocument
        {
            Torrents =
            [
                new SessionEntry
                {
                    Name = "Ubuntu",
                    TorrentPath = @"C:\torrents\ubuntu.torrent",
                    TrackerUrl = "http://tracker.test/announce",
                    Settings = new TorrentSettings { ClientName = "Vuze 4.2.0.8", FinishedPercent = 42.5 },
                },
            ],
        };

        SessionFile.Save(path, document);
        var loaded = SessionFile.Load(path);

        Assert.Equal(SessionDocument.CurrentVersion, loaded.Version);
        var entry = Assert.Single(loaded.Torrents);
        Assert.Equal("Ubuntu", entry.Name);
        Assert.Equal("Vuze 4.2.0.8", entry.Settings.ClientName);
        Assert.Equal(42.5, entry.Settings.FinishedPercent);
        Assert.Equal("http://tracker.test/announce", entry.TrackerUrl);
    }

    [Fact]
    public void ReadsTheLegacyXmlSessionFormat()
    {
        const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <main>
          <RatioMaster>
            <Name>RM 1</Name>
            <Address>C:\torrents\old.torrent</Address>
            <Tracker>http://old.tracker/announce.php?passkey=abc</Tracker>
            <UploadSpeed>120</UploadSpeed>
            <UploadRandom>True</UploadRandom>
            <UploadRandMin>2</UploadRandMin>
            <UploadRandMax>8</UploadRandMax>
            <DownloadSpeed>40</DownloadSpeed>
            <DownloadRandom>False</DownloadRandom>
            <DownloadRandMin>1</DownloadRandMin>
            <DownloadRandMax>5</DownloadRandMax>
            <Client>BitComet</Client>
            <Version>1.20</Version>
            <Finished>37,5</Finished>
            <StopType>When uploaded &gt;</StopType>
            <StopValue>2048</StopValue>
            <Port>45123</Port>
            <UseTCP>False</UseTCP>
            <UseScrape>True</UseScrape>
            <ProxyType>SOCKS5</ProxyType>
            <ProxyUser>bob</ProxyUser>
            <ProxyPass>hunter2</ProxyPass>
            <ProxyHost>proxy.local</ProxyHost>
            <ProxyPort>1080</ProxyPort>
            <NextUpdateUpload>True</NextUpdateUpload>
            <NextUpdateUploadFrom>15</NextUpdateUploadFrom>
            <NextUpdateUploadTo>60</NextUpdateUploadTo>
            <NextUpdateDownload>False</NextUpdateDownload>
            <NextUpdateDownloadFrom>10</NextUpdateDownloadFrom>
            <NextUpdateDownloadTo>100</NextUpdateDownloadTo>
            <IgnoreFailureReason>True</IgnoreFailureReason>
          </RatioMaster>
        </main>
        """;

        var document = SessionFile.Parse(xml);

        var entry = Assert.Single(document.Torrents);
        Assert.Equal("RM 1", entry.Name);
        Assert.Equal(@"C:\torrents\old.torrent", entry.TorrentPath);
        Assert.Equal("http://old.tracker/announce.php?passkey=abc", entry.TrackerUrl);

        var s = entry.Settings;
        Assert.Equal("BitComet 1.20", s.ClientName);
        Assert.Equal(120 * 1024, s.UploadRateBytes);
        Assert.Equal(40 * 1024, s.DownloadRateBytes);
        Assert.True(s.UploadRandomEnabled);
        Assert.Equal(2, s.UploadRandomMinKb);
        Assert.Equal(8, s.UploadRandomMaxKb);
        Assert.False(s.DownloadRandomEnabled);
        Assert.Equal(37.5, s.FinishedPercent); // comma decimal separator from the old app
        Assert.Equal(StopConditionType.UploadedAboveMb, s.Stop.Type);
        Assert.Equal(2048, s.Stop.Value);
        Assert.Equal(IdentityMode.Custom, s.IdentityMode);
        Assert.Equal("45123", s.CustomPort);
        Assert.False(s.UseTcpListener);
        Assert.True(s.RequestScrape);
        Assert.True(s.IgnoreFailureReason);
        Assert.True(s.NextUpdateRandomUpload);
        Assert.Equal(15, s.NextUpdateUploadMinKb);
        Assert.Equal(60, s.NextUpdateUploadMaxKb);
        Assert.Equal(ProxyType.Socks5, s.Proxy.Type);
        Assert.Equal("proxy.local", s.Proxy.Host);
        Assert.Equal(1080, s.Proxy.Port);
        Assert.Equal("bob", s.Proxy.Username);
        Assert.Equal("hunter2", s.Proxy.Password);
    }

    [Fact]
    public void LegacyXmlWithMissingElementsFallsBackToDefaults()
    {
        const string xml = "<main><RatioMaster><Name>Minimal</Name></RatioMaster></main>";

        var entry = Assert.Single(SessionFile.Parse(xml).Torrents);

        Assert.Equal("Minimal", entry.Name);
        Assert.Null(entry.TorrentPath);
        Assert.Equal(TorrentSettings.DefaultClientName, entry.Settings.ClientName);
        Assert.Equal(StopConditionType.Never, entry.Settings.Stop.Type);
        Assert.Equal(ProxyType.None, entry.Settings.Proxy.Type);
        Assert.Equal(IdentityMode.Automatic, entry.Settings.IdentityMode);
    }

    [Theory]
    [InlineData("Never", StopConditionType.Never)]
    [InlineData("After time:", StopConditionType.AfterSeconds)]
    [InlineData("When seeders <", StopConditionType.SeedersBelow)]
    [InlineData("When leechers <", StopConditionType.LeechersBelow)]
    [InlineData("When downloaded >", StopConditionType.DownloadedAboveMb)]
    [InlineData("When leechers/seeders <", StopConditionType.LeecherSeederRatioBelow)]
    public void MapsEveryLegacyStopCondition(string legacy, StopConditionType expected)
    {
        var xml = $"<main><RatioMaster><StopType>{System.Security.SecurityElement.Escape(legacy)}</StopType><StopValue>7</StopValue></RatioMaster></main>";
        var entry = Assert.Single(SessionFile.Parse(xml).Torrents);
        Assert.Equal(expected, entry.Settings.Stop.Type);
    }
}
