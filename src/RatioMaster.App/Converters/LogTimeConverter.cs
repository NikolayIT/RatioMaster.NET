using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using RatioMaster.Core.Logging;
using RatioMaster.Core.Sessions;

namespace RatioMaster.App.Converters;

/// <summary>Formats a log timestamp, honouring the application's 24-hour setting.</summary>
public sealed class LogTimeConverter : IValueConverter
{
    /// <summary>Gets or sets a value indicating whether times are shown in 24-hour form, taken from the settings so every log line re-renders in the chosen format.</summary>
    public static bool Use24HourTime { get; set; }

    public static LogTimeConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTimeOffset time ? Formatting.Time(time, Use24HourTime) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
