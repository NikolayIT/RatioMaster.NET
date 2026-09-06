using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Converters;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.Core.Logging;

namespace RatioMaster.App.ViewModels;

/// <summary>
/// The Log tab. Producers enqueue from any thread; the UI drains in batches so thousands of lines stay cheap.
/// </summary>
public sealed partial class LogViewModel : ViewModelBase
{
    private const int MaxLines = 10_000;

    private readonly ConcurrentQueue<LogEntry> _pending = new();
    private readonly List<LogEntry> _all = new(1024);
    private readonly IFileDialogService? _files;
    private readonly IClipboardService? _clipboard;
    private readonly string _torrentName;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public LogViewModel(string torrentName = "torrent", IFileDialogService? files = null, IClipboardService? clipboard = null)
    {
        _torrentName = torrentName;
        _files = files;
        _clipboard = clipboard;
    }

    /// <summary>The lines currently shown (all of them, or those matching the filter).</summary>
    public ObservableCollection<LogEntry> Entries { get; } = [];

    /// <summary>Raised after a batch is appended so the view can scroll to the end.</summary>
    public event EventHandler? LinesAppended;

    /// <summary>Called from the engine's thread.</summary>
    public void Enqueue(LogEntry entry)
    {
        if (IsEnabled)
        {
            _pending.Enqueue(entry);
        }
    }

    /// <summary>Called on the UI thread by the refresh timer.</summary>
    public void Drain()
    {
        var appended = false;
        while (_pending.TryDequeue(out var entry))
        {
            _all.Add(entry);
            if (Matches(entry))
            {
                Entries.Add(entry);
                appended = true;
            }
        }

        if (_all.Count > MaxLines)
        {
            _all.RemoveRange(0, _all.Count - MaxLines);
        }

        while (Entries.Count > MaxLines)
        {
            Entries.RemoveAt(0);
        }

        if (appended && AutoScroll)
        {
            LinesAppended?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnFilterTextChanged(string value) => Rebuild();

    private bool Matches(LogEntry entry) =>
        FilterText.Length == 0 || entry.Text.Contains(FilterText, StringComparison.OrdinalIgnoreCase);

    private void Rebuild()
    {
        Entries.Clear();
        foreach (var entry in _all.Where(Matches))
        {
            Entries.Add(entry);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _all.Clear();
        Entries.Clear();
    }

    [RelayCommand]
    private async Task CopyAllAsync()
    {
        if (_clipboard is not null)
        {
            await _clipboard.SetTextAsync(BuildText());
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_files is null)
        {
            return;
        }

        var path = await _files.SaveLogFileAsync($"{Sanitize(_torrentName)}.log");
        if (path is not null)
        {
            await File.WriteAllTextAsync(path, BuildText());
        }
    }

    private string BuildText()
    {
        var builder = new StringBuilder(_all.Count * 64);
        foreach (var entry in _all)
        {
            builder.Append('[').Append(Formatting.Time(entry.Time, LogTimeConverter.Use24HourTime)).Append("] ")
                .AppendLine(entry.Text);
        }

        return builder.ToString();
    }

    private static string Sanitize(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "torrent" : name;
    }
}
