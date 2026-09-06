using System.Globalization;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Sessions;

/// <summary>Parses the string formats RatioMaster.NET 0.43 wrote to the registry and to .session files.</summary>
public static class LegacyValueParser
{
    /// <summary>Maps a legacy "Stop after" combo entry and its value.</summary>
    public static StopCondition ParseStopCondition(string? type, string? value)
    {
        var parsed = ParseDouble(value, 0);
        return type switch
        {
            "After time:" => new StopCondition { Type = StopConditionType.AfterSeconds, Value = parsed },
            "When seeders <" => new StopCondition { Type = StopConditionType.SeedersBelow, Value = parsed },
            "When leechers <" => new StopCondition { Type = StopConditionType.LeechersBelow, Value = parsed },
            "When uploaded >" => new StopCondition { Type = StopConditionType.UploadedAboveMb, Value = parsed },
            "When downloaded >" => new StopCondition { Type = StopConditionType.DownloadedAboveMb, Value = parsed },
            "When leechers/seeders <" => new StopCondition { Type = StopConditionType.LeecherSeederRatioBelow, Value = parsed },
            _ => StopCondition.Never,
        };
    }

    /// <summary>Maps a legacy proxy combo entry ("None", "HTTP", "SOCKS4", "SOCKS4a", "SOCKS5").</summary>
    public static ProxyType ParseProxyType(string? type) => type switch
    {
        "HTTP" or "HttpConnect" => ProxyType.HttpConnect,
        "SOCKS4" or "Socks4" => ProxyType.Socks4,
        "SOCKS4a" or "Socks4a" => ProxyType.Socks4a,
        "SOCKS5" or "Socks5" => ProxyType.Socks5,
        _ => ProxyType.None,
    };

    /// <summary>Parses a number written with either a dot or a comma decimal separator.</summary>
    public static double ParseDouble(string? text, double fallback) =>
        double.TryParse((text ?? string.Empty).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    public static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    /// <summary>Converts a legacy kilobytes-per-second string to bytes per second.</summary>
    public static long KilobytesToBytes(string? text, long fallbackKb) => (long)(ParseDouble(text, fallbackKb) * 1024);
}
