using Wortlaut.Core;

namespace Wortlaut.Tests;

public class TranscriptionPathsTests
{
    [Theory]
    [InlineData(OutputFormat.Text, @"D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.txt")]
    [InlineData(OutputFormat.Json, @"D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.json")]
    [InlineData(OutputFormat.Srt, @"D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.srt")]
    [InlineData(OutputFormat.Vtt, @"D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.vtt")]
    public void TargetPathKeepsFolderAndNameWithCyrillicAndSpaces(OutputFormat format, string expected)
    {
        var target = TranscriptionPaths.GetTargetPath(@"D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.mp4", format);

        Assert.Equal(expected, target);
    }

    [Fact]
    public void TargetPathOnlyReplacesTheLastExtension()
    {
        var target = TranscriptionPaths.GetTargetPath(@"D:\Videos\Q&A 14.09.mov", OutputFormat.Json);

        Assert.Equal(@"D:\Videos\Q&A 14.09.json", target);
    }

    [Fact]
    public void WorkDirectoryLivesInTempFolderNextToTheMedia()
    {
        var workDirectory = TranscriptionPaths.GetWorkDirectory(@"D:\Videos\Лекция 1.mp4", "abc123");

        Assert.Equal(@"D:\Videos\.wortlaut-tmp\abc123", workDirectory);
    }

    [Fact]
    public void ExistingTranscriptIsSkippedWhenOverwriteIsOff()
    {
        using var folder = new TempFolder();
        var target = folder.CreateFile("Лекция 1.txt", "alt");

        Assert.True(TranscriptionPaths.ShouldSkip(target, overwrite: false));
    }

    [Fact]
    public void ExistingTranscriptIsNotSkippedWhenOverwriteIsOn()
    {
        using var folder = new TempFolder();
        var target = folder.CreateFile("Лекция 1.txt", "alt");

        Assert.False(TranscriptionPaths.ShouldSkip(target, overwrite: true));
    }

    [Fact]
    public void MissingTranscriptIsNeverSkipped()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "Лекция 1.txt");

        Assert.False(TranscriptionPaths.ShouldSkip(target, overwrite: false));
        Assert.False(TranscriptionPaths.ShouldSkip(target, overwrite: true));
    }
}
