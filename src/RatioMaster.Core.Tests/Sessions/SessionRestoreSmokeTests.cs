namespace RatioMaster.Core.Tests.Sessions
{
    using RatioMaster.Core.Bencode;
    using RatioMaster.Core.Sessions;
    using RatioMaster.Core.Torrents;

    /// <summary>
    /// Guards the shape the application actually writes and reads back: a v2 session whose entries carry only
    /// the settings the user changed, everything else falling back to defaults.
    /// </summary>
    public class SessionRestoreSmokeTests
    {
        [Fact]
        public void ReadsASessionWhoseSettingsOnlySpecifySomeProperties()
        {
            const string json = """
        {
          "version": 2,
          "torrents": [
            {
              "name": "ubuntu-24",
              "torrentPath": "C:\\temp\\ubuntu-24.torrent",
              "trackerUrl": "http://tracker.example.org:6969/announce",
              "settings": {
                "clientName": "uTorrent 3.6.0",
                "uploadRateBytes": 61440,
                "downloadRateBytes": 30720
              }
            }
          ]
        }
        """;

            var document = SessionFile.Parse(json);

            var entry = Assert.Single(document.Torrents);
            Assert.Equal("ubuntu-24", entry.Name);
            Assert.Equal(@"C:\temp\ubuntu-24.torrent", entry.TorrentPath);
            Assert.Equal("http://tracker.example.org:6969/announce", entry.TrackerUrl);
            Assert.Equal("uTorrent 3.6.0", entry.Settings.ClientName);
            Assert.Equal(61440, entry.Settings.UploadRateBytes);

            // Unspecified values fall back to the defaults.
            Assert.Equal(1800, entry.Settings.IntervalSeconds);
            Assert.True(entry.Settings.RequestScrape);
            Assert.Equal(StopConditionType.Never, entry.Settings.Stop.Type);
        }

        [Fact]
        public void ParsesATorrentWrittenWithTheMinimumRequiredKeys()
        {
            var info = new BencodeDictionary()
                .Set("name", "ubuntu-24.04-desktop-amd64.iso")
                .Set("length", 6_203_355_136)
                .Set("piece length", 262_144)
                .Set("pieces", new BencodeString(new byte[20 * 24]));
            var root = new BencodeDictionary()
                .Set("announce", "http://tracker.example.org:6969/announce")
                .Set("created by", "RatioMaster smoke test")
                .Set("info", info);

            var torrent = TorrentFile.Parse(root.ToBytes());

            Assert.Equal("ubuntu-24.04-desktop-amd64.iso", torrent.Name);
            Assert.Equal(6_203_355_136, torrent.TotalLength);
            Assert.Equal("http://tracker.example.org:6969/announce", torrent.Announce);
            Assert.Equal(24, torrent.PieceCount);
        }

        [Fact]
        public void EmptyJsonObjectKeepsTheTorrentSettingsDefaults()
        {
            var direct = System.Text.Json.JsonSerializer.Deserialize(
                "{}", RatioMaster.Core.Settings.CoreJsonContext.Default.TorrentSettings);

            Assert.NotNull(direct);
            Assert.Equal(1800, direct!.IntervalSeconds);
            Assert.Equal(TorrentSettings.DefaultClientName, direct.ClientName);
            Assert.True(direct.RequestScrape);
            Assert.True(direct.UseTcpListener);

            // These are reference types: before the fix they came back null and crashed the session restore.
            Assert.NotNull(direct.Stop);
            Assert.NotNull(direct.Proxy);
        }

        [Fact]
        public void EmptyJsonObjectKeepsTheAppSettingsDefaults()
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize(
                "{}", RatioMaster.Core.Settings.CoreJsonContext.Default.AppSettings);

            Assert.NotNull(settings);
            Assert.True(settings!.CheckForUpdatesOnStartup);
            Assert.True(settings.RestoreLastSessionOnStartup);
            Assert.Equal(RatioMaster.Core.Settings.AppSettings.TrayIsReliable, settings.MinimizeToTray); // off by default on Linux
            Assert.Equal(1800, settings.DefaultTorrentSettings.IntervalSeconds);
            Assert.NotNull(settings.DefaultTorrentSettings.Stop);
            Assert.NotNull(settings.DefaultTorrentSettings.Proxy);
            Assert.NotNull(settings.Window);
        }
    }
}
