namespace RatioMaster.App.ViewModels
{
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

    /// <summary>
    /// The Log tab. Producers enqueue from any thread; the UI drains in batches so thousands of lines stay cheap.
    /// </summary>
    public sealed partial class LogViewModel : ViewModelBase
    {
        private const int MaxLines = 10_000;

        private readonly ConcurrentQueue<LogEntry> pending = new();
        private readonly List<LogEntry> all = new(1024);
        private readonly IFileDialogService? files;
        private readonly IClipboardService? clipboard;
        private readonly string torrentName;

        [ObservableProperty]
        private bool isEnabled = true;

        [ObservableProperty]
        private bool autoScroll = true;

        [ObservableProperty]
        private string filterText = string.Empty;

        public LogViewModel(string torrentName = "torrent", IFileDialogService? files = null, IClipboardService? clipboard = null)
        {
            this.torrentName = torrentName;
            this.files = files;
            this.clipboard = clipboard;
        }

        /// <summary>Raised after a batch is appended so the view can scroll to the end.</summary>
        public event EventHandler? LinesAppended;

        /// <summary>Gets the lines currently shown (all of them, or those matching the filter).</summary>
        public ObservableCollection<LogEntry> Entries { get; } = [];

        /// <summary>Called from the engine's thread.</summary>
        public void Enqueue(LogEntry entry)
        {
            if (this.IsEnabled)
            {
                this.pending.Enqueue(entry);
            }
        }

        /// <summary>Called on the UI thread by the refresh timer.</summary>
        public void Drain()
        {
            var appended = false;
            while (this.pending.TryDequeue(out var entry))
            {
                this.all.Add(entry);
                if (this.Matches(entry))
                {
                    this.Entries.Add(entry);
                    appended = true;
                }
            }

            if (this.all.Count > MaxLines)
            {
                this.all.RemoveRange(0, this.all.Count - MaxLines);
            }

            while (this.Entries.Count > MaxLines)
            {
                this.Entries.RemoveAt(0);
            }

            if (appended && this.AutoScroll)
            {
                this.LinesAppended?.Invoke(this, EventArgs.Empty);
            }
        }

        private static string Sanitize(string name)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(name) ? "torrent" : name;
        }

        partial void OnFilterTextChanged(string value) => Rebuild();

        private bool Matches(LogEntry entry) =>
            this.FilterText.Length == 0 || entry.Text.Contains(this.FilterText, StringComparison.OrdinalIgnoreCase);

        private void Rebuild()
        {
            this.Entries.Clear();
            foreach (var entry in this.all.Where(this.Matches))
            {
                this.Entries.Add(entry);
            }
        }

        [RelayCommand]
        private void Clear()
        {
            this.all.Clear();
            this.Entries.Clear();
        }

        [RelayCommand]
        private async Task CopyAllAsync()
        {
            if (this.clipboard is not null)
            {
                await this.clipboard.SetTextAsync(this.BuildText());
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (this.files is null)
            {
                return;
            }

            var path = await this.files.SaveLogFileAsync($"{Sanitize(this.torrentName)}.log");
            if (path is not null)
            {
                await File.WriteAllTextAsync(path, this.BuildText());
            }
        }

        private string BuildText()
        {
            var builder = new StringBuilder(this.all.Count * 64);
            foreach (var entry in this.all)
            {
                builder.Append('[').Append(Formatting.Time(entry.Time, LogTimeConverter.Use24HourTime)).Append("] ")
                    .AppendLine(entry.Text);
            }

            return builder.ToString();
        }
    }
}
