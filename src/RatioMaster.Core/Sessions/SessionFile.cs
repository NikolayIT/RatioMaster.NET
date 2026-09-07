namespace RatioMaster.Core.Sessions
{
    using System.Globalization;
    using System.Text.Json;
    using System.Xml.Linq;

    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Settings;

    /// <summary>
    /// Reads and writes .session files. New files are JSON; the XML format written by RatioMaster.NET 0.43
    /// is still read so old sessions keep working.
    /// </summary>
    public static class SessionFile
    {
        public static SessionDocument Load(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            return Parse(File.ReadAllText(path));
        }

        public static SessionDocument Parse(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            var trimmed = content.TrimStart();
            if (trimmed.StartsWith('<'))
            {
                return ParseLegacyXml(trimmed);
            }

            return JsonSerializer.Deserialize(content, CoreJsonContext.Default.SessionDocument) ?? new SessionDocument();
        }

        public static void Save(string path, SessionDocument document)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(document);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(document, CoreJsonContext.Default.SessionDocument);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, overwrite: true);
        }

        /// <summary>Reads the 0.43 XML format: &lt;main&gt;&lt;RatioMaster&gt;...&lt;/RatioMaster&gt;&lt;/main&gt;.</summary>
        public static SessionDocument ParseLegacyXml(string xml)
        {
            var document = XDocument.Parse(xml);
            var root = document.Root ?? throw new InvalidOperationException("The session file has no root element.");

            var entries = new List<SessionEntry>();
            foreach (var element in root.Elements())
            {
                var client = Text(element, "Client");
                var version = Text(element, "Version");
                var clientName = string.IsNullOrWhiteSpace(client)
                    ? TorrentSettings.DefaultClientName
                    : $"{client} {version}".Trim();

                var port = Text(element, "Port");
                var settings = new TorrentSettings
                {
                    ClientName = clientName,
                    IdentityMode = string.IsNullOrWhiteSpace(port) ? IdentityMode.Automatic : IdentityMode.Custom,
                    CustomPort = string.IsNullOrWhiteSpace(port) ? null : port,
                    UploadRateBytes = KilobytesToBytes(Text(element, "UploadSpeed"), 60),
                    DownloadRateBytes = KilobytesToBytes(Text(element, "DownloadSpeed"), 30),
                    UploadRandomEnabled = Bool(element, "UploadRandom", true),
                    UploadRandomMinKb = Int(element, "UploadRandMin", 1),
                    UploadRandomMaxKb = Int(element, "UploadRandMax", 10),
                    DownloadRandomEnabled = Bool(element, "DownloadRandom", true),
                    DownloadRandomMinKb = Int(element, "DownloadRandMin", 1),
                    DownloadRandomMaxKb = Int(element, "DownloadRandMax", 10),
                    NextUpdateRandomUpload = Bool(element, "NextUpdateUpload", false),
                    NextUpdateUploadMinKb = Int(element, "NextUpdateUploadFrom", 10),
                    NextUpdateUploadMaxKb = Int(element, "NextUpdateUploadTo", 50),
                    NextUpdateRandomDownload = Bool(element, "NextUpdateDownload", false),
                    NextUpdateDownloadMinKb = Int(element, "NextUpdateDownloadFrom", 10),
                    NextUpdateDownloadMaxKb = Int(element, "NextUpdateDownloadTo", 100),
                    FinishedPercent = Double(element, "Finished", 0),
                    Stop = ParseStopCondition(Text(element, "StopType"), Text(element, "StopValue")),
                    UseTcpListener = Bool(element, "UseTCP", true),
                    RequestScrape = Bool(element, "UseScrape", true),
                    IgnoreFailureReason = Bool(element, "IgnoreFailureReason", false),
                    Proxy = ParseProxy(element),
                };

                entries.Add(new SessionEntry
                {
                    Name = Text(element, "Name"),
                    TorrentPath = NullIfEmpty(Text(element, "Address")),
                    TrackerUrl = NullIfEmpty(Text(element, "Tracker")),
                    Settings = settings,
                });
            }

            return new SessionDocument { Version = 1, Torrents = entries };
        }

        private static ProxySettings ParseProxy(XElement element) => new()
        {
            Type = LegacyValueParser.ParseProxyType(Text(element, "ProxyType")),
            Host = Text(element, "ProxyHost"),
            Port = Int(element, "ProxyPort", 0),
            Username = Text(element, "ProxyUser"),
            Password = Text(element, "ProxyPass"),
        };

        private static StopCondition ParseStopCondition(string type, string value) =>
            LegacyValueParser.ParseStopCondition(type, value);

        private static string Text(XElement parent, string name) => parent.Element(name)?.Value.Trim() ?? string.Empty;

        private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static bool Bool(XElement parent, string name, bool fallback) =>
            bool.TryParse(Text(parent, name), out var value) ? value : fallback;

        private static int Int(XElement parent, string name, int fallback) =>
            LegacyValueParser.ParseInt(Text(parent, name), fallback);

        private static double Double(XElement parent, string name, double fallback) =>
            LegacyValueParser.ParseDouble(Text(parent, name), fallback);

        private static long KilobytesToBytes(string text, long fallbackKb) =>
            LegacyValueParser.KilobytesToBytes(text, fallbackKb);
    }
}
