using Wortlaut.Core;
using Wortlaut.UI;

namespace Wortlaut.Tests;

public class UiTextTests
{
    [Theory]
    [InlineData("Russisch (ru)", "ru")]
    [InlineData("Deutsch (de)", "de")]
    [InlineData("Englisch (en)", "en")]
    [InlineData("Automatisch erkennen", "auto")]
    [InlineData("automatisch erkennen", "auto")]
    [InlineData("", "auto")]
    [InlineData("fr", "fr")]
    [InlineData(" uk ", "uk")]
    [InlineData("Ukrainisch (uk)", "uk")]
    public void LanguageTextIsParsedToCode(string text, string expected)
    {
        Assert.Equal(expected, MainForm.ParseLanguage(text));
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("auto")]
    public void KnownLanguagesRoundTrip(string code)
    {
        Assert.Equal(code, MainForm.ParseLanguage(UiText.LanguageName(code)));
    }

    [Theory]
    [InlineData(25, "0:25")]
    [InlineData(48 * 60 + 12, "48:12")]
    [InlineData(3600 + 2 * 60 + 40, "1:02:40")]
    public void DurationIsFormattedLikeTheMockup(int seconds, string expected)
    {
        Assert.Equal(expected, UiText.FormatDuration(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void StatusBarSummaryShowsActiveSettings()
    {
        var settings = new WhisperSettings { Model = "large-v2", Device = "cuda", Language = "ru", Format = OutputFormat.Text };

        Assert.Equal("large-v2 · cuda · ru · .txt", UiText.SettingsSummary(settings));
        Assert.Equal("large-v2 · cuda · auto · .json", UiText.SettingsSummary(settings with { Language = "auto", Format = OutputFormat.Json }));
    }

    [Fact]
    public void FormatNamesContainExtensionFromTheCentralMapping()
    {
        Assert.Equal(
            ["Text (.txt)", "JSON (.json)", "Untertitel (.srt)", "WebVTT (.vtt)"],
            OutputFormats.All.Select(UiText.FormatName));
    }
}
