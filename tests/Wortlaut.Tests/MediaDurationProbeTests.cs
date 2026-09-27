using Wortlaut.Core;

namespace Wortlaut.Tests;

public class MediaDurationProbeTests
{
    [Fact]
    public void FfmpegDurationIsParsed()
    {
        const string output = """
            Input #0, mov,mp4,m4a,3gp,3g2,mj2, from 'Лекция 12.mp4':
              Duration: 01:02:40.12, start: 0.000000, bitrate: 344 kb/s
            """;

        Assert.Equal(new TimeSpan(0, 1, 2, 40, 120), MediaDurationProbe.ParseFfmpegDuration(output));
    }

    [Theory]
    [InlineData("  Duration: N/A, bitrate: N/A")]
    [InlineData("")]
    [InlineData(null)]
    public void MissingFfmpegDurationYieldsNull(string? output)
    {
        Assert.Null(MediaDurationProbe.ParseFfmpegDuration(output));
    }

    [Fact]
    public void FfmpegIsLookedUpNextToFasterWhisper()
    {
        using var folder = new TempFolder();
        var exe = folder.CreateFile("faster-whisper-xxl.exe", "exe");

        Assert.Null(MediaDurationProbe.FindFfmpegNextTo(exe));

        var ffmpeg = folder.CreateFile("ffmpeg.exe", "exe");
        Assert.Equal(ffmpeg, MediaDurationProbe.FindFfmpegNextTo(exe));
        Assert.Null(MediaDurationProbe.FindFfmpegNextTo(""));
    }

    [Fact]
    public async Task UnreadableFileYieldsNoDuration()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia("kaputt.mp4", size: 128);
        var probe = new MediaDurationProbe(() => null);

        Assert.Null(await probe.GetDurationAsync(media, CancellationToken.None));
    }
}
