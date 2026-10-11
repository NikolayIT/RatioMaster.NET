namespace RatioMaster.App
{
    using System;

    using Avalonia;
    using RatioMaster.App.Services;
    using RatioMaster.Core.Settings;

    internal static class Program
    {
        // The Avalonia entry point. Must not use any Avalonia type before AppMain is called.
        [STAThread]
        public static void Main(string[] args)
        {
            // The views read their texts as they load, so the language has to be settled first.
            Localization.Apply(new SettingsStore().Load().Language);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
