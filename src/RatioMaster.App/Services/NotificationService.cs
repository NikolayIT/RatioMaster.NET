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

    /// <summary>Shows toasts inside the main window.</summary>
    public sealed class NotificationService : INotificationService
    {
        private WindowNotificationManager? manager;

        public void Attach(Window window) =>
            this.manager = new WindowNotificationManager(window) { Position = NotificationPosition.BottomRight, MaxItems = 4 };

        public void ShowInformation(string title, string message) => this.Show(title, message, NotificationType.Information);

        public void ShowWarning(string title, string message) => this.Show(title, message, NotificationType.Warning);

        private void Show(string title, string message, NotificationType type)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                this.manager?.Show(new Notification(title, message, type));
            }
            else
            {
                Dispatcher.UIThread.Post(() => this.manager?.Show(new Notification(title, message, type)));
            }
        }
    }
}
