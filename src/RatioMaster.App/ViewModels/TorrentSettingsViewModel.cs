namespace RatioMaster.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using RatioMaster.App.Resources;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Sessions;

    /// <summary>
    /// The single settings editor, reused by the Add dialog, the details Settings tab and the defaults page.
    /// Fields the old app locked once a torrent was started stay locked while it runs.
    /// </summary>
    public sealed partial class TorrentSettingsViewModel : ViewModelBase
    {
        private const decimal MaxSpeedKb = 1_000_000_000m;

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
        [NotifyPropertyChangedFor(nameof(CanEditPortAndPeers))]
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
        private decimal? uploadKb = 60m;

        [ObservableProperty]
        private decimal? downloadKb = 30m;

        [ObservableProperty]
        private bool uploadRandomEnabled = true;

        [ObservableProperty]
        private decimal? uploadRandomMinKb = 1m;

        [ObservableProperty]
        private decimal? uploadRandomMaxKb = 10m;

        [ObservableProperty]
        private bool downloadRandomEnabled = true;

        [ObservableProperty]
        private decimal? downloadRandomMinKb = 1m;

        [ObservableProperty]
        private decimal? downloadRandomMaxKb = 10m;

        [ObservableProperty]
        private bool nextUpdateRandomUpload;

        [ObservableProperty]
        private decimal? nextUpdateUploadMinKb = 10m;

        [ObservableProperty]
        private decimal? nextUpdateUploadMaxKb = 50m;

        [ObservableProperty]
        private bool nextUpdateRandomDownload;

        [ObservableProperty]
        private decimal? nextUpdateDownloadMinKb = 10m;

        [ObservableProperty]
        private decimal? nextUpdateDownloadMaxKb = 100m;

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
        [NotifyPropertyChangedFor(nameof(CanEditPortAndPeers))]
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

        /// <summary>
        /// Gets a value indicating whether the Port and Peers wanted boxes take a value: only for a custom identity, since an
        /// automatic one always uses the client's own port and peer count (what was typed there used to be ignored), and
        /// not while the torrent runs.
        /// </summary>
        public bool CanEditPortAndPeers => !this.UseAutomaticIdentity && !this.IsLocked;

        /// <summary>
        /// Cleared numeric fields have a null value. Disabled random ranges are ignored until enabled.
        /// </summary>
        public bool IsSpeedSettingsValid =>
            IsValidSpeed(this.UploadKb) &&
            IsValidSpeed(this.DownloadKb) &&
            (!this.UploadRandomEnabled || IsValidRange(this.UploadRandomMinKb, this.UploadRandomMaxKb)) &&
            (!this.DownloadRandomEnabled || IsValidRange(this.DownloadRandomMinKb, this.DownloadRandomMaxKb)) &&
            (!this.NextUpdateRandomUpload || IsValidRange(this.NextUpdateUploadMinKb, this.NextUpdateUploadMaxKb)) &&
            (!this.NextUpdateRandomDownload || IsValidRange(this.NextUpdateDownloadMinKb, this.NextUpdateDownloadMaxKb));

        /// <summary>Reports friendly input errors without exposing .NET conversion exceptions in the form.</summary>
        public string? SpeedValidationMessage
        {
            get
            {
                if (this.IsSpeedSettingsValid)
                {
                    return null;
                }

                if (!IsValidSpeed(this.UploadKb) || !IsValidSpeed(this.DownloadKb))
                {
                    return "Enter a non-negative upload and download speed (kB/s).";
                }

                if ((this.UploadRandomEnabled && (!IsValidRangeNumber(this.UploadRandomMinKb) || !IsValidRangeNumber(this.UploadRandomMaxKb))) ||
                    (this.DownloadRandomEnabled && (!IsValidRangeNumber(this.DownloadRandomMinKb) || !IsValidRangeNumber(this.DownloadRandomMaxKb))) ||
                    (this.NextUpdateRandomUpload && (!IsValidRangeNumber(this.NextUpdateUploadMinKb) || !IsValidRangeNumber(this.NextUpdateUploadMaxKb))) ||
                    (this.NextUpdateRandomDownload && (!IsValidRangeNumber(this.NextUpdateDownloadMinKb) || !IsValidRangeNumber(this.NextUpdateDownloadMaxKb))))
                {
                    return "Enter whole, non-negative min/max speeds (kB/s) for each enabled option.";
                }

                return "The minimum speed must not exceed the maximum speed (kB/s).";
            }
        }

        public bool IsProxyConfigured => this.ProxyType != ProxyType.None;

        public bool IsStopValueVisible => this.StopType != StopConditionType.Never;

        public string StopValueUnit => this.StopType switch
        {
            StopConditionType.AfterSeconds => Strings.UnitSeconds,
            StopConditionType.UploadedAboveMb or StopConditionType.DownloadedAboveMb => Strings.UnitMegabytes,
            StopConditionType.LeecherSeederRatioBelow => Strings.UnitRatio,
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

            this.UploadKb = settings.UploadRateBytes / 1024m;
            this.DownloadKb = settings.DownloadRateBytes / 1024m;
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
            UploadRateBytes = (long)(Math.Clamp(this.UploadKb ?? 0m, 0m, MaxSpeedKb) * 1024m),
            DownloadRateBytes = (long)(Math.Clamp(this.DownloadKb ?? 0m, 0m, MaxSpeedKb) * 1024m),
            UploadRandomEnabled = this.UploadRandomEnabled,
            UploadRandomMinKb = AsRangeInteger(this.UploadRandomMinKb),
            UploadRandomMaxKb = AsRangeInteger(this.UploadRandomMaxKb),
            DownloadRandomEnabled = this.DownloadRandomEnabled,
            DownloadRandomMinKb = AsRangeInteger(this.DownloadRandomMinKb),
            DownloadRandomMaxKb = AsRangeInteger(this.DownloadRandomMaxKb),
            NextUpdateRandomUpload = this.NextUpdateRandomUpload,
            NextUpdateUploadMinKb = AsRangeInteger(this.NextUpdateUploadMinKb),
            NextUpdateUploadMaxKb = AsRangeInteger(this.NextUpdateUploadMaxKb),
            NextUpdateRandomDownload = this.NextUpdateRandomDownload,
            NextUpdateDownloadMinKb = AsRangeInteger(this.NextUpdateDownloadMinKb),
            NextUpdateDownloadMaxKb = AsRangeInteger(this.NextUpdateDownloadMaxKb),
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

        private static bool IsValidSpeed(decimal? value) => value is >= 0 and <= MaxSpeedKb;

        private static bool IsValidRangeNumber(decimal? value) =>
            value.HasValue &&
            value.Value >= 0 &&
            value.Value <= int.MaxValue &&
            decimal.Truncate(value.Value) == value.Value;

        private static bool IsValidRange(decimal? min, decimal? max) =>
            IsValidRangeNumber(min) && IsValidRangeNumber(max) && min!.Value <= max!.Value;

        // Disabled ranges may have unfinished values; never let them overflow during serialization.
        private static int AsRangeInteger(decimal? value) =>
            (int)Math.Clamp(decimal.Truncate(value ?? 0m), 0m, int.MaxValue);

        private void NotifySpeedValidationChanged()
        {
            this.OnPropertyChanged(nameof(this.IsSpeedSettingsValid));
            this.OnPropertyChanged(nameof(this.SpeedValidationMessage));
        }

        partial void OnUploadKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnDownloadKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnUploadRandomEnabledChanged(bool value) => this.NotifySpeedValidationChanged();
        partial void OnUploadRandomMinKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnUploadRandomMaxKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnDownloadRandomEnabledChanged(bool value) => this.NotifySpeedValidationChanged();
        partial void OnDownloadRandomMinKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnDownloadRandomMaxKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateRandomUploadChanged(bool value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateUploadMinKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateUploadMaxKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateRandomDownloadChanged(bool value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateDownloadMinKbChanged(decimal? value) => this.NotifySpeedValidationChanged();
        partial void OnNextUpdateDownloadMaxKbChanged(decimal? value) => this.NotifySpeedValidationChanged();

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
            this.IdentityStatus = Strings.IdentityGenerating;
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
                    ClientIdentitySource.CopiedFromProcess => string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.IdentityCopied, this.ClientName),
                    _ => this.ProfileSupportsScanning()
                        ? Strings.IdentityGeneratedNotRunning
                        : Strings.IdentityGenerated,
                };
            }
            finally
            {
                this.IsGeneratingIdentity = false;
            }
        }

        private bool ProfileSupportsScanning() => this.catalog.TryGet(this.ClientName, out var profile) && profile.CanScanMemory;
    }
}
