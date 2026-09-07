namespace RatioMaster.App.Services
{
    using Avalonia;
    using Avalonia.Styling;
    using RatioMaster.Core.Settings;

    /// <summary>Applies the chosen theme to the running application.</summary>
    public static class ThemeApplier
    {
        public static void Apply(AppTheme theme)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = theme switch
                {
                    AppTheme.Light => ThemeVariant.Light,
                    AppTheme.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Default,
                };
            }
        }
    }
}
