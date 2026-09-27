using Wortlaut.Core;

namespace Wortlaut.Tests;

public class WhisperCommandLineTests
{
    private const string Input = @"D:\Videos\Satsang 2026-09\.wortlaut-tmp\abc\job.mp4";
    private const string OutputDir = @"D:\Videos\Satsang 2026-09\.wortlaut-tmp\abc";

    [Fact]
    public void ArgumentsContainAllSettingsInOrder()
    {
        var arguments = WhisperCommandLine.BuildArguments(TestSettings.Create(), Input, OutputDir);

        Assert.Equal(
            ["--model", "large-v2", "--device", "cuda", "--output_format", "text", "--language", "ru", "--output_dir", OutputDir, Input],
            arguments);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("")]
    [InlineData("  ")]
    public void AutomaticLanguageOmitsLanguageArgument(string language)
    {
        var arguments = WhisperCommandLine.BuildArguments(TestSettings.Create(language: language), Input, OutputDir);

        Assert.DoesNotContain("--language", arguments);
        Assert.Equal(
            ["--model", "large-v2", "--device", "cuda", "--output_format", "text", "--output_dir", OutputDir, Input],
            arguments);
    }

    [Theory]
    [InlineData(OutputFormat.Json, "json")]
    [InlineData(OutputFormat.Srt, "srt")]
    [InlineData(OutputFormat.Vtt, "vtt")]
    public void OutputFormatUsesCommandLineValue(OutputFormat format, string expected)
    {
        var arguments = WhisperCommandLine.BuildArguments(TestSettings.Create(format), Input, OutputDir);

        var index = arguments.ToList().IndexOf("--output_format");
        Assert.Equal(expected, arguments[index + 1]);
    }

    [Fact]
    public void PathsWithSpacesAndCyrillicStayOneArgumentEach()
    {
        const string input = @"D:\Видео и аудио\Лекция 12 — Медитация.mp4";
        const string outputDir = @"D:\Видео и аудио\.wortlaut-tmp\x";

        var arguments = WhisperCommandLine.BuildArguments(TestSettings.Create(), input, outputDir);

        Assert.Equal(input, arguments[^1]);
        Assert.Equal(outputDir, arguments[^2]);
        Assert.Equal("--output_dir", arguments[^3]);
    }

    [Fact]
    public void InputFileDoesNotDirectlyFollowOutputFormat()
    {
        // --output_format takes several values; an input file right after it would be read as a format.
        var arguments = WhisperCommandLine.BuildArguments(TestSettings.Create(language: "auto"), Input, OutputDir).ToList();

        var index = arguments.IndexOf("--output_format");
        Assert.StartsWith("--", arguments[index + 2]);
    }

    [Fact]
    public void DisplayStringQuotesValuesWithSpaces()
    {
        var display = WhisperCommandLine.ToDisplayString(@"C:\Tools\faster-whisper-xxl.exe", ["--model", "large-v2", @"D:\A B\job.mp4"]);

        Assert.Equal(@"C:\Tools\faster-whisper-xxl.exe --model large-v2 ""D:\A B\job.mp4""", display);
    }
}
