using System.Collections.Generic;
using System.Threading.Tasks;

namespace RatioMaster.App.Services.Abstractions;

/// <summary>File open/save dialogs over Avalonia's storage provider.</summary>
public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickTorrentFilesAsync(string? startDirectory = null);

    Task<string?> PickSessionFileAsync(string? startDirectory = null);

    Task<string?> SaveSessionFileAsync(string suggestedName, string? startDirectory = null);

    Task<string?> SaveLogFileAsync(string suggestedName);
}
