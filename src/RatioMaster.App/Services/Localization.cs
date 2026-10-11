namespace RatioMaster.App.Services
{
    using System.Globalization;

    using RatioMaster.Core.Settings;

    /// <summary>
    /// Chooses the interface language once at startup, before any view is loaded: the language picked in the
    /// settings, otherwise the display language of the operating system. Changing it takes a restart, because the
    /// views read their texts when they are created.
    /// </summary>
    /// <remarks>
    /// Only the UI culture changes. Numbers, dates and times keep following the regional format of the system.
    /// </remarks>
    public static class Localization
    {
        /// <summary>Gets the display language of the operating system, as it was before the app picked its own.</summary>
        public static CultureInfo SystemCulture { get; private set; } = CultureInfo.CurrentUICulture;

        /// <summary>Gets the language the interface shows.</summary>
        public static UiLanguage Current { get; private set; } = UiLanguages.English;

        /// <summary>Gets a value indicating whether the current language is written right to left (Arabic, Hebrew).</summary>
        public static bool IsRightToLeft { get; private set; }

        /// <summary>Gets the language that "System default" stands for on this computer.</summary>
        public static UiLanguage SystemLanguage => UiLanguages.Match(SystemCulture) ?? UiLanguages.English;

        /// <summary>Makes <paramref name="preferred"/> (or the system language when it is null) the UI culture of every thread.</summary>
        public static void Apply(string? preferred)
        {
            SystemCulture = CultureInfo.CurrentUICulture;
            Current = UiLanguages.Resolve(preferred, SystemCulture);

            var culture = CultureInfo.GetCultureInfo(Current.Code);
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
            IsRightToLeft = culture.TextInfo.IsRightToLeft;
        }
    }
}
