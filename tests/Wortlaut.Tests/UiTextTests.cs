using System.Globalization;
using System.Reflection;
using Wortlaut.Core;
using Wortlaut.UI;

namespace Wortlaut.Tests;

/// <summary>
/// <see cref="UiText.Language"/> is global, so all tests that read UI texts run in this collection, one at a time.
/// </summary>
[CollectionDefinition(nameof(UiTextCollection), DisableParallelization = true)]
public sealed class UiTextCollection;

/// <summary>Each test starts in German; tests switch to Russian where needed.</summary>
[Collection(nameof(UiTextCollection))]
public sealed class UiTextTests : IDisposable
{
    // Texts that are the same in all languages on purpose.
    private static readonly HashSet<string> LanguageNeutralTexts = [nameof(UiText.SettingsGroup)];

    // Texts that are the same in German and English on purpose (international words).
    private static readonly HashSet<string> SameInGermanAndEnglish =
    [
        nameof(UiText.SettingsGroup),
        nameof(UiText.FormatLabel),
        nameof(UiText.ColumnStatus),
        nameof(UiText.ModelColumnStatus),
        nameof(UiText.SetupVersionLabel),
        nameof(UiText.SetupDownloadLabel),
    ];

    public UiTextTests() => UiText.Language = UiLanguage.German;

    public void Dispose() => UiText.Language = UiLanguage.German;

