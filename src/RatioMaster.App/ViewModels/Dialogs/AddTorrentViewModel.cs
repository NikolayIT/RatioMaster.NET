using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Converters;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.Core.Sessions;
using RatioMaster.Core.Torrents;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>The Add torrent dialog: pick files, review what they are, adjust settings, add.</summary>
public sealed partial class AddTorrentViewModel : DialogViewModel<IReadOnlyList<AddTorrentRequest>>
{
    private readonly IFileDialogService files;
    private readonly TorrentSettings defaults;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSingle))]
    [NotifyPropertyChangedFor(nameof(HasTorrents))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private ParsedTorrentViewModel? selected;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackerWarning))]
    private string trackerUrl = string.Empty;

    [ObservableProperty]
    private bool startImmediately = true;

    [ObservableProperty]
    private string? error;

    public AddTorrentViewModel(
        TorrentSettingsViewModel settings,
        IFileDialogService files,
        TorrentSettings defaults,
        bool startImmediately)
    {
        this.Title = "Add torrent";
        this.Settings = settings;
        this.files = files;
        this.defaults = defaults;
        this.startImmediately = startImmediately;
    }

    public TorrentSettingsViewModel Settings { get; }

    public ObservableCollection<ParsedTorrentViewModel> Torrents { get; } = [];

    public ObservableCollection<string> TrackerUrls { get; } = [];

    public bool HasTorrents => this.Torrents.Count > 0;

    public bool IsSingle => this.Torrents.Count == 1;

    public string Summary => this.Torrents.Count switch
    {
        0 => "No torrent selected yet.",
        1 => this.Selected?.Name ?? string.Empty,
        _ => $"{this.Torrents.Count} torrents; the settings below apply to all of them.",
    };

    public bool CanAdd => this.Torrents.Count > 0;

    /// <summary>
    /// Why the chosen tracker cannot be announced to (no tracker, udp://, malformed), or null when it is fine.
    /// The torrent can still be added; it just needs another tracker before it can start.
    /// </summary>
    public string? TrackerWarning
    {
        get
        {
            if (this.IsSingle)
            {
                return Core.Tracker.TrackerUrl.Validate(this.TrackerUrl);
            }

            var unsupported = this.Torrents.Count(t => !Core.Tracker.TrackerUrl.IsValid(t.File.Announce));
            return unsupported switch
            {
                0 => null,
                1 => "One of these torrents has no http or https tracker and will not be able to start.",
                _ => $"{unsupported} of these torrents have no http or https tracker and will not be able to start.",
            };
        }
    }

    /// <summary>Parses the given .torrent paths and adds them to the dialog.</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        var problems = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                var file = TorrentFile.Load(path);
                if (this.Torrents.Any(t => string.Equals(t.InfoHash, file.InfoHashHex, StringComparison.Ordinal)))
                {
                    continue;
                }

                this.Torrents.Add(new ParsedTorrentViewModel(file));
            }
            catch (TorrentFormatException ex)
            {
                problems.Add($"{System.IO.Path.GetFileName(path)}: {ex.Message}");
            }
        }

        this.Error = problems.Count > 0 ? string.Join(Environment.NewLine, problems) : null;

        this.Selected ??= this.Torrents.FirstOrDefault();
        if (this.Torrents.Count == 1 && this.Selected is not null)
        {
            this.DisplayName = this.Selected.Name;
            this.TrackerUrls.Clear();
            foreach (var url in this.Selected.File.AnnounceList)
            {
                this.TrackerUrls.Add(url);
            }

            this.TrackerUrl = this.Selected.File.Announce;
        }

        this.OnPropertyChanged(nameof(this.HasTorrents));
        this.OnPropertyChanged(nameof(this.IsSingle));
        this.OnPropertyChanged(nameof(this.Summary));
        this.OnPropertyChanged(nameof(this.TrackerWarning));
        this.AddCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var paths = await this.files.PickTorrentFilesAsync();
        if (paths.Count > 0)
        {
            this.AddFiles(paths);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        var settings = this.Settings.ToSettings();
        var requests = new List<AddTorrentRequest>(this.Torrents.Count);
        foreach (var torrent in this.Torrents)
        {
            var trackerUrl = this.IsSingle && !string.IsNullOrWhiteSpace(this.TrackerUrl) ? this.TrackerUrl.Trim() : torrent.File.Announce;
            var name = this.IsSingle && !string.IsNullOrWhiteSpace(this.DisplayName) ? this.DisplayName : torrent.Name;
            requests.Add(new AddTorrentRequest(
                TorrentDescriptor.FromFile(torrent.File, trackerUrl),
                name,
                settings,
                this.StartImmediately));
        }

        this.Close(requests);
    }

    [RelayCommand]
    private void Cancel() => this.Close(null);
}
