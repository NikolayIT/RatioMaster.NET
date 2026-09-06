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

namespace RatioMaster.App;

public partial class App : Application
{
    private IServiceProvider? _services;
    private MainWindowViewModel? _mainViewModel;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Hiding the last window must not quit the app (close-to-tray).
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _services = BuildServices();
            var settingsStore = _services.GetRequiredService<ISettingsStore>();
            ImportLegacySettingsOnce(settingsStore);

            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            _mainViewModel = viewModel;
            DataContext = viewModel;
            ThemeApplier.Apply(viewModel.Settings.Theme);

            var window = new MainWindow { DataContext = viewModel };
            _services.GetRequiredService<IMainWindowProvider>().Window = window;
            ((NotificationService)_services.GetRequiredService<INotificationService>()).Attach(window);

            viewModel.ExitRequested += async (_, _) => await ShutdownAsync(desktop);
            viewModel.RestoreRequested += (_, _) => window.RestoreFromTray();
            desktop.ShutdownRequested += async (_, e) =>
            {
                e.Cancel = true;
                await ShutdownAsync(desktop);
            };

            desktop.MainWindow = window;
            if (viewModel.Settings.StartMinimized)
            {
                window.WindowState = WindowState.Minimized;
            }

            _ = viewModel.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ShutdownAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainViewModel is { } viewModel)
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

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        AppPaths.EnsureConfigDirectory();
        var catalog = ClientProfileCatalog.Load(AppPaths.UserClientsFile);

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
}
