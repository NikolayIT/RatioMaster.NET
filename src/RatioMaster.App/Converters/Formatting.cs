using System;
using System.Globalization;

namespace RatioMaster.App.Converters;

/// <summary>Shared display formatting, matching the wording the old app used.</summary>
public static class Formatting
{
    public const string Unknown = "—";

    /// <summary>Formats a byte count as bytes, KB, MB or GB.</summary>
    public static string Bytes(long value)
    {
        if (value < 0)
        {
            return Unknown;
        }

        return value switch
        {
            >= 0x40000000 => string.Format(CultureInfo.CurrentCulture, "{0:########0.00} GB", value / 1073741824d),
            >= 0x100000 => string.Format(CultureInfo.CurrentCulture, "{0:####0.00} MB", value / 1048576d),
            >= 0x400 => string.Format(CultureInfo.CurrentCulture, "{0:####0.00} KB", value / 1024d),
            _ => string.Format(CultureInfo.CurrentCulture, "{0} bytes", value),
        };
    }

    /// <summary>Formats a rate in bytes per second as kB/s (or MB/s when large).</summary>
    public static string Speed(long bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
        {
            return "0 kB/s";
        }

        return bytesPerSecond >= 1024 * 1024
            ? string.Format(CultureInfo.CurrentCulture, "{0:0.00} MB/s", bytesPerSecond / 1048576d)
            : string.Format(CultureInfo.CurrentCulture, "{0:0.#} kB/s", bytesPerSecond / 1024d);
    }

    /// <summary>mm:ss below an hour, hh:mm:ss above it (as ConvertToTime did).</summary>
    public static string Duration(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        return value.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", (int)value.TotalHours, value.Minutes, value.Seconds)
            : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", value.Minutes, value.Seconds);
    }

    public static string Duration(TimeSpan? value) => value is { } v ? Duration(v) : Unknown;

    /// <summary>The ratio to two decimals, or "NaN" before enough has been downloaded (as in 0.43).</summary>
    public static string Ratio(double? value) =>
        value is { } v ? v.ToString("0.00", CultureInfo.CurrentCulture) : "NaN";

    public static string Percent(double value) =>
        value.ToString("0.00", CultureInfo.CurrentCulture) + " %";

    public static string Count(int? value) =>
        value is { } v ? v.ToString(CultureInfo.CurrentCulture) : Unknown;

    /// <summary>The host part of a tracker URL, for the compact column.</summary>
    public static string Host(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Unknown;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
    }

    /// <summary>Formats a timestamp honouring the 24-hour setting.</summary>
    public static string Time(DateTimeOffset value, bool use24Hour) =>
        value.ToString(use24Hour ? "HH:mm:ss" : "hh:mm:ss", CultureInfo.CurrentCulture);
}
