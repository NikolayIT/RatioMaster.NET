using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RatioMaster.Core.Clients;

namespace RatioMaster.Core.MemoryScan;

/// <summary>
/// Copies a running client's real announce values (peer id, key, port, numwant) out of its memory,
/// reproducing the old RM.GETDATA scan.
/// </summary>
public sealed partial class ClientValueScanner
{
    private const int BlockSize = 64 * 1024;

    private readonly IProcessMemoryScanner scanner;

    public ClientValueScanner(IProcessMemoryScanner? scanner = null)
    {
        this.scanner = scanner ?? ProcessMemoryScanners.ForCurrentPlatform();
    }

    public bool IsSupported => this.scanner.IsSupported;

    /// <summary>
    /// Scans the client's process and returns the values found, or null when the client is not running,
    /// the platform does not allow it, or nothing matched. <paramref name="fallback"/> fills any missing field.
    /// </summary>
    public ClientIdentity? TryScan(ClientProfile profile, ClientIdentity fallback, Action<string>? log = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(fallback);

        var spec = profile.MemoryScan;
        if (spec is null || !this.scanner.IsSupported)
        {
            return null;
        }

        log?.Invoke($"Looking for the {spec.ProcessName} process...");
        using var session = this.scanner.Open(spec.ProcessName);
        if (session is null)
        {
            log?.Invoke($"No {spec.ProcessName} process found. Make sure the torrent client is running.");
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
            log?.Invoke("Search finished successfully.");
            return identity;
        }

        log?.Invoke($"Search failed. Make sure {profile.Name} is running with at least one active torrent.");
        return null;
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
}
