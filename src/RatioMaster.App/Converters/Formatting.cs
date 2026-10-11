namespace RatioMaster.App.Converters
{
    using System;
    using System.Globalization;

    using RatioMaster.App.Resources;
    using RatioMaster.App.Services;

    /// <summary>Shared display formatting, matching the wording the old app used.</summary>
    public static class Formatting
    {
        public const string Unknown = "—";

        /// <summary>The left-to-right mark: a strong left-to-right character that keeps "60 kB/s" in order inside right-to-left text.</summary>
        private const char LeftToRightMark = '\u200E';

        /// <summary>The right-to-left mark, which ends such a quantity so the text after it (an arrow, a space) reads right to left again.</summary>
        private const char RightToLeftMark = '\u200F';

        /// <summary>Formats a byte count as bytes, KB, MB or GB.</summary>
        public static string Bytes(long value)
        {
            if (value < 0)
            {
                return Unknown;
            }

            return value switch
            {
                >= 0x40000000 => Unit(Strings.SizeGigabytes, value / 1073741824d, "0.00"),
                >= 0x100000 => Unit(Strings.SizeMegabytes, value / 1048576d, "0.00"),
                >= 0x400 => Unit(Strings.SizeKilobytes, value / 1024d, "0.00"),
                _ => Unit(Strings.SizeBytes, value, "0"),
            };
        }

        /// <summary>Formats a rate in bytes per second as kB/s (or MB/s when large).</summary>
        public static string Speed(long bytesPerSecond)
        {
            if (bytesPerSecond <= 0)
            {
                return Unit(Strings.SpeedKilobytes, 0, "0");
            }

            return bytesPerSecond >= 1024 * 1024
                ? Unit(Strings.SpeedMegabytes, bytesPerSecond / 1048576d, "0.00")
                : Unit(Strings.SpeedKilobytes, bytesPerSecond / 1024d, "0.#");
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

        /// <summary>The ratio to two decimals, or a dash until enough has been downloaded to compute one.</summary>
        public static string Ratio(double? value) =>
            value is { } v && double.IsFinite(v) ? v.ToString("0.00", CultureInfo.CurrentCulture) : Unknown;

        /// <summary>A percentage with one decimal, the sign placed as the regional format places it (12.5%, 12,5 %, %12,5).</summary>
        public static string Percent(double value) =>
            (value / 100).ToString("P1", CultureInfo.CurrentCulture);

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

        /// <summary>
        /// Formats a number in the user's regional format and puts it into a translated unit pattern such as "{0} MB".
        /// In a right-to-left language the number starts with a left-to-right mark, so a Latin unit stays after it
        /// ("60 kB/s", not "kB/s 60") while an Arabic or Hebrew unit still reads right to left, and the whole
        /// quantity ends with a right-to-left mark so it does not merge with the next one ("↑ 60 kB/s   ↓ 30 kB/s").
        /// </summary>
        public static string Unit(string pattern, double value, string numberFormat)
        {
            var number = value.ToString(numberFormat, CultureInfo.CurrentCulture);
            return Localization.IsRightToLeft
                ? string.Format(CultureInfo.CurrentCulture, pattern, LeftToRightMark + number) + RightToLeftMark
                : string.Format(CultureInfo.CurrentCulture, pattern, number);
        }

        /// <summary>
        /// Formats a timestamp: a 24-hour clock when forced by the setting, otherwise the system's own long time
        /// format (which already reflects the user's 12/24-hour preference and AM/PM designators).
        /// </summary>
        public static string Time(DateTimeOffset value, bool force24Hour) =>
            value.ToString(force24Hour ? "HH:mm:ss" : CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern, CultureInfo.CurrentCulture);
    }
}
