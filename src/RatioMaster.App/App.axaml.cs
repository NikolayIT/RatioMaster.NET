namespace RatioMaster.App
{
    using System;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using Microsoft.Extensions.DependencyInjection;
    using RatioMaster.App.Services;
    using RatioMaster.App.Services.Abstractions;
    using RatioMaster.App.ViewModels;
    using RatioMaster.App.Views;
    using RatioMaster.Core.Clients;
    using RatioMaster.Core.MemoryScan;
    using RatioMaster.Core.Networking;
    using RatioMaster.Core.Sessions;
    using RatioMaster.Core.Settings;
    using RatioMaster.Core.Tracker;
    using RatioMaster.Core.Updates;

    public partial class App : Application
    {
        private IServiceProvider? services;
        private MainWindowViewModel? mainViewModel;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Hiding the last window must not quit the app (close-to-tray).
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                this.services = BuildServices(out var catalogWarning);
                var settingsStore = this.services.GetRequiredService<ISettingsStore>();
                ImportLegacySettingsOnce(settingsStore);

                var viewModel = this.services.GetRequiredService<MainWindowViewModel>();
                this.mainViewModel = viewModel;
                this.DataContext = viewModel;
                ThemeApplier.Apply(viewModel.Settings.Theme);

                var window = new MainWindow { DataContext = viewModel };
                this.services.GetRequiredService<IMainWindowProvider>().Window = window;
                var notifications = (NotificationService)this.services.GetRequiredService<INotificationService>();
                notifications.Attach(window);
                if (catalogWarning is not null)
                {
                    // A broken user clients.json must not stop the app; say so once the window can show it.
                    window.Opened += (_, _) => notifications.ShowWarning("Custom clients.json ignored", catalogWarning);
                }

                viewModel.ExitRequested += async (_, _) => await this.ShutdownAsync(desktop);
                viewModel.RestoreRequested += (_, _) => window.RestoreFromTray();
                desktop.ShutdownRequested += async (_, e) =>
                {
                    e.Cancel = true;
                    await this.ShutdownAsync(desktop);
                };

                // macOS: clicking the Dock icon while the window is hidden in the menu bar brings it back.
                if (this.TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)
                {
                    activatable.Activated += (_, e) =>
                    {
                        if (e.Kind == ActivationKind.Reopen)
                        {
                            window.RestoreFromTray();
                        }
                    };
                }

                desktop.MainWindow = window;
                if (viewModel.Settings.StartMinimized)
                {
                    window.WindowState = WindowState.Minimized;
                }

                _ = viewModel.InitializeAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>On the first run, carry over the settings RatioMaster.NET 0.43 kept in the registry.</summary>
        private static void ImportLegacySettingsOnce(ISettingsStore store)
        {
            if (!OperatingSystem.IsWindows() || System.IO.File.Exists(store.FilePath))
            {
                return;
            }

            try
            {
                using var reader = new WindowsLegacyRegistryReader();
                if (LegacyRegistrySettingsImporter.TryImport(reader, store.Load()) is { } imported)
                {
                    store.Save(imported);
                }
            }
            catch (Exception)
            {
                // A failed import must never stop the app from starting.
            }
        }

        private static ServiceProvider BuildServices(out string? catalogWarning)
        {
            var services = new ServiceCollection();

            AppPaths.EnsureConfigDirectory();
            var catalog = ClientProfileCatalog.Load(AppPaths.UserClientsFile, out catalogWarning);

            services.AddSingleton(catalog);
            services.AddSingleton<ISettingsStore>(_ => new SettingsStore());
            services.AddSingleton<ILocalIpProvider>(LocalIpProvider.Instance);
            services.AddSingleton<ITrackerClient>(_ => new TrackerClient());
            services.AddSingleton(_ => new ClientValueScanner());
            services.AddSingleton(sp => new TorrentSessionFactory(
                sp.GetRequiredService<ClientProfileCatalog>(),
                sp.GetRequiredService<ITrackerClient>(),
                sp.GetRequiredService<ClientValueScanner>(),
                sp.GetRequiredService<ILocalIpProvider>()));
            services.AddSingleton(_ => new UpdateChecker());

            services.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            services.AddSingleton<IUiDispatcher, AvaloniaDispatcher>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IUrlLauncher, UrlLauncher>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IDialogService, DialogService>();

            services.AddSingleton<MainWindowViewModel>();

            return services.BuildServiceProvider();
        }

        private async Task ShutdownAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (this.mainViewModel is { } viewModel)
            {
                try
                {
                    await viewModel.ShutdownAsync(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    // Quitting must not be blocked by a slow tracker.
                }
            }

            desktop.Shutdown();
        }
    }
}
