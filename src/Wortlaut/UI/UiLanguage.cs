using System.Globalization;

namespace Wortlaut.UI;

/// <summary>Languages of the user interface.</summary>
internal enum UiLanguage
{
    German,
    Russian,
}

/// <summary>
/// Codes, names and selection of the user interface language.
/// </summary>
internal static class UiLanguages
{
    public static IReadOnlyList<UiLanguage> All { get; } = [UiLanguage.German, UiLanguage.Russian];

    /// <summary>Code stored in the settings file.</summary>
    public static string Code(UiLanguage language) => language switch
    {
        UiLanguage.Russian => "ru",
        _ => "de",
    };

    /// <summary>Name of the language in that language, as shown in the language menu.</summary>
    public static string NativeName(UiLanguage language) => language switch
    {
        UiLanguage.Russian => "Русский",
        _ => "Deutsch",
    };

    /// <summary>Parses a stored code; <c>null</c> if empty or unknown.</summary>
    public static UiLanguage? FromCode(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "de" => UiLanguage.German,
        "ru" => UiLanguage.Russian,
        _ => null,
    };

    /// <summary>Russian for a Russian Windows display language, German otherwise.</summary>
    public static UiLanguage FromCulture(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "ru" ? UiLanguage.Russian : UiLanguage.German;

    /// <summary>The language chosen in the settings, or the one matching Windows if none was chosen yet.</summary>
    public static UiLanguage Resolve(string? storedCode, CultureInfo windowsCulture) =>
        FromCode(storedCode) ?? FromCulture(windowsCulture);

    /// <summary>
    /// Switches all texts, and the culture used for .NET's own messages (e.g. file system errors), to
    /// <paramref name="language"/>. Number and date formats keep following Windows.
    /// </summary>
    public static void Apply(UiLanguage language)
    {
        UiText.Language = language;

        var culture = CultureInfo.GetCultureInfo(language == UiLanguage.Russian ? "ru-RU" : "de-DE");
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
