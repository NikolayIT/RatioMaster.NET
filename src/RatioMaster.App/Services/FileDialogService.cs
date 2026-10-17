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
    using RatioMaster.App.Resources;
    using RatioMaster.App.Services.Abstractions;

    /// <summary>File pickers over <see cref="IStorageProvider"/>.</summary>
    public sealed class FileDialogService(IMainWindowProvider windows) : IFileDialogService
    {
        private static readonly FilePickerFileType TorrentFiles = new(Strings.FileTypeTorrents)
        {
            Patterns = ["*.torrent"],
            MimeTypes = ["application/x-bittorrent"],
        };

        private static readonly FilePickerFileType SessionFiles = new(Strings.FileTypeSessions)
        {
            Patterns = ["*.session"],
        };

        private static readonly FilePickerFileType LogFiles = new(Strings.FileTypeLogs)
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
                Title = Strings.PickAddTorrents,
                AllowMultiple = true,
                FileTypeFilter = [TorrentFiles, FilePickerFileTypes.All],
                SuggestedStartLocation = await FolderAsync(storage, startDirectory),
            });

            return ToPaths(files);
        }

        public async Task<string?> PickFolderAsync(string? startDirectory = null)
        {
            var storage = windows.Window?.StorageProvider;
            if (storage is null)
            {
                return null;
            }

            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Strings.PickAddTorrents,
                AllowMultiple = false,
                SuggestedStartLocation = await FolderAsync(storage, startDirectory),
            });

            return folders.Count > 0 && folders[0].TryGetLocalPath() is { Length: > 0 } path ? path : null;
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
                Title = Strings.LoadSession,
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
                Title = Strings.PickSaveSession,
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
                Title = Strings.PickSaveLog,
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
}
