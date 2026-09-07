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

    /// <summary>One torrent the user confirmed adding.</summary>
    public sealed record AddTorrentRequest(
        TorrentDescriptor Descriptor,
        string DisplayName,
        TorrentSettings Settings,
        bool StartImmediately);
}
