using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using RatioMaster.Core.Logging;
using RatioMaster.Core.Sessions;

namespace RatioMaster.App.Converters;

/// <summary>Converters used by the views. Exposed as static instances for compiled bindings.</summary>
public static class Converters
{
    public static FuncValueConverter<long, string> Bytes { get; } = new(Formatting.Bytes);

    public static FuncValueConverter<long, string> Speed { get; } = new(Formatting.Speed);

    public static FuncValueConverter<TimeSpan?, string> Duration { get; } = new(Formatting.Duration);

    public static FuncValueConverter<double?, string> Ratio { get; } = new(Formatting.Ratio);

    public static FuncValueConverter<double, string> Percent { get; } = new(Formatting.Percent);

    public static FuncValueConverter<int?, string> Count { get; } = new(Formatting.Count);

    public static FuncValueConverter<string?, string> Host { get; } = new(Formatting.Host);

    /// <summary>Gets the converter that shows an em dash instead of an empty value.</summary>
    public static FuncValueConverter<string?, string> OrDash { get; } =
        new(value => string.IsNullOrWhiteSpace(value) ? Formatting.Unknown : value);

    /// <summary>Gets the brush for a session state, resolved from the current theme.</summary>
    public static FuncValueConverter<TorrentSessionState, object?> StatusBrush { get; } =
        new(state => Resource(state switch
        {
            TorrentSessionState.Downloading => "StatusDownloadingBrush",
            TorrentSessionState.Seeding => "StatusSeedingBrush",
            TorrentSessionState.Starting or TorrentSessionState.Updating => "StatusUpdatingBrush",
            TorrentSessionState.Stopping => "StatusStoppingBrush",
            TorrentSessionState.Stopped => "StatusStoppedBrush",
            TorrentSessionState.Error => "StatusErrorBrush",
            _ => "StatusIdleBrush",
        }));

    /// <summary>Gets the icon geometry for a session state.</summary>
    public static FuncValueConverter<TorrentSessionState, object?> StatusIcon { get; } =
        new(state => Resource(state switch
        {
            TorrentSessionState.Downloading => "IconDownloading",
            TorrentSessionState.Seeding => "IconSeeding",
            TorrentSessionState.Starting or TorrentSessionState.Updating => "IconUpdate",
            TorrentSessionState.Error => "IconError",
            _ => "IconCircle",
        }));

    /// <summary>Gets the brush for a log level.</summary>
    public static FuncValueConverter<LogLevel, object?> LogBrush { get; } =
        new(level => Resource(level switch
        {
            LogLevel.Warning => "LogWarningBrush",
            LogLevel.Error => "LogErrorBrush",
            _ => "LogInfoBrush",
        }));

    private static object? Resource(string key) =>
        Avalonia.Application.Current?.TryFindResource(key, Avalonia.Application.Current.ActualThemeVariant, out var value) == true
            ? value
            : null;
}
