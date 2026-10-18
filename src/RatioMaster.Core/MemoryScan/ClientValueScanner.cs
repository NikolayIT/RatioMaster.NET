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

        private const int BlockSize = 64 * 1024;

        private readonly IProcessMemoryScanner scanner;
        private readonly ISystemClock clock;
        private readonly Lock gate = new();

        /// <summary>The newest search that ended for each profile, by name: its number, and when it found nothing (null when it found the client).</summary>
        private readonly Dictionary<string, (long Search, DateTimeOffset? MissedAt)> results = new(StringComparer.Ordinal);

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
        /// After a search that found nothing the client is not looked for again for <see cref="MissWaitSeconds"/>
        /// seconds, unless <paramref name="ignoreRecentMiss"/> is set: when the user asks, the client may just have started.
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

            bool skip;
            long search = 0;
            lock (this.gate)
            {
                skip = !ignoreRecentMiss && this.MissedRecently(profile.Name);
                if (!skip)
                {
                    search = ++this.searches;
                }
            }

            if (skip)
            {
                log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanSkippedAfterMiss, spec.ProcessName));
                return null;
            }

            var identity = this.Scan(profile, spec, fallback, log);
            lock (this.gate)
            {
                // Searches can overlap (a start on the UI thread, Regenerate on the thread pool). The one that began last
                // saw the client last, so an older one that ends after it does not undo what it found.
                if (!this.results.TryGetValue(profile.Name, out var newest) || newest.Search < search)
                {
                    this.results[profile.Name] = (search, identity is null ? this.clock.Now : null);
                }
            }

            return identity;
        }

        private static ClientIdentity Extract(string text, ClientProfile profile, ClientIdentity fallback, Action<string>? log)
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

            return new ClientIdentity
            {
                PeerId = peerId ?? fallback.PeerId,
                Key = key ?? fallback.Key,
                Port = port ?? fallback.Port,
                NumWant = numWant ?? fallback.NumWant,
                Source = ClientIdentitySource.CopiedFromProcess,
            };
        }

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

        private ClientIdentity? Scan(ClientProfile profile, ClientMemoryScanSpec spec, ClientIdentity fallback, Action<string>? log)
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

                var identity = Extract(text, profile, fallback, log);
                log?.Invoke(Strings.ScanSucceeded);
                return identity;
            }

            log?.Invoke(string.Format(CultureInfo.CurrentCulture, Strings.ScanFailed, profile.Name));
            return null;
        }

        /// <summary>Gets whether the newest search for the profile found nothing less than a minute ago. Call it inside the gate.</summary>
        private bool MissedRecently(string profileName)
        {
            if (!this.results.TryGetValue(profileName, out var newest) || newest.MissedAt is not { } missedAt)
            {
                return false;
            }

            // Measured both ways, so a clock set back since the miss does not stretch the wait.
            var since = this.clock.Now - missedAt;
            return since >= TimeSpan.Zero && since < TimeSpan.FromSeconds(MissWaitSeconds);
        }
    }
}
