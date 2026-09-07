using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;

namespace RatioMaster.App.ViewModels;

/// <summary>
/// The single settings editor, reused by the Add dialog, the details Settings tab and the defaults page.
/// Fields the old app locked once a torrent was started stay locked while it runs.
/// </summary>
public sealed partial class TorrentSettingsViewModel : ViewModelBase
{
    private readonly ClientProfileCatalog catalog;
    private readonly TorrentSessionFactory? factory;
    private bool suppressVersionReset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientName))]
    private string clientFamily;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientName))]
    private string clientVersion;

    [ObservableProperty]
    private bool useAutomaticIdentity = true;

    [ObservableProperty]
    private string peerId = string.Empty;

    [ObservableProperty]
    private string key = string.Empty;

    [ObservableProperty]
    private string port = string.Empty;

    [ObservableProperty]
    private string numWant = string.Empty;

    [ObservableProperty]
    private string identityStatus = string.Empty;

    [ObservableProperty]
    private bool isGeneratingIdentity;

    [ObservableProperty]
    private double uploadKb = 60;

    [ObservableProperty]
    private double downloadKb = 30;

    [ObservableProperty]
    private bool uploadRandomEnabled = true;

    [ObservableProperty]
    private int uploadRandomMinKb = 1;

    [ObservableProperty]
    private int uploadRandomMaxKb = 10;

    [ObservableProperty]
    private bool downloadRandomEnabled = true;

    [ObservableProperty]
    private int downloadRandomMinKb = 1;

    [ObservableProperty]
    private int downloadRandomMaxKb = 10;

    [ObservableProperty]
    private bool nextUpdateRandomUpload;

    [ObservableProperty]
    private int nextUpdateUploadMinKb = 10;

    [ObservableProperty]
    private int nextUpdateUploadMaxKb = 50;

    [ObservableProperty]
    private bool nextUpdateRandomDownload;

    [ObservableProperty]
    private int nextUpdateDownloadMinKb = 10;

    [ObservableProperty]
    private int nextUpdateDownloadMaxKb = 100;

    [ObservableProperty]
    private double finishedPercent;

    [ObservableProperty]
    private int intervalSeconds = 1800;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopValueUnit))]
    [NotifyPropertyChangedFor(nameof(IsStopValueVisible))]
    private StopConditionType stopType = StopConditionType.Never;

    [ObservableProperty]
    private double stopValue;

    [ObservableProperty]
    private bool ignoreFailureReason;

    [ObservableProperty]
    private bool stopUploadWhenNoLeechers = true;

    [ObservableProperty]
    private bool requestScrape = true;

    [ObservableProperty]
    private bool useTcpListener = true;

    [ObservableProperty]
    private bool enableLog = true;

    [ObservableProperty]
    private bool ignoreCertificateErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProxyConfigured))]
    private ProxyType proxyType = ProxyType.None;

    [ObservableProperty]
    private string proxyHost = string.Empty;

    [ObservableProperty]
    private int proxyPort;

    [ObservableProperty]
    private string proxyUsername = string.Empty;

    [ObservableProperty]
    private string proxyPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocked))]
    private bool isRunning;

    public TorrentSettingsViewModel(ClientProfileCatalog catalog, TorrentSessionFactory? factory = null, TorrentSettings? settings = null)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.factory = factory;
        this.Families = new ObservableCollection<string>(catalog.Families);
        this.clientFamily = catalog.Default.Family;
        this.clientVersion = catalog.Default.Version;
        this.Versions = new ObservableCollection<string>(catalog.VersionsOf(this.clientFamily));
        if (settings is not null)
        {
            this.LoadFrom(settings);
        }
    }

    public static IReadOnlyList<StopConditionType> StopTypes { get; } = Enum.GetValues<StopConditionType>();

    public static IReadOnlyList<ProxyType> ProxyTypes { get; } = Enum.GetValues<ProxyType>();

    public ObservableCollection<string> Families { get; }

    public ObservableCollection<string> Versions { get; }

    /// <summary>Gets the profile name assembled from the family and version pickers.</summary>
    public string ClientName => $"{this.ClientFamily} {this.ClientVersion}".Trim();

    /// <summary>Gets a value indicating whether the torrent runs: the fields the engine cannot change mid-flight are disabled.</summary>
    public bool IsLocked => this.IsRunning;

    public bool IsProxyConfigured => this.ProxyType != ProxyType.None;

    public bool IsStopValueVisible => this.StopType != StopConditionType.Never;

    public string StopValueUnit => this.StopType switch
    {
        StopConditionType.AfterSeconds => "seconds",
        StopConditionType.UploadedAboveMb or StopConditionType.DownloadedAboveMb => "MB",
        StopConditionType.LeecherSeederRatioBelow => "ratio",
        _ => string.Empty,
    };

    /// <summary>Fills the editor from a settings record.</summary>
    public void LoadFrom(TorrentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var profile = this.catalog.GetByName(settings.ClientName);

        this.suppressVersionReset = true;
        this.ClientFamily = profile.Family;
        this.suppressVersionReset = false;
        this.ClientVersion = profile.Version;

        this.UseAutomaticIdentity = settings.IdentityMode == IdentityMode.Automatic;
        this.PeerId = settings.CustomPeerId ?? string.Empty;
        this.Key = settings.CustomKey ?? string.Empty;
        this.Port = settings.CustomPort ?? string.Empty;
        this.NumWant = settings.CustomNumWant ?? string.Empty;

        this.UploadKb = settings.UploadRateBytes / 1024d;
        this.DownloadKb = settings.DownloadRateBytes / 1024d;
        this.UploadRandomEnabled = settings.UploadRandomEnabled;
        this.UploadRandomMinKb = settings.UploadRandomMinKb;
        this.UploadRandomMaxKb = settings.UploadRandomMaxKb;
        this.DownloadRandomEnabled = settings.DownloadRandomEnabled;
        this.DownloadRandomMinKb = settings.DownloadRandomMinKb;
        this.DownloadRandomMaxKb = settings.DownloadRandomMaxKb;

        this.NextUpdateRandomUpload = settings.NextUpdateRandomUpload;
        this.NextUpdateUploadMinKb = settings.NextUpdateUploadMinKb;
        this.NextUpdateUploadMaxKb = settings.NextUpdateUploadMaxKb;
        this.NextUpdateRandomDownload = settings.NextUpdateRandomDownload;
        this.NextUpdateDownloadMinKb = settings.NextUpdateDownloadMinKb;
        this.NextUpdateDownloadMaxKb = settings.NextUpdateDownloadMaxKb;

        this.FinishedPercent = settings.FinishedPercent;
        this.IntervalSeconds = settings.IntervalSeconds;
        this.StopType = settings.Stop.Type;
        this.StopValue = settings.Stop.Value;

        this.IgnoreFailureReason = settings.IgnoreFailureReason;
        this.StopUploadWhenNoLeechers = settings.StopUploadWhenNoLeechers;
        this.RequestScrape = settings.RequestScrape;
        this.UseTcpListener = settings.UseTcpListener;
        this.EnableLog = settings.EnableLog;
        this.IgnoreCertificateErrors = settings.IgnoreCertificateErrors;

        this.ProxyType = settings.Proxy.Type;
        this.ProxyHost = settings.Proxy.Host;
        this.ProxyPort = settings.Proxy.Port;
        this.ProxyUsername = settings.Proxy.Username;
        this.ProxyPassword = settings.Proxy.Password;
    }

    /// <summary>Builds a settings record from the editor.</summary>
    public TorrentSettings ToSettings() => new()
    {
        ClientName = this.ClientName,
        IdentityMode = this.UseAutomaticIdentity ? IdentityMode.Automatic : IdentityMode.Custom,
        CustomPeerId = NullIfBlank(this.PeerId),
        CustomKey = NullIfBlank(this.Key),
        CustomPort = NullIfBlank(this.Port),
        CustomNumWant = NullIfBlank(this.NumWant),
        UploadRateBytes = (long)Math.Max(0, this.UploadKb * 1024),
        DownloadRateBytes = (long)Math.Max(0, this.DownloadKb * 1024),
        UploadRandomEnabled = this.UploadRandomEnabled,
        UploadRandomMinKb = this.UploadRandomMinKb,
        UploadRandomMaxKb = this.UploadRandomMaxKb,
        DownloadRandomEnabled = this.DownloadRandomEnabled,
        DownloadRandomMinKb = this.DownloadRandomMinKb,
        DownloadRandomMaxKb = this.DownloadRandomMaxKb,
        NextUpdateRandomUpload = this.NextUpdateRandomUpload,
        NextUpdateUploadMinKb = this.NextUpdateUploadMinKb,
        NextUpdateUploadMaxKb = this.NextUpdateUploadMaxKb,
        NextUpdateRandomDownload = this.NextUpdateRandomDownload,
        NextUpdateDownloadMinKb = this.NextUpdateDownloadMinKb,
        NextUpdateDownloadMaxKb = this.NextUpdateDownloadMaxKb,
        FinishedPercent = Math.Clamp(this.FinishedPercent, 0, 100),
        IntervalSeconds = this.IntervalSeconds,
        Stop = new StopCondition { Type = this.StopType, Value = this.StopValue },
        IgnoreFailureReason = this.IgnoreFailureReason,
        StopUploadWhenNoLeechers = this.StopUploadWhenNoLeechers,
        RequestScrape = this.RequestScrape,
        UseTcpListener = this.UseTcpListener,
        EnableLog = this.EnableLog,
        IgnoreCertificateErrors = this.IgnoreCertificateErrors,
        Proxy = new ProxySettings
        {
            Type = this.ProxyType,
            Host = this.ProxyHost,
            Port = this.ProxyPort,
            Username = this.ProxyUsername,
            Password = this.ProxyPassword,
        },
    };

    private static double DefaultStopValue(StopConditionType type) => type switch
    {
        StopConditionType.AfterSeconds => 3600,
        StopConditionType.SeedersBelow or StopConditionType.LeechersBelow => 10,
        StopConditionType.UploadedAboveMb or StopConditionType.DownloadedAboveMb => 1024,
        StopConditionType.LeecherSeederRatioBelow => 1.0,
        _ => 0,
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    partial void OnClientFamilyChanged(string value)
    {
        Versions.Clear();
        foreach (var version in catalog.VersionsOf(value))
        {
            Versions.Add(version);
        }

        if (!suppressVersionReset && Versions.Count > 0)
        {
            ClientVersion = Versions[0];
        }

        // Adopt the client's own default peer count when the user has not set one.
        if (string.IsNullOrWhiteSpace(NumWant) && catalog.TryGet(ClientName, out var profile))
        {
            NumWant = profile.DefaultNumWant.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    partial void OnStopTypeChanged(StopConditionType value) => StopValue = DefaultStopValue(value);

    /// <summary>Generates or copies fresh identity values, off the UI thread (the memory scan can take seconds).</summary>
    [RelayCommand]
    private async Task RegenerateIdentityAsync()
    {
        if (this.factory is null || this.IsGeneratingIdentity)
        {
            return;
        }

        this.IsGeneratingIdentity = true;
        this.IdentityStatus = "Generating...";
        try
        {
            var settings = this.ToSettings() with { IdentityMode = IdentityMode.Automatic };
            var identity = await Task.Run(() => this.factory.CreateIdentity(settings));

            this.PeerId = identity.PeerId;
            this.Key = identity.Key;
            this.Port = identity.Port;
            this.NumWant = identity.NumWant;
            this.IdentityStatus = identity.Source switch
            {
                ClientIdentitySource.CopiedFromProcess => $"Copied from the running {this.ClientName}.",
                _ => this.ProfileSupportsScanning()
                    ? "Generated random values (the client is not running)."
                    : "Generated random values.",
            };
        }
        finally
        {
            this.IsGeneratingIdentity = false;
        }
    }

    private bool ProfileSupportsScanning() => this.catalog.TryGet(this.ClientName, out var profile) && profile.CanScanMemory;
}