    public static TheoryData<string> TextProperties() =>
        [.. typeof(UiText)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.Name)];

    [Theory]
    [MemberData(nameof(TextProperties))]
    public void EveryTextExistsInAllLanguages(string propertyName)
    {
        var property = typeof(UiText).GetProperty(propertyName)!;

        UiText.Language = UiLanguage.German;
        var german = (string)property.GetValue(null)!;
        UiText.Language = UiLanguage.Russian;
        var russian = (string)property.GetValue(null)!;
        UiText.Language = UiLanguage.English;
        var english = (string)property.GetValue(null)!;

        Assert.False(string.IsNullOrWhiteSpace(german));
        Assert.False(string.IsNullOrWhiteSpace(russian));
        Assert.False(string.IsNullOrWhiteSpace(english));
        Assert.False(ContainsCyrillic(german), $"German text of {propertyName} contains Cyrillic: {german}");
        if (!LanguageNeutralTexts.Contains(propertyName))
        {
            // Catches a German text pasted into the Russian slot.
            Assert.True(ContainsCyrillic(russian), $"Russian text of {propertyName} is not Russian: {russian}");
        }

        // Catches German or Russian text pasted into the English slot.
        Assert.False(ContainsCyrillic(english), $"English text of {propertyName} contains Cyrillic: {english}");
        Assert.False(english.Any(c => "äöüÄÖÜß„".Contains(c)), $"English text of {propertyName} looks German: {english}");
        if (!SameInGermanAndEnglish.Contains(propertyName))
            Assert.NotEqual(german, english);
    }

    [Fact]
    public void ThereAreTextsToCheck()
    {
        Assert.True(TextProperties().Count > 50);
    }

    [Theory]
    [InlineData(1, "1 Datei in D:\\Videos")]
    [InlineData(2, "2 Dateien in D:\\Videos")]
    [InlineData(0, "0 Dateien in D:\\Videos")]
    public void GermanFileCountUsesSingularAndPlural(int count, string expected)
    {
        Assert.Equal(expected, UiText.LogFolderLoaded(count, @"D:\Videos"));
    }

    [Theory]
    [InlineData(1, "1 файл в папке D:\\Videos")]
    [InlineData(3, "3 файла в папке D:\\Videos")]
    [InlineData(5, "5 файлов в папке D:\\Videos")]
    [InlineData(12, "12 файлов в папке D:\\Videos")]
    [InlineData(21, "21 файл в папке D:\\Videos")]
    [InlineData(50, "50 файлов в папке D:\\Videos")]
    public void RussianFileCountUsesTheRightPluralForm(int count, string expected)
    {
        UiText.Language = UiLanguage.Russian;

        Assert.Equal(expected, UiText.LogFolderLoaded(count, @"D:\Videos"));
    }

    [Theory]
    [InlineData(0, "many")]
    [InlineData(1, "one")]
    [InlineData(2, "few")]
    [InlineData(4, "few")]
    [InlineData(5, "many")]
    [InlineData(11, "many")]
    [InlineData(12, "many")]
    [InlineData(14, "many")]
    [InlineData(21, "one")]
    [InlineData(22, "few")]
    [InlineData(25, "many")]
    [InlineData(101, "one")]
    [InlineData(111, "many")]
    [InlineData(1004, "few")]
    public void RussianPluralFollowsTheGrammarRules(int count, string expected)
    {
        Assert.Equal(expected, UiText.RussianPlural(count, "one", "few", "many"));
    }

    [Fact]
    public void TextsWithValuesAreTranslated()
    {
        UiText.Language = UiLanguage.Russian;

        Assert.Equal("0 из 3", UiText.CountOf(0, 3));
        Assert.Equal("обработка (45 %)", UiText.RowRunning(0.45));
        Assert.Equal("Русский (ru)", UiText.LanguageName("ru"));
        Assert.Equal("Определить автоматически", UiText.LanguageName("auto"));
        Assert.Equal("fr", UiText.LanguageName("fr"));
        Assert.StartsWith("Файлы типа «.mkv» не поддерживаются.", UiText.MediaNotSupported(".mkv"));
    }

    [Fact]
    public void ResultTextsAreTranslated()
    {
        var crashed = new TranscriptionResult(JobOutcome.Completed, @"D:\a.mp4", @"D:\a.txt", TimeSpan.FromSeconds(90)) { ExitCode = -1073740791 };
        var failed = new TranscriptionResult(JobOutcome.Failed, @"D:\a.mp4", @"D:\a.txt", TimeSpan.Zero)
        {
            Error = JobError.ProcessFailed,
            ExitCode = 1,
            Detail = "CUDA out of memory",
        };

        Assert.StartsWith("Fertig nach 1:30: D:\\a.txt. Hinweis:", UiText.LogResult(crashed));
        Assert.Equal("Fehler: faster-whisper wurde mit Code 1 beendet. Letzte Meldung: CUDA out of memory", UiText.ErrorText(failed));

        UiText.Language = UiLanguage.Russian;
        Assert.StartsWith("Готово за 1:30: D:\\a.txt. Примечание:", UiText.LogResult(crashed));
        Assert.Contains("(код -1073740791 (0xC0000409)", UiText.LogResult(crashed));
        Assert.Equal("Ошибка: faster-whisper завершился с кодом 1. Последнее сообщение: CUDA out of memory", UiText.ErrorText(failed));
    }

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
    [InlineData("Русский (ru)", "ru")]
    [InlineData("Немецкий (de)", "de")]
    [InlineData("Определить автоматически", "auto")]
    [InlineData("Украинский (uk)", "uk")]
    [InlineData("fr", "fr")]
    public void RussianLanguageTextIsParsedToCode(string text, string expected)
    {
        UiText.Language = UiLanguage.Russian;

        Assert.Equal(expected, MainForm.ParseLanguage(text));
    }

    [Theory]
    [InlineData("de", "ru")]
    [InlineData("de", "de")]
    [InlineData("de", "en")]
    [InlineData("de", "auto")]
    [InlineData("ru", "ru")]
    [InlineData("ru", "de")]
    [InlineData("ru", "en")]
    [InlineData("en", "ru")]
    [InlineData("en", "de")]
    [InlineData("en", "en")]
    [InlineData("en", "auto")]
    [InlineData("ru", "auto")]
    public void KnownLanguagesRoundTrip(string uiLanguage, string code)
    {
        UiText.Language = UiLanguages.FromCode(uiLanguage)!.Value;

        Assert.Equal(code, MainForm.ParseLanguage(UiText.LanguageName(code)));
    }

    [Fact]
    public void FormatNamesContainExtensionFromTheCentralMapping()
    {
        Assert.Equal(
            ["Text (.txt)", "JSON (.json)", "Untertitel (.srt)", "WebVTT (.vtt)"],
            OutputFormats.All.Select(UiText.FormatName));

        UiText.Language = UiLanguage.Russian;
        Assert.Equal(
            ["Текст (.txt)", "JSON (.json)", "Субтитры (.srt)", "WebVTT (.vtt)"],
            OutputFormats.All.Select(UiText.FormatName));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("ru")]
    public void StatusBarSummaryUsesLanguageNeutralCodes(string uiLanguage)
    {
        UiText.Language = UiLanguages.FromCode(uiLanguage)!.Value;
        var settings = new WhisperSettings { Model = "large-v2", Device = "cuda", Language = "ru", Format = OutputFormat.Text };

        Assert.Equal("large-v2 · cuda · ru · .txt", UiText.SettingsSummary(settings));
        Assert.Equal("large-v2 · cuda · auto · .json", UiText.SettingsSummary(settings with { Language = "auto", Format = OutputFormat.Json }));
    }

    [Theory]
    [InlineData(25, "0:25")]
    [InlineData(48 * 60 + 12, "48:12")]
    [InlineData(3600 + 2 * 60 + 40, "1:02:40")]
    public void DurationIsFormattedLikeTheMockup(int seconds, string expected)
    {
        Assert.Equal(expected, UiText.FormatDuration(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(-1073740791, "-1073740791 (0xC0000409)")]
    [InlineData(null, "?")]
    public void ExitCodesAreShownWithHexForWindowsStatusCodes(int? exitCode, string expected)
    {
        Assert.Equal(expected, UiText.FormatExitCode(exitCode));
    }

    [Theory]
    [InlineData(@"""D:\Videos\Лекция 12.mp4""", @"D:\Videos\Лекция 12.mp4")] // Explorer "Copy as path"
    [InlineData(@"  D:\Videos\Лекция 12.mp4  ", @"D:\Videos\Лекция 12.mp4")]
    [InlineData(@" ""D:\Videos"" ", @"D:\Videos")]
    [InlineData("", "")]
    public void PastedPathsAreCleaned(string input, string expected)
    {
        Assert.Equal(expected, PathInput.Clean(input));
    }

    // ----- Choosing the UI language -----

    [Theory]
    [InlineData("", "ru-RU", "ru")]
    [InlineData("", "de-DE", "de")]
    [InlineData("", "en-US", "de")]
    [InlineData("", "uk-UA", "de")]
    [InlineData("de", "ru-RU", "de")]
    [InlineData("ru", "de-DE", "ru")]
    [InlineData("RU", "en-US", "ru")]
    [InlineData("en", "de-DE", "en")]
    [InlineData("EN", "ru-RU", "en")]
    [InlineData("fr", "ru-RU", "ru")]
    public void StoredChoiceWinsOverWindowsLanguage(string storedCode, string windowsCulture, string expected)
    {
        var resolved = UiLanguages.Resolve(storedCode, CultureInfo.GetCultureInfo(windowsCulture));

        Assert.Equal(expected, UiLanguages.Code(resolved));
    }

    [Fact]
    public void LanguageCodesRoundTrip()
    {
        foreach (var language in UiLanguages.All)
            Assert.Equal(language, UiLanguages.FromCode(UiLanguages.Code(language)));

        Assert.Null(UiLanguages.FromCode(null));
        Assert.Null(UiLanguages.FromCode("fr"));
    }

    [Fact]
    public void LanguagesAreListedInTheirOwnLanguage()
    {
        Assert.Equal(["Deutsch", "English", "Русский"], UiLanguages.All.Select(UiLanguages.NativeName));
    }

    [Theory]
    [InlineData(1, "1 file in D:\\Videos")]
    [InlineData(2, "2 files in D:\\Videos")]
    public void EnglishFileCountUsesSingularAndPlural(int count, string expected)
    {
        UiText.Language = UiLanguage.English;

        Assert.Equal(expected, UiText.LogFolderLoaded(count, @"D:\Videos"));
    }

    [Theory]
    [InlineData("de", "1,36 GB")]
    [InlineData("ru", "1,36 ГБ")]
    [InlineData("en", "1.36 GB")]
    public void SizesUseTheNumberFormatOfTheLanguage(string code, string expected)
    {
        UiText.Language = UiLanguages.FromCode(code)!.Value;

        Assert.Equal(expected, UiText.FormatSize(1_460_000_000));
    }

    [Fact]
    public void EnglishLanguageNamesAndFormatsAreTranslated()
    {
        UiText.Language = UiLanguage.English;

        Assert.Equal("Russian (ru)", UiText.LanguageName("ru"));
        Assert.Equal("Detect automatically", UiText.LanguageName(WhisperSettings.AutoLanguage));
        Assert.Equal(["Text (.txt)", "JSON (.json)", "Subtitles (.srt)", "WebVTT (.vtt)"], OutputFormats.All.Select(UiText.FormatName));
        Assert.Equal("ru", MainForm.ParseLanguage("Russian (ru)"));
        Assert.Equal("auto", MainForm.ParseLanguage("detect automatically"));
    }

    [Fact]
    public void ApplySwitchesTextsAndUiCulture()
    {
        var previousDefault = CultureInfo.DefaultThreadCurrentUICulture;
        var previousCurrent = CultureInfo.CurrentUICulture;
        try
        {
            UiLanguages.Apply(UiLanguage.Russian);

            Assert.Equal(UiLanguage.Russian, UiText.Language);
            Assert.Equal("ru-RU", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("Расшифровать", UiText.Transcribe);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentUICulture = previousDefault;
            CultureInfo.CurrentUICulture = previousCurrent;
        }
    }

    [Fact]
    public void GpuAndCudaTextsExistInBothLanguagesForEveryCase()
    {
        var verdicts = Enum.GetValues<Core.Gpu.GpuVerdict>();
        var problems = Enum.GetValues<Core.Gpu.CudaProblem>().Where(p => p != Core.Gpu.CudaProblem.None);

        foreach (var language in UiLanguages.All)
        {
            UiText.Language = language;
            var texts = verdicts.SelectMany(v => new[]
                {
                    UiText.GpuHeadline(v),
                    UiText.GpuBadge(v),
                    UiText.GpuExplanation(Core.Gpu.GpuAdvisor.Recommend(v switch
                    {
                        Core.Gpu.GpuVerdict.Cuda => new Core.Gpu.GpuCheckResult(1, []),
                        Core.Gpu.GpuVerdict.CudaLowMemory => new Core.Gpu.GpuCheckResult(1, [new Core.Gpu.GpuInfo("x", 2048)]),
                        Core.Gpu.GpuVerdict.Cpu => new Core.Gpu.GpuCheckResult(0, []),
                        _ => new Core.Gpu.GpuCheckResult(null, []),
                    })),
                })
                .Concat(problems.SelectMany(p => new[] { UiText.CudaProblemShort(p), UiText.CudaProblemMessage(p) }))
                .ToList();

            Assert.All(texts, text => Assert.Equal(language == UiLanguage.Russian, ContainsCyrillic(text)));
            Assert.Equal(texts.Count, texts.Distinct().Count()); // no case falls back to another case's text
        }
    }

    [Theory]
    [InlineData(4096, "NVIDIA GeForce GTX 1650 \u00B7 4 GB Grafikspeicher")]
    [InlineData(6144, "NVIDIA GeForce GTX 1650 \u00B7 6 GB Grafikspeicher")]
    [InlineData(1536, "NVIDIA GeForce GTX 1650 \u00B7 1,5 GB Grafikspeicher")]
    public void GraphicsMemoryIsShownInWholeGigabytes(long mib, string expected)
    {
        Assert.Equal(expected, UiText.GpuDescription(new Core.Gpu.GpuInfo("NVIDIA GeForce GTX 1650", mib)));
    }

    [Fact]
    public void CudaFailureIsExplainedInTheLog()
    {
        var result = new TranscriptionResult(JobOutcome.Failed, @"D:\a.mp4", @"D:\a.txt", TimeSpan.Zero)
        {
            Error = JobError.ProcessFailed,
            ExitCode = 1,
            Detail = "RuntimeError: CUDA failed with error no CUDA-capable device is detected",
            CudaProblem = Core.Gpu.CudaProblem.NoDevice,
        };

        Assert.Equal(
            "Fehler: Die Grafikkarte konnte nicht verwendet werden \u2013 keine passende NVIDIA-Grafikkarte gefunden (Code 1). " +
            "Letzte Meldung: RuntimeError: CUDA failed with error no CUDA-capable device is detected",
            UiText.ErrorText(result));
    }

    private static bool ContainsCyrillic(string text) => text.Any(c => c is >= '\u0400' and <= '\u04FF');
}
