namespace RatioMaster.App.ViewModels.Dialogs
{
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

    /// <summary>A parsed .torrent shown in the Add dialog.</summary>
    public sealed class ParsedTorrentViewModel(TorrentFile file)
    {
        public TorrentFile File { get; } = file;

        public string Name => this.File.Name;

        public string InfoHash => this.File.InfoHashHex;

        public string Size => Formatting.Bytes(this.File.TotalLength);

        public string FileCount => this.File.Files.Count == 1 ? "1 file" : $"{this.File.Files.Count} files";

        public string Path => this.File.Path ?? string.Empty;
    }
}
