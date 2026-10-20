namespace RatioMaster.Core.Settings
{
    using System.Globalization;

    /// <summary>
    /// The languages the interface is translated into, and how the one to show is chosen: the language picked in
    /// the settings, otherwise the display language of the operating system, otherwise English.
    /// </summary>
    public static class UiLanguages
    {
        /// <summary>Gets the language of the built-in texts, used when nothing else matches.</summary>
        public static UiLanguage English { get; } = new("en", "English");

        /// <summary>Gets every interface language, sorted by native name (Latin script first).</summary>
        public static IReadOnlyList<UiLanguage> All { get; } =
        [
            new("id", "Bahasa Indonesia"),
            new("cs", "Čeština"),
            new("da", "Dansk"),
            new("de", "Deutsch"),
            new("et", "Eesti"),
            English,
            new("es", "Español"),
            new("fr", "Français"),
            new("hr", "Hrvatski"),
            new("it", "Italiano"),
            new("lt", "Lietuvių"),
            new("hu", "Magyar"),
            new("nl", "Nederlands"),
            new("nb", "Norsk bokmål"),
            new("pl", "Polski"),
            new("pt-BR", "Português (Brasil)"),
            new("pt", "Português (Portugal)"),
            new("ro", "Română"),
            new("sq", "Shqip"),
            new("sk", "Slovenčina"),
            new("sl", "Slovenščina"),
            new("sr-Latn", "Srpski (latinica)"),
            new("fi", "Suomi"),
            new("sv", "Svenska"),
            new("vi", "Tiếng Việt"),
            new("tr", "Türkçe"),
            new("el", "Ελληνικά"),
            new("bg", "Български"),
            new("ru", "Русский"),
            new("sr-Cyrl", "Српски (ћирилица)"),
            new("uk", "Українська"),
            new("he", "עברית", IsRightToLeft: true),
            new("ar", "العربية", IsRightToLeft: true),
            new("hi", "हिन्दी"),
            new("th", "ไทย"),
            new("ja", "日本語"),
            new("zh-Hans", "简体中文"),
            new("zh-Hant", "繁體中文"),
        ];

        /// <summary>
        /// Picks the language to show: <paramref name="preferred"/> when it names a translation, otherwise the
        /// closest translation of <paramref name="system"/> (the operating system's display language), otherwise English.
        /// </summary>
        public static UiLanguage Resolve(string? preferred, CultureInfo system) =>
            Find(preferred) ?? Match(system) ?? English;

        /// <summary>Gets the language stored under <paramref name="code"/>, or null when there is no such translation.</summary>
        public static UiLanguage? Find(string? code) =>
            string.IsNullOrWhiteSpace(code)
                ? null
                : All.FirstOrDefault(language => string.Equals(language.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Finds the translation for a culture: the culture itself or its nearest parent (fr-CA uses fr, zh-HK uses
        /// zh-Hant, pt-PT uses pt), then the cases a parent chain cannot settle, such as Norwegian Nynorsk or a
        /// Chinese or Serbian culture without a script.
        /// </summary>
        public static UiLanguage? Match(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);
            for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
            {
                if (Find(current.Name) is { } language)
                {
                    return language;
                }
            }

            var name = culture.Name;
            return culture.TwoLetterISOLanguageName switch
            {
                "no" or "nn" => Find("nb"),
                "iw" => Find("he"),
                "in" => Find("id"),
                "zh" => Find(Contains(name, "Hant") || Contains(name, "TW") || Contains(name, "HK") || Contains(name, "MO") ? "zh-Hant" : "zh-Hans"),

                // Serbia writes Cyrillic by default; Montenegro and an explicit Latin script use the Latin alphabet.
                "sr" => Find(Contains(name, "Latn") || Contains(name, "ME") ? "sr-Latn" : "sr-Cyrl"),
                _ => null,
            };
        }

        private static bool Contains(string name, string part) =>
            name.Split('-').Contains(part, StringComparer.OrdinalIgnoreCase);
    }
}
