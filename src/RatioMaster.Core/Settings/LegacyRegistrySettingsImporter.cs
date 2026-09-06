using System.Runtime.Versioning;
using Microsoft.Win32;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Settings;

/// <summary>Reads the values RatioMaster.NET 0.43 stored under HKCU\Software\RatioMaster.NET.</summary>
public interface ILegacyRegistryReader
{
    bool Exists { get; }

    string? GetString(string name);

    int? GetInt(string name);
}

/// <summary>The real registry reader (Windows only).</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsLegacyRegistryReader : ILegacyRegistryReader, IDisposable
{
    public const string KeyPath = @"Software\RatioMaster.NET";

    private readonly RegistryKey? _key;

    public WindowsLegacyRegistryReader()
    {
        try
        {
            _key = Registry.CurrentUser.OpenSubKey(KeyPath);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            _key = null;
        }
    }

    public bool Exists => _key is not null;

    public string? GetString(string name) => _key?.GetValue(name)?.ToString();

    public int? GetInt(string name) => _key?.GetValue(name) is int value ? value : null;

    public void Dispose() => _key?.Dispose();
}

/// <summary>
/// One-time migration of the 0.43 registry settings into <see cref="AppSettings"/>, so upgrading users
/// keep their defaults.
/// </summary>
public static class LegacyRegistrySettingsImporter
{
    /// <summary>Returns the baseline with the legacy values applied, or null when there is nothing to import.</summary>
    public static AppSettings? TryImport(ILegacyRegistryReader reader, AppSettings? baseline = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (!reader.Exists || reader.GetString("Version") is null)
        {
            return null;
        }

        var settings = baseline ?? new AppSettings();
        var defaults = settings.DefaultTorrentSettings;

        var client = reader.GetString("Client");
        var clientVersion = reader.GetString("ClientVersion");
        var clientName = string.IsNullOrWhiteSpace(client)
            ? defaults.ClientName
            : $"{client} {clientVersion}".Trim();

        var alwaysNewValues = Bool(reader, "NewValues", true);

        return settings with
        {
            ShowTorrentListInTrayTooltip = Bool(reader, "BallonTip", settings.ShowTorrentListInTrayTooltip),
            MinimizeToTray = Bool(reader, "MinimizeToTray", settings.MinimizeToTray),
            CloseToTray = Bool(reader, "CloseToTray", settings.CloseToTray),
            LastTorrentDirectory = NullIfEmpty(reader.GetString("Directory")) ?? settings.LastTorrentDirectory,
            DefaultTorrentSettings = defaults with
            {
                ClientName = clientName,
                IdentityMode = alwaysNewValues ? IdentityMode.Automatic : IdentityMode.Custom,
                CustomKey = NullIfEmpty(reader.GetString("CustomKey")),
                CustomPeerId = NullIfEmpty(reader.GetString("CustomPeerID")),
                CustomPort = NullIfEmpty(reader.GetString("CustomPort")),
                CustomNumWant = NullIfEmpty(reader.GetString("CustomPeers")),
                UploadRateBytes = LegacyValueParser.KilobytesToBytes(reader.GetString("UploadRate"), defaults.UploadRateBytes / 1024),
                DownloadRateBytes = LegacyValueParser.KilobytesToBytes(reader.GetString("DownloadRate"), defaults.DownloadRateBytes / 1024),
                IntervalSeconds = LegacyValueParser.ParseInt(reader.GetString("Interval"), defaults.IntervalSeconds),
                FinishedPercent = LegacyValueParser.ParseDouble(reader.GetString("fileSize"), defaults.FinishedPercent),
                UseTcpListener = Bool(reader, "TCPlistener", defaults.UseTcpListener),
                RequestScrape = Bool(reader, "ScrapeInfo", defaults.RequestScrape),
                EnableLog = Bool(reader, "EnableLog", defaults.EnableLog),
                IgnoreFailureReason = Bool(reader, "IgnoreFailureReason", defaults.IgnoreFailureReason),
                UploadRandomEnabled = Bool(reader, "GetRandUp", defaults.UploadRandomEnabled),
                UploadRandomMinKb = LegacyValueParser.ParseInt(reader.GetString("MinRandUp"), defaults.UploadRandomMinKb),
                UploadRandomMaxKb = LegacyValueParser.ParseInt(reader.GetString("MaxRandUp"), defaults.UploadRandomMaxKb),
                DownloadRandomEnabled = Bool(reader, "GetRandDown", defaults.DownloadRandomEnabled),
                DownloadRandomMinKb = LegacyValueParser.ParseInt(reader.GetString("MinRandDown"), defaults.DownloadRandomMinKb),
                DownloadRandomMaxKb = LegacyValueParser.ParseInt(reader.GetString("MaxRandDown"), defaults.DownloadRandomMaxKb),
                NextUpdateRandomUpload = Bool(reader, "GetRandUpNext", defaults.NextUpdateRandomUpload),
                NextUpdateUploadMinKb = LegacyValueParser.ParseInt(reader.GetString("MinRandUpNext"), defaults.NextUpdateUploadMinKb),
                NextUpdateUploadMaxKb = LegacyValueParser.ParseInt(reader.GetString("MaxRandUpNext"), defaults.NextUpdateUploadMaxKb),
                NextUpdateRandomDownload = Bool(reader, "GetRandDownNext", defaults.NextUpdateRandomDownload),
                NextUpdateDownloadMinKb = LegacyValueParser.ParseInt(reader.GetString("MinRandDownNext"), defaults.NextUpdateDownloadMinKb),
                NextUpdateDownloadMaxKb = LegacyValueParser.ParseInt(reader.GetString("MaxRandDownNext"), defaults.NextUpdateDownloadMaxKb),
                Stop = LegacyValueParser.ParseStopCondition(reader.GetString("StopWhen"), reader.GetString("StopAfter")),
                Proxy = new ProxySettings
                {
                    Type = LegacyValueParser.ParseProxyType(reader.GetString("ProxyType")),
                    Host = reader.GetString("ProxyAdress") ?? string.Empty,
                    Port = LegacyValueParser.ParseInt(reader.GetString("ProxyPort"), 0),
                    Username = reader.GetString("ProxyUser") ?? string.Empty,
                    Password = reader.GetString("ProxyPass") ?? string.Empty,
                },
            },
        };
    }

    private static bool Bool(ILegacyRegistryReader reader, string name, bool fallback) =>
        reader.GetInt(name) is { } value ? value != 0 : fallback;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
