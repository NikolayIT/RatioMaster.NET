namespace RatioMaster.Core.Tests.Localization
{
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using RatioMaster.Core.Resources;
    using RatioMaster.Core.Settings;

    /// <summary>
    /// Checks every translation against the English texts it was made from: a missing key falls back to English
    /// silently, and a broken placeholder throws when the message is formatted, so both are caught here instead.
    /// </summary>
    public partial class TranslationFilesTests
    {
        private static readonly string[] Projects = ["RatioMaster.App", "RatioMaster.Core"];

        private static readonly string[] TopLevelMenus = ["MenuFile", "MenuTorrent", "MenuAll", "MenuView", "MenuHelp"];

        public static TheoryData<string> Translated => new(UiLanguages.All.Where(l => l != UiLanguages.English).Select(l => l.Code));

        public static TheoryData<string, string> TranslationFiles
        {
            get
            {
                var data = new TheoryData<string, string>();
                foreach (var project in Projects)
                {
                    foreach (var language in UiLanguages.All.Where(l => l != UiLanguages.English))
                    {
                        data.Add(project, language.Code);
                    }
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(TranslationFiles))]
        public void HasEveryKeyOfTheEnglishTexts(string project, string code)
        {
            var english = Read(project, null);
            var translated = Read(project, code);

            Assert.Empty(english.Keys.Except(translated.Keys));
            Assert.Empty(translated.Keys.Except(english.Keys));
            Assert.All(translated, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), $"{code} {pair.Key} is empty"));
        }

        [Theory]
        [MemberData(nameof(TranslationFiles))]
        public void KeepsThePlaceholdersAndFormats(string project, string code)
        {
            var english = Read(project, null);
            foreach (var (key, value) in Read(project, code))
            {
                Assert.True(
                    Placeholders(english[key]).SequenceEqual(Placeholders(value)),
                    $"{code} {key}: \"{value}\" does not have the placeholders of \"{english[key]}\"");

                var arguments = Enumerable.Range(0, 10).Select(i => (object)i).ToArray();
                var exception = Record.Exception(() => string.Format(CultureInfo.InvariantCulture, value, arguments));
                Assert.True(exception is null, $"{code} {key}: \"{value}\" cannot be formatted: {exception?.Message}");
            }
        }

        [Theory]
        [MemberData(nameof(Translated))]
        public void GivesTheTopLevelMenusDistinctAccessKeys(string code)
        {
            var texts = Read("RatioMaster.App", code);
            var keys = TopLevelMenus.Select(menu => AccessKey(texts[menu]) ?? throw new Xunit.Sdk.XunitException($"{code} {menu} \"{texts[menu]}\" has no single access key")).ToList();
            Assert.Equal(keys.Count, keys.Select(k => char.ToUpperInvariant(k)).Distinct().Count());
        }

        [Fact]
        public void HasNoTranslationForALanguageThatIsNotListed()
        {
            foreach (var project in Projects)
            {
                var listed = UiLanguages.All.Select(l => l.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var files = Directory.GetFiles(ResourceFolder(project), "Strings.*.resx")
                    .Select(path => Path.GetFileNameWithoutExtension(path)["Strings.".Length..]);
                Assert.All(files, code => Assert.Contains(code, listed));
            }
        }

        [Theory]
        [MemberData(nameof(Translated))]
        public void LoadsTheEngineTextsFromTheSatelliteAssembly(string code)
        {
            var culture = CultureInfo.GetCultureInfo(code);
            var expected = Read("RatioMaster.Core", code)[nameof(Strings.TrackerUrlMissing)];
            Assert.Equal(expected, Strings.ResourceManager.GetString(nameof(Strings.TrackerUrlMissing), culture));
        }

        private static Dictionary<string, string> Read(string project, string? code)
        {
            var file = Path.Combine(ResourceFolder(project), code is null ? "Strings.resx" : $"Strings.{code}.resx");
            Assert.True(File.Exists(file), $"missing {file}");
            return XDocument.Load(file).Root!.Elements("data")
                .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? string.Empty, StringComparer.Ordinal);
        }

        private static string ResourceFolder(string project)
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "RatioMaster.slnx")))
                {
                    return Path.Combine(directory.FullName, project, "Resources");
                }
            }

            throw new DirectoryNotFoundException("RatioMaster.slnx not found above " + AppContext.BaseDirectory);
        }

        /// <summary>Gets the format items of a composite format string in order, such as "{0}" and "{0:0}".</summary>
        private static List<string> Placeholders(string text) =>
            PlaceholderPattern().Matches(text.Replace("{{", string.Empty, StringComparison.Ordinal)).Select(m => m.Value).Order(StringComparer.Ordinal).ToList();

        /// <summary>Gets the character after the one single underscore of a menu header, or null when there is not exactly one.</summary>
        private static char? AccessKey(string text)
        {
            var markers = AccessKeyPattern().Matches(text);
            return markers.Count == 1 ? markers[0].Groups[1].Value[0] : null;
        }

        [GeneratedRegex(@"\{\d+(?:,-?\d+)?(?::[^}]*)?\}")]
        private static partial Regex PlaceholderPattern();

        [GeneratedRegex(@"(?<!_)_([^_\s])")]
        private static partial Regex AccessKeyPattern();
    }
}
