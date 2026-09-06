using Avalonia.Controls;

namespace RatioMaster.App.Services.Abstractions;

/// <summary>Gives services access to the main window (for dialogs, pickers and the clipboard).</summary>
public interface IMainWindowProvider
{
    Window? Window { get; set; }
}
