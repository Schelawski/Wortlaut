using Wortlaut.Core;

namespace Wortlaut.Tests;

public class WhisperOutputParserTests
{
    [Theory]
    [InlineData("[00:03.120 --> 00:08.240]  Я могу сказать, что 99% практикующих", 8.24)]
    [InlineData("[48:10.000 --> 48:12.500]  Спасибо.", 48 * 60 + 12.5)]
    [InlineData("[01:02:03.456 --> 01:02:05.000]  Текст", 3600 + 2 * 60 + 5)]
    [InlineData("  [00:00.000 --> 00:01.900]  с пробелами в начале", 1.9)]
    public void SegmentLineYieldsEndTime(string line, double expectedSeconds)
    {
        Assert.True(WhisperOutputParser.TryParseSegmentEnd(line, out var end));
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), end);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Standalone Faster-Whisper-XXL r239.1 running on: CUDA")]
    [InlineData("Transcription speed: 3.72 audio seconds/s")]
    [InlineData("VAD filter kept the following audio segments: [00:00.000 -> 00:25.008]")]
    public void OtherLinesAreNotSegments(string? line)
    {
        Assert.False(WhisperOutputParser.TryParseSegmentEnd(line, out _));
    }

    [Theory]
    [InlineData("Processing audio with duration 00:25.008 ", 25.008)]
    [InlineData("Processing audio with duration 01:02:40.000", 3760)]
    public void DurationLineYieldsDuration(string line, double expectedSeconds)
    {
        Assert.True(WhisperOutputParser.TryParseDuration(line, out var duration));
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), duration);
    }

    [Theory]
    [InlineData(@"Subtitles are written to 'C:\Projekte\Spracherkennung\Faster-Whisper-XXL\.wortlaut-tmp\b16f8b61bc9d' directory.")]
    [InlineData("Operation finished in:  0:08:32.858 ")]
    public void CompletionLinesAreRecognized(string line)
    {
        Assert.True(WhisperOutputParser.IsCompletionLine(line));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Transcription speed: 2.41 audio seconds/s")]
    [InlineData("[19:32.320 --> 19:32.480]  Operation finished in: сказал он")]
    public void OtherLinesAreNotCompletion(string? line)
    {
        Assert.False(WhisperOutputParser.IsCompletionLine(line));
    }

    [Fact]
    public void ProgressFractionUsesDuration()
    {
        Assert.Equal(0.25, new JobProgressUpdate(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(4)).Fraction);
        Assert.Equal(1.0, new JobProgressUpdate(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(4)).Fraction);
        Assert.Null(new JobProgressUpdate(TimeSpan.FromMinutes(1), null).Fraction);
    }
}
