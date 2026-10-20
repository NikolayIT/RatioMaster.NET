namespace RatioMaster.Core.Settings
{
    /// <summary>A language the interface is translated into.</summary>
    /// <param name="Code">The culture name the translation is stored under, such as "fr", "pt-BR" or "zh-Hant".</param>
    /// <param name="NativeName">The language's name in that language, as the language picker shows it.</param>
    /// <param name="IsRightToLeft">
    /// Whether the language is written right to left. It is kept here because the culture cannot tell in .NET's
    /// globalization-invariant mode.
    /// </param>
    public sealed record UiLanguage(string Code, string NativeName, bool IsRightToLeft = false);
}
