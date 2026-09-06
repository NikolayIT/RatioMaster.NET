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

namespace RatioMaster.App.Services;

/// <summary>Marshals onto Avalonia's UI thread.</summary>
public sealed class AvaloniaDispatcher : IUiDispatcher
{
    public bool IsOnUiThread => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action) => Dispatcher.UIThread.Post(action);

    public Task InvokeAsync(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();
}

/// <summary>Holds the main window so services can reach the current TopLevel.</summary>
public sealed class MainWindowProvider : IMainWindowProvider
{
    public Window? Window { get; set; }
}

/// <summary>File pickers over <see cref="IStorageProvider"/>.</summary>
public sealed class FileDialogService(IMainWindowProvider windows) : IFileDialogService
{
    private static readonly FilePickerFileType TorrentFiles = new("Torrent files")
    {
        Patterns = ["*.torrent"],
        MimeTypes = ["application/x-bittorrent"],
    };

    private static readonly FilePickerFileType SessionFiles = new("RatioMaster sessions")
    {
        Patterns = ["*.session"],
    };

    private static readonly FilePickerFileType LogFiles = new("Log files")
    {
        Patterns = ["*.log", "*.txt"],
    };

    public async Task<IReadOnlyList<string>> PickTorrentFilesAsync(string? startDirectory = null)
    {
        var storage = windows.Window?.StorageProvider;
        if (storage is null)
        {
            return [];
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Add torrents",
            AllowMultiple = true,
            FileTypeFilter = [TorrentFiles, FilePickerFileTypes.All],
            SuggestedStartLocation = await FolderAsync(storage, startDirectory),
        });

        return ToPaths(files);
    }

    public async Task<string?> PickSessionFileAsync(string? startDirectory = null)
    {
        var storage = windows.Window?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load session",
            AllowMultiple = false,
            FileTypeFilter = [SessionFiles, FilePickerFileTypes.All],
            SuggestedStartLocation = await FolderAsync(storage, startDirectory),
        });

        var paths = ToPaths(files);
        return paths.Count > 0 ? paths[0] : null;
    }

    public async Task<string?> SaveSessionFileAsync(string suggestedName, string? startDirectory = null)
    {
        var storage = windows.Window?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save session",
            SuggestedFileName = suggestedName,
            DefaultExtension = "session",
            FileTypeChoices = [SessionFiles],
            SuggestedStartLocation = await FolderAsync(storage, startDirectory),
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> SaveLogFileAsync(string suggestedName)
    {
        var storage = windows.Window?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save log",
            SuggestedFileName = suggestedName,
            DefaultExtension = "log",
            FileTypeChoices = [LogFiles],
        });

        return file?.TryGetLocalPath();
    }

    private static async Task<IStorageFolder?> FolderAsync(IStorageProvider storage, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        try
        {
            return await storage.TryGetFolderFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<string> ToPaths(IReadOnlyList<IStorageFile> files)
    {
        var paths = new List<string>(files.Count);
        foreach (var file in files)
        {
            // Non-local providers (for example a cloud picker) have no path we can open.
            if (file.TryGetLocalPath() is { Length: > 0 } path)
            {
                paths.Add(path);
            }
        }

        return paths;
    }
}

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

/// <summary>Shows toasts inside the main window.</summary>
public sealed class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    public void Attach(Window window) =>
        _manager = new WindowNotificationManager(window) { Position = NotificationPosition.BottomRight, MaxItems = 4 };

    public void ShowInformation(string title, string message) => Show(title, message, NotificationType.Information);

    public void ShowWarning(string title, string message) => Show(title, message, NotificationType.Warning);

    private void Show(string title, string message, NotificationType type)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _manager?.Show(new Notification(title, message, type));
        }
        else
        {
            Dispatcher.UIThread.Post(() => _manager?.Show(new Notification(title, message, type)));
        }
    }
}
