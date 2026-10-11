namespace RatioMaster.Core.Tests.Localization
{
    using System.Globalization;
    using System.Runtime.CompilerServices;

    using RatioMaster.Core.Resources;

    /// <summary>
    /// Several tests check the wording of engine messages, so the engine speaks English here whatever the display
    /// language of the machine running the tests. Only the message language is pinned; number formats are not.
    /// </summary>
    internal static class EnglishMessages
    {
        [ModuleInitializer]
        internal static void Pin() => Strings.Culture = CultureInfo.InvariantCulture;
    }
}
