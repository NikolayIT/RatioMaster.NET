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
    private readonly ClientProfileCatalog _catalog;
    private readonly TorrentSessionFactory? _factory;
    private bool _suppressVersionReset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientName))]
    private string _clientFamily;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientName))]
    private string _clientVersion;

    [ObservableProperty]
    private bool _useAutomaticIdentity = true;

    [ObservableProperty]
    private string _peerId = string.Empty;

    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _port = string.Empty;

    [ObservableProperty]
    private string _numWant = string.Empty;

    [ObservableProperty]
    private string _identityStatus = string.Empty;

    [ObservableProperty]
    private bool _isGeneratingIdentity;

    [ObservableProperty]
    private double _uploadKb = 60;

    [ObservableProperty]
    private double _downloadKb = 30;

    [ObservableProperty]
    private bool _uploadRandomEnabled = true;

    [ObservableProperty]
    private int _uploadRandomMinKb = 1;

    [ObservableProperty]
    private int _uploadRandomMaxKb = 10;

    [ObservableProperty]
    private bool _downloadRandomEnabled = true;

    [ObservableProperty]
    private int _downloadRandomMinKb = 1;

    [ObservableProperty]
    private int _downloadRandomMaxKb = 10;

    [ObservableProperty]
    private bool _nextUpdateRandomUpload;

    [ObservableProperty]
    private int _nextUpdateUploadMinKb = 10;

    [ObservableProperty]
    private int _nextUpdateUploadMaxKb = 50;

    [ObservableProperty]
    private bool _nextUpdateRandomDownload;

    [ObservableProperty]
    private int _nextUpdateDownloadMinKb = 10;

    [ObservableProperty]
    private int _nextUpdateDownloadMaxKb = 100;

    [ObservableProperty]
    private double _finishedPercent;

    [ObservableProperty]
    private int _intervalSeconds = 1800;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopValueUnit))]
    [NotifyPropertyChangedFor(nameof(IsStopValueVisible))]
    private StopConditionType _stopType = StopConditionType.Never;

    [ObservableProperty]
    private double _stopValue;

    [ObservableProperty]
    private bool _ignoreFailureReason;

    [ObservableProperty]
    private bool _requestScrape = true;

    [ObservableProperty]
    private bool _useTcpListener = true;

    [ObservableProperty]
    private bool _enableLog = true;

    [ObservableProperty]
    private bool _ignoreCertificateErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProxyConfigured))]
    private ProxyType _proxyType = ProxyType.None;

    [ObservableProperty]
    private string _proxyHost = string.Empty;

    [ObservableProperty]
    private int _proxyPort;

    [ObservableProperty]
    private string _proxyUsername = string.Empty;

    [ObservableProperty]
    private string _proxyPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocked))]
    private bool _isRunning;

    public TorrentSettingsViewModel(ClientProfileCatalog catalog, TorrentSessionFactory? factory = null, TorrentSettings? settings = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _factory = factory;
        Families = new ObservableCollection<string>(catalog.Families);
        _clientFamily = catalog.Default.Family;
        _clientVersion = catalog.Default.Version;
        Versions = new ObservableCollection<string>(catalog.VersionsOf(_clientFamily));
        if (settings is not null)
        {
            LoadFrom(settings);
        }
    }

    public ObservableCollection<string> Families { get; }

    public ObservableCollection<string> Versions { get; }

    public static IReadOnlyList<StopConditionType> StopTypes { get; } = Enum.GetValues<StopConditionType>();

    public static IReadOnlyList<ProxyType> ProxyTypes { get; } = Enum.GetValues<ProxyType>();

    /// <summary>The profile name assembled from the family and version pickers.</summary>
    public string ClientName => $"{ClientFamily} {ClientVersion}".Trim();

    /// <summary>True while the torrent runs: the fields the engine cannot change mid-flight are disabled.</summary>
    public bool IsLocked => IsRunning;

    public bool IsProxyConfigured => ProxyType != ProxyType.None;

    public bool IsStopValueVisible => StopType != StopConditionType.Never;

    public string StopValueUnit => StopType switch
    {
        StopConditionType.AfterSeconds => "seconds",
        StopConditionType.UploadedAboveMb or StopConditionType.DownloadedAboveMb => "MB",
        StopConditionType.LeecherSeederRatioBelow => "ratio",
        _ => string.Empty,
    };

    partial void OnClientFamilyChanged(string value)
    {
        Versions.Clear();
        foreach (var version in _catalog.VersionsOf(value))
        {
            Versions.Add(version);
        }

        if (!_suppressVersionReset && Versions.Count > 0)
        {
            ClientVersion = Versions[0];
        }

        // Adopt the client's own default peer count when the user has not set one.
        if (string.IsNullOrWhiteSpace(NumWant) && _catalog.TryGet(ClientName, out var profile))
        {
            NumWant = profile.DefaultNumWant.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    partial void OnStopTypeChanged(StopConditionType value) => StopValue = DefaultStopValue(value);

    private static double DefaultStopValue(StopConditionType type) => type switch
    {
        StopConditionType.AfterSeconds => 3600,
        StopConditionType.SeedersBelow or StopConditionType.LeechersBelow => 10,
        StopConditionType.UploadedAboveMb or StopConditionType.DownloadedAboveMb => 1024,
        StopConditionType.LeecherSeederRatioBelow => 1.0,
        _ => 0,
    };

    /// <summary>Fills the editor from a settings record.</summary>
    public void LoadFrom(TorrentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var profile = _catalog.GetByName(settings.ClientName);

        _suppressVersionReset = true;
        ClientFamily = profile.Family;
        _suppressVersionReset = false;
        ClientVersion = profile.Version;

        UseAutomaticIdentity = settings.IdentityMode == IdentityMode.Automatic;
        PeerId = settings.CustomPeerId ?? string.Empty;
        Key = settings.CustomKey ?? string.Empty;
        Port = settings.CustomPort ?? string.Empty;
        NumWant = settings.CustomNumWant ?? string.Empty;

        UploadKb = settings.UploadRateBytes / 1024d;
        DownloadKb = settings.DownloadRateBytes / 1024d;
        UploadRandomEnabled = settings.UploadRandomEnabled;
        UploadRandomMinKb = settings.UploadRandomMinKb;
        UploadRandomMaxKb = settings.UploadRandomMaxKb;
        DownloadRandomEnabled = settings.DownloadRandomEnabled;
        DownloadRandomMinKb = settings.DownloadRandomMinKb;
        DownloadRandomMaxKb = settings.DownloadRandomMaxKb;

        NextUpdateRandomUpload = settings.NextUpdateRandomUpload;
        NextUpdateUploadMinKb = settings.NextUpdateUploadMinKb;
        NextUpdateUploadMaxKb = settings.NextUpdateUploadMaxKb;
        NextUpdateRandomDownload = settings.NextUpdateRandomDownload;
        NextUpdateDownloadMinKb = settings.NextUpdateDownloadMinKb;
        NextUpdateDownloadMaxKb = settings.NextUpdateDownloadMaxKb;

        FinishedPercent = settings.FinishedPercent;
        IntervalSeconds = settings.IntervalSeconds;
        StopType = settings.Stop.Type;
        StopValue = settings.Stop.Value;

        IgnoreFailureReason = settings.IgnoreFailureReason;
        RequestScrape = settings.RequestScrape;
        UseTcpListener = settings.UseTcpListener;
        EnableLog = settings.EnableLog;
        IgnoreCertificateErrors = settings.IgnoreCertificateErrors;

        ProxyType = settings.Proxy.Type;
        ProxyHost = settings.Proxy.Host;
        ProxyPort = settings.Proxy.Port;
        ProxyUsername = settings.Proxy.Username;
        ProxyPassword = settings.Proxy.Password;
    }

    /// <summary>Builds a settings record from the editor.</summary>
    public TorrentSettings ToSettings() => new()
    {
        ClientName = ClientName,
        IdentityMode = UseAutomaticIdentity ? IdentityMode.Automatic : IdentityMode.Custom,
        CustomPeerId = NullIfBlank(PeerId),
        CustomKey = NullIfBlank(Key),
        CustomPort = NullIfBlank(Port),
        CustomNumWant = NullIfBlank(NumWant),
        UploadRateBytes = (long)Math.Max(0, UploadKb * 1024),
        DownloadRateBytes = (long)Math.Max(0, DownloadKb * 1024),
        UploadRandomEnabled = UploadRandomEnabled,
        UploadRandomMinKb = UploadRandomMinKb,
        UploadRandomMaxKb = UploadRandomMaxKb,
        DownloadRandomEnabled = DownloadRandomEnabled,
        DownloadRandomMinKb = DownloadRandomMinKb,
        DownloadRandomMaxKb = DownloadRandomMaxKb,
        NextUpdateRandomUpload = NextUpdateRandomUpload,
        NextUpdateUploadMinKb = NextUpdateUploadMinKb,
        NextUpdateUploadMaxKb = NextUpdateUploadMaxKb,
        NextUpdateRandomDownload = NextUpdateRandomDownload,
        NextUpdateDownloadMinKb = NextUpdateDownloadMinKb,
        NextUpdateDownloadMaxKb = NextUpdateDownloadMaxKb,
        FinishedPercent = Math.Clamp(FinishedPercent, 0, 100),
        IntervalSeconds = IntervalSeconds,
        Stop = new StopCondition { Type = StopType, Value = StopValue },
        IgnoreFailureReason = IgnoreFailureReason,
        RequestScrape = RequestScrape,
        UseTcpListener = UseTcpListener,
        EnableLog = EnableLog,
        IgnoreCertificateErrors = IgnoreCertificateErrors,
        Proxy = new ProxySettings
        {
            Type = ProxyType,
            Host = ProxyHost,
            Port = ProxyPort,
            Username = ProxyUsername,
            Password = ProxyPassword,
        },
    };

    /// <summary>Generates or copies fresh identity values, off the UI thread (the memory scan can take seconds).</summary>
    [RelayCommand]
    private async Task RegenerateIdentityAsync()
    {
        if (_factory is null || IsGeneratingIdentity)
        {
            return;
        }

        IsGeneratingIdentity = true;
        IdentityStatus = "Generating...";
        try
        {
            var settings = ToSettings() with { IdentityMode = IdentityMode.Automatic };
            var identity = await Task.Run(() => _factory.CreateIdentity(settings));

            PeerId = identity.PeerId;
            Key = identity.Key;
            Port = identity.Port;
            NumWant = identity.NumWant;
            IdentityStatus = identity.Source switch
            {
                ClientIdentitySource.CopiedFromProcess => $"Copied from the running {ClientName}.",
                _ => ProfileSupportsScanning()
                    ? "Generated random values (the client is not running)."
                    : "Generated random values.",
            };
        }
        finally
        {
            IsGeneratingIdentity = false;
        }
    }

    private bool ProfileSupportsScanning() => _catalog.TryGet(ClientName, out var profile) && profile.CanScanMemory;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
