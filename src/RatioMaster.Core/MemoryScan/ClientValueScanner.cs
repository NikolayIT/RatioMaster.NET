namespace RatioMaster.Core.MemoryScan
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    using RatioMaster.Core.Abstractions;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.Resources;

    /// <summary>
    /// Copies a running client's real announce values (peer id, key, port, numwant) out of its memory,
    /// reproducing the old RM.GETDATA scan.
    /// </summary>
    public sealed partial class ClientValueScanner
    {
        /// <summary>
        /// How long a search that found nothing stands before the next one. Every torrent that starts looks for its
        /// client, on the UI thread, and a search goes through the whole process list, and through the client's memory
        /// when it runs: starting a thousand uTorrent torrents at once kept the window busy for 11 seconds.
        /// </summary>
        internal const int MissWaitSeconds = 60;
        internal const int HitWaitSeconds = 5;

        private const int BlockSize = 64 * 1024;

        private readonly IProcessMemoryScanner scanner;
        private readonly ISystemClock clock;
        private readonly Lock gate = new();

        private readonly Dictionary<string, (long Search, DateTimeOffset CheckedAt, ScanValues? Values)> results = new(StringComparer.Ordinal);

        private long searches;

        public ClientValueScanner(IProcessMemoryScanner? scanner = null, ISystemClock? clock = null)
        {
            this.scanner = scanner ?? ProcessMemoryScanners.ForCurrentPlatform();
            this.clock = clock ?? SystemClock.Instance;
        }

        public bool IsSupported => this.scanner.IsSupported;

        /// <summary>
        /// Scans the client's process and returns the values found, or null when the client is not running,
        /// the platform does not allow it, or nothing matched. <paramref name="fallback"/> fills any missing field.
        /// A recent result is reused for <see cref="HitWaitSeconds"/> seconds, or <see cref="MissWaitSeconds"/>
        /// seconds when nothing was found. Explicit regeneration bypasses both waits.
        /// </summary>
        public ClientIdentity? TryScan(ClientProfile profile, ClientIdentity fallback, Action<string>? log = null, bool ignoreRecentMiss = false)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(fallback);

            var spec = profile.MemoryScan;
            if (spec is null || !this.scanner.IsSupported)
            {
                return null;
            }

            bool skipMiss = false;
            ScanValues? cached = null;
            long search = 0;
            lock (this.gate)
            {
                if (!ignoreRecentMiss &&
                    this.results.TryGetValue(profile.Name, out var previous) &&
                    this.IsRecent(previous.CheckedAt, previous.Values is null ? MissWaitSeconds : HitWaitSeconds))
                {
                    if (previous.Values is { } found)
                    {
                        cached = found;
                    }
                    else
                    {
                        skipMiss = true;
                    }
                }

                if (!skipMiss && cached is null)
                {
                    search = ++this.searches;
                }
            }

            if (cached is not null)
            {
                return ApplyFallback(cached, fallback);
            }

            if (skipMiss)
            {
                log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanSkippedAfterMiss, spec.ProcessName));
                return null;
            }

            var values = this.Scan(profile, spec, log);
            lock (this.gate)
            {
                // A newer forced scan must win even if an earlier scan finishes last.
                if (!this.results.TryGetValue(profile.Name, out var newest) || newest.Search < search)
                {
                    this.results[profile.Name] = (search, this.clock.Now, values);
                }
            }

            return values is null ? null : ApplyFallback(values, fallback);
        }

        private static ScanValues Extract(string text, ClientProfile profile, Action<string>? log)
        {
            var peerId = Match(PeerIdRegex(), text);
            var key = Match(KeyRegex(), text);
            var port = Match(PortRegex(), text);
            var numWant = Match(NumWantRegex(), text);

            if (peerId is not null)
            {
                log?.Invoke($"====> PeerID = {peerId}");
            }

            if (key is not null)
            {
                log?.Invoke($"====> Key = {key}");
            }

            if (port is not null)
            {
                log?.Invoke($"====> Port = {port}");
            }

            if (numWant is not null)
            {
                log?.Invoke($"====> NumWant = {numWant}");
            }

            if (numWant is not null && !int.TryParse(numWant, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                numWant = profile.DefaultNumWant.ToString(CultureInfo.InvariantCulture);
            }

            return new ScanValues(peerId, key, port, numWant);
        }

        private static ClientIdentity ApplyFallback(ScanValues values, ClientIdentity fallback) => new()
        {
            PeerId = values.PeerId ?? fallback.PeerId,
            Key = values.Key ?? fallback.Key,
            Port = values.Port ?? fallback.Port,
            NumWant = values.NumWant ?? fallback.NumWant,
            Source = ClientIdentitySource.CopiedFromProcess,
        };

        private static string? Match(Regex regex, string text)
        {
            var match = regex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        [GeneratedRegex("&peer_id=(.+?)(&| )")]
        private static partial Regex PeerIdRegex();

        [GeneratedRegex("&key=(.+?)(&| )")]
        private static partial Regex KeyRegex();

        [GeneratedRegex("&port=(.+?)(&| )")]
        private static partial Regex PortRegex();

        [GeneratedRegex("&numwant=(.+?)(&| )")]
        private static partial Regex NumWantRegex();

        private ScanValues? Scan(ClientProfile profile, ClientMemoryScanSpec spec, Action<string>? log)
        {
            log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanLookingForProcess, spec.ProcessName));
            using var session = this.scanner.Open(spec.ProcessName);
            if (session is null)
            {
                log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanProcessNotFound, spec.ProcessName));
                return null;
            }

            var buffer = new byte[BlockSize];
            for (var offset = spec.StartOffset; offset < spec.MaxOffset; offset += BlockSize)
            {
                var read = session.Read(offset, buffer);
                if (read <= 0)
                {
                    continue;
                }

                var text = Encoding.ASCII.GetString(buffer, 0, read);
                if (!text.Contains(spec.SearchString, StringComparison.Ordinal))
                {
                    continue;
                }

                var values = Extract(text, profile, log);
                log?.Invoke(Strings.ScanSucceeded);
                return values;
            }

            log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanFailed, profile.Name));
            return null;
        }

        private bool IsRecent(DateTimeOffset checkedAt, int seconds)
        {
            // A clock adjustment backwards should not extend a cached result.
            var since = this.clock.Now - checkedAt;
            return since >= TimeSpan.Zero && since < TimeSpan.FromSeconds(seconds);
        }

        private sealed record ScanValues(string? PeerId, string? Key, string? Port, string? NumWant);
    }
}
