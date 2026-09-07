namespace RatioMaster.App.Services.Abstractions
{
    /// <summary>In-app toasts (the cross-platform stand-in for the old tray balloons).</summary>
    public interface INotificationService
    {
        void ShowInformation(string title, string message);

        void ShowWarning(string title, string message);
    }
}
