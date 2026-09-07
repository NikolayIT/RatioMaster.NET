namespace RatioMaster.App.Services
{
    using System.Threading.Tasks;

    using Avalonia.Input.Platform;
    using RatioMaster.App.Services.Abstractions;

    /// <summary>Writes text to the system clipboard through the main window's clipboard.</summary>
    public sealed class ClipboardService(IMainWindowProvider windows) : IClipboardService
    {
        public async Task SetTextAsync(string text)
        {
            var clipboard = windows.Window?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
