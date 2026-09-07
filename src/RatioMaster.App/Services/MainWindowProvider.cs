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

    /// <summary>Holds the main window so services can reach the current TopLevel.</summary>
    public sealed class MainWindowProvider : IMainWindowProvider
    {
        public Window? Window { get; set; }
    }
}
