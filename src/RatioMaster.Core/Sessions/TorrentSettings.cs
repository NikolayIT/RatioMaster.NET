using RatioMaster.Core.Networking;
using System.Text.Json.Serialization;

namespace RatioMaster.Core.Sessions;

/// <summary>Everything the user can configure for one torrent. JSON-serializable; used by sessions and defaults.</summary>
public sealed record TorrentSettings
{
    /// <summary>
    /// Construct through the parameterless constructor so System.Text.Json sets only the properties
    /// present in the JSON and the initializers below survive. The properties use <c>set</c> rather
    /// than <c>init</c> for the same reason: the source generator cannot call an init accessor from a
    /// setter delegate, so it would assign every property in an object initializer and reset the
    /// absent ones to default.
    /// </summary>
    [JsonConstructor]
    public TorrentSettings()
    {
    }

    public const string DefaultClientName = "qBittorrent 5.2.3";

    /// <summary>The emulated client profile name.</summary>
    public string ClientName { get; set; } = DefaultClientName;

    public IdentityMode IdentityMode { get; set; } = IdentityMode.Automatic;

    public string? CustomPeerId { get; set; }

    public string? CustomKey { get; set; }

    public string? CustomPort { get; set; }

    public string? CustomNumWant { get; set; }

    /// <summary>Base upload rate in bytes per second.</summary>
    public long UploadRateBytes { get; set; } = 60 * 1024;

    /// <summary>Base download rate in bytes per second.</summary>
    public long DownloadRateBytes { get; set; } = 30 * 1024;

    public bool UploadRandomEnabled { get; set; } = true;

    public int UploadRandomMinKb { get; set; } = 1;

    public int UploadRandomMaxKb { get; set; } = 10;

    public bool DownloadRandomEnabled { get; set; } = true;

    public int DownloadRandomMinKb { get; set; } = 1;

    public int DownloadRandomMaxKb { get; set; } = 10;

    public bool NextUpdateRandomUpload { get; set; }

    public int NextUpdateUploadMinKb { get; set; } = 10;

    public int NextUpdateUploadMaxKb { get; set; } = 50;

    public bool NextUpdateRandomDownload { get; set; }

    public int NextUpdateDownloadMinKb { get; set; } = 10;

    public int NextUpdateDownloadMaxKb { get; set; } = 100;

    /// <summary>Starting completion percentage (100 means seed only).</summary>
    public double FinishedPercent { get; set; }

    public int IntervalSeconds { get; set; } = 1800;

    public StopCondition Stop { get; set; } = StopCondition.Never;

    public bool IgnoreFailureReason { get; set; }

    /// <summary>
    /// Pause the upload while the tracker reports no leechers (issue #16). There is nobody to upload to,
    /// and uploading anyway may get an account banned on a private tracker, so this is on by default. The
    /// upload resumes on its own once the tracker reports leechers again.
    /// </summary>
    public bool StopUploadWhenNoLeechers { get; set; } = true;

    public bool RequestScrape { get; set; } = true;

    public bool UseTcpListener { get; set; } = true;

    public bool EnableLog { get; set; } = true;

    public bool IgnoreCertificateErrors { get; set; }

    public ProxySettings Proxy { get; set; } = ProxySettings.None;
}
