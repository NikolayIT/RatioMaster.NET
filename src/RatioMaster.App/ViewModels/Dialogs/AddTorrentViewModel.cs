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

/// <summary>One torrent the user confirmed adding.</summary>
public sealed record AddTorrentRequest(
    TorrentDescriptor Descriptor,
    string DisplayName,
    TorrentSettings Settings,
    bool StartImmediately);

/// <summary>A parsed .torrent shown in the Add dialog.</summary>
public sealed class ParsedTorrentViewModel(TorrentFile file)
{
    public TorrentFile File { get; } = file;

    public string Name => File.Name;

    public string InfoHash => File.InfoHashHex;

    public string Size => Formatting.Bytes(File.TotalLength);

    public string FileCount => File.Files.Count == 1 ? "1 file" : $"{File.Files.Count} files";

    public string Path => File.Path ?? string.Empty;
}

/// <summary>The Add torrent dialog: pick files, review what they are, adjust settings, add.</summary>
public sealed partial class AddTorrentViewModel : DialogViewModel<IReadOnlyList<AddTorrentRequest>>
{
    private readonly IFileDialogService _files;
    private readonly TorrentSettings _defaults;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSingle))]
    [NotifyPropertyChangedFor(nameof(HasTorrents))]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private ParsedTorrentViewModel? _selected;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _trackerUrl = string.Empty;

    [ObservableProperty]
    private bool _startImmediately = true;

    [ObservableProperty]
    private string? _error;

    public AddTorrentViewModel(
        TorrentSettingsViewModel settings,
        IFileDialogService files,
        TorrentSettings defaults,
        bool startImmediately)
    {
        Title = "Add torrent";
        Settings = settings;
        _files = files;
        _defaults = defaults;
        _startImmediately = startImmediately;
    }

    public TorrentSettingsViewModel Settings { get; }

    public ObservableCollection<ParsedTorrentViewModel> Torrents { get; } = [];

    public ObservableCollection<string> TrackerUrls { get; } = [];

    public bool HasTorrents => Torrents.Count > 0;

    public bool IsSingle => Torrents.Count == 1;

    public string Summary => Torrents.Count switch
    {
        0 => "No torrent selected yet.",
        1 => Selected?.Name ?? string.Empty,
        _ => $"{Torrents.Count} torrents; the settings below apply to all of them.",
    };

    public bool CanAdd => Torrents.Count > 0;

    /// <summary>Parses the given .torrent paths and adds them to the dialog.</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        var problems = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                var file = TorrentFile.Load(path);
                if (Torrents.Any(t => string.Equals(t.InfoHash, file.InfoHashHex, StringComparison.Ordinal)))
                {
                    continue;
                }

                Torrents.Add(new ParsedTorrentViewModel(file));
            }
            catch (TorrentFormatException ex)
            {
                problems.Add($"{System.IO.Path.GetFileName(path)}: {ex.Message}");
            }
        }

        Error = problems.Count > 0 ? string.Join(Environment.NewLine, problems) : null;

        Selected ??= Torrents.FirstOrDefault();
        if (Torrents.Count == 1 && Selected is not null)
        {
            DisplayName = Selected.Name;
            TrackerUrls.Clear();
            foreach (var url in Selected.File.AnnounceList)
            {
                TrackerUrls.Add(url);
            }

            TrackerUrl = Selected.File.Announce;
        }

        OnPropertyChanged(nameof(HasTorrents));
        OnPropertyChanged(nameof(IsSingle));
        OnPropertyChanged(nameof(Summary));
        AddCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var paths = await _files.PickTorrentFilesAsync(_defaults.ClientName is null ? null : null);
        if (paths.Count > 0)
        {
            AddFiles(paths);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        var settings = Settings.ToSettings();
        var requests = new List<AddTorrentRequest>(Torrents.Count);
        foreach (var torrent in Torrents)
        {
            var trackerUrl = IsSingle && !string.IsNullOrWhiteSpace(TrackerUrl) ? TrackerUrl : torrent.File.Announce;
            var name = IsSingle && !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : torrent.Name;
            requests.Add(new AddTorrentRequest(
                TorrentDescriptor.FromFile(torrent.File, trackerUrl),
                name,
                settings,
                StartImmediately));
        }

        Close(requests);
    }

    [RelayCommand]
    private void Cancel() => Close(null);
}
