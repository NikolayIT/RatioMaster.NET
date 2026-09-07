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

    /// <summary>Marshals onto Avalonia's UI thread.</summary>
    public sealed class AvaloniaDispatcher : IUiDispatcher
    {
        public bool IsOnUiThread => Dispatcher.UIThread.CheckAccess();

        public void Post(Action action) => Dispatcher.UIThread.Post(action);

        public Task InvokeAsync(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();
    }
}
