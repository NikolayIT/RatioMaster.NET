namespace RatioMaster.App.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Platform.Storage;
    using Avalonia.Threading;
    using RatioMaster.App.Services.Abstractions;

    /// <summary>Opens links and reveals files with the desktop's default handler.</summary>
    public sealed class UrlLauncher(IMainWindowProvider windows) : IUrlLauncher
    {
        public async Task OpenUrlAsync(string url)
        {
            var launcher = windows.Window?.Launcher;
            if (launcher is not null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                await launcher.LaunchUriAsync(uri);
            }
        }

        public async Task RevealFileAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var launcher = windows.Window?.Launcher;
            if (launcher is not null)
            {
                await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(directory));
            }
        }
    }
}
