namespace RatioMaster.App
{
    using System;

    using Avalonia;

    internal static class Program
    {
        // The Avalonia entry point. Must not use any Avalonia type before AppMain is called.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
