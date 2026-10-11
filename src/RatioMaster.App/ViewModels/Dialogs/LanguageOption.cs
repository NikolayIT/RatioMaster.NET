namespace RatioMaster.App.ViewModels.Dialogs
{
    /// <summary>An entry of the language picker.</summary>
    /// <param name="Code">The language code, or null for the entry that follows the operating system.</param>
    /// <param name="Name">What the picker shows: the language's own name.</param>
    public sealed record LanguageOption(string? Code, string Name);
}
