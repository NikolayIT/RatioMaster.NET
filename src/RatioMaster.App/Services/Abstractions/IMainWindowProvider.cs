namespace RatioMaster.App.Services.Abstractions
{
    using Avalonia.Controls;

    /// <summary>Gives services access to the main window (for dialogs, pickers and the clipboard).</summary>
    public interface IMainWindowProvider
    {
        Window? Window { get; set; }
    }
}
