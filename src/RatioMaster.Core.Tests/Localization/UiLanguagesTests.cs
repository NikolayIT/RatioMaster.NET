namespace RatioMaster.Core.Tests.Localization
{
    using System.Globalization;

    using RatioMaster.Core.Settings;

    public class UiLanguagesTests
    {
        [Theory]
        [InlineData("fr-FR", "fr")]
        [InlineData("fr-CA", "fr")]
        [InlineData("fr-BE", "fr")]
        [InlineData("de-CH", "de")]
        [InlineData("nl-BE", "nl")]
        [InlineData("ar-DZ", "ar")]
        [InlineData("ar-MA", "ar")]
        [InlineData("en-US", "en")]
        [InlineData("en-GB", "en")]
        [InlineData("es-419", "es")]
        [InlineData("pt-BR", "pt-BR")]
        [InlineData("pt-PT", "pt")]
        [InlineData("pt-AO", "pt")]
        [InlineData("zh-CN", "zh-Hans")]
        [InlineData("zh-SG", "zh-Hans")]
        [InlineData("zh-TW", "zh-Hant")]
        [InlineData("zh-HK", "zh-Hant")]
        [InlineData("zh", "zh-Hans")]
        [InlineData("sr-Latn-RS", "sr-Latn")]
        [InlineData("sr-Cyrl-RS", "sr-Cyrl")]
        [InlineData("sr-RS", "sr-Cyrl")]
        [InlineData("sr-ME", "sr-Latn")]
        [InlineData("nb-NO", "nb")]
        [InlineData("nn-NO", "nb")]
        [InlineData("no", "nb")]
        [InlineData("he-IL", "he")]
        [InlineData("bg-BG", "bg")]
        public void MatchesTheClosestTranslation(string culture, string expected)
        {
            Assert.Equal(expected, UiLanguages.Match(CultureInfo.GetCultureInfo(culture))?.Code);
        }

        [Theory]
        [InlineData("ko-KR")]
        [InlineData("fil-PH")]
        public void MatchesNothingForAnUntranslatedLanguage(string culture)
        {
            Assert.Null(UiLanguages.Match(CultureInfo.GetCultureInfo(culture)));
        }

        [Fact]
        public void MatchesNothingForTheInvariantCulture()
        {
            Assert.Null(UiLanguages.Match(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("de", "ja-JP", "de")]
        [InlineData("DE", "ja-JP", "de")]
        [InlineData(null, "fr-FR", "fr")]
        [InlineData("", "it-IT", "it")]
        [InlineData("xx", "fr-FR", "fr")]
        [InlineData(null, "ko-KR", "en")]
        [InlineData("en", "fr-FR", "en")]
        public void PrefersTheSavedLanguageThenTheSystemThenEnglish(string? saved, string system, string expected)
        {
            Assert.Equal(expected, UiLanguages.Resolve(saved, CultureInfo.GetCultureInfo(system)).Code);
        }

        [Fact]
        public void ResolvesTheInvariantCultureToEnglish()
        {
            Assert.Same(UiLanguages.English, UiLanguages.Resolve(null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ListsEveryLanguageOnceAndOnlyRealCultures()
        {
            var codes = UiLanguages.All.Select(l => l.Code).ToList();
            Assert.Equal(codes.Count, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains(UiLanguages.English, UiLanguages.All);
            Assert.All(codes, code => Assert.Equal(code, CultureInfo.GetCultureInfo(code).Name));
            Assert.True(UiLanguages.All.Count >= 31, "at least 30 translations plus English");
        }
    }
}
