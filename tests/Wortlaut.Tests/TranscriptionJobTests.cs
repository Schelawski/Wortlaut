using Wortlaut.Core;

namespace Wortlaut.Tests;

public class TranscriptionJobTests
{
    private const string MediaName = "Лекция 12 — Медитация и дыхание.mp4";

    [Fact]
    public async Task CompletedJobWritesTranscriptNextToMedia()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds("Привет, мир");
        var job = new TranscriptionJob(runner);

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        var expectedTarget = Path.Combine(folder.Path, "Лекция 12 — Медитация и дыхание.txt");
        Assert.Equal(expectedTarget, result.TargetPath);
        Assert.Equal("Привет, мир", File.ReadAllText(expectedTarget));
    }

    [Theory]
    [InlineData(OutputFormat.Text, ".text", ".txt")] // current Faster-Whisper-XXL
    [InlineData(OutputFormat.Text, ".txt", ".txt")]  // older builds
    [InlineData(OutputFormat.Json, ".json", ".json")]
    [InlineData(OutputFormat.Srt, ".srt", ".srt")]
    [InlineData(OutputFormat.Vtt, ".vtt", ".vtt")]
    public async Task ProducedFileIsRenamedToTargetExtension(OutputFormat format, string produced, string expectedExtension)
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds("ok", produced);
        var job = new TranscriptionJob(runner);

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(format), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        Assert.True(File.Exists(Path.Combine(folder.Path, "Лекция 12 — Медитация и дыхание" + expectedExtension)));
    }

    [Fact]
    public async Task RunnerTranscribesHardLinkInWorkFolder()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds();
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(runner, jobIdFactory: () => "job42");

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), progress, CancellationToken.None);

        // The temp folder is on NTFS, so CreateHardLinkW must succeed with the Cyrillic path (no copy).
        Assert.DoesNotContain(progress.Items, u => u is JobMessageUpdate { Kind: JobMessageKind.CopiedInsteadOfHardLink });
        var request = Assert.Single(runner.Requests);
        var workDirectory = Path.Combine(folder.Path, ".wortlaut-tmp", "job42");
        Assert.Equal(workDirectory, request.OutputDirectory);
        Assert.Equal(Path.Combine(workDirectory, "job.mp4"), request.InputPath);
        Assert.Equal(File.ReadAllBytes(media), runner.InputContents[0]);
    }

    [Fact]
    public async Task OriginalMediaStaysUnchanged()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var before = FileSnapshot.Take(media);
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds());

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(before, FileSnapshot.Take(media));
    }

    [Fact]
    public async Task WorkFolderIsRemovedAfterSuccess()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds());

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
        Assert.Equal(
            [MediaName, "Лекция 12 — Медитация и дыхание.txt"],
            Directory.GetFileSystemEntries(folder.Path).Select(Path.GetFileName).Order());
    }

    [Fact]
    public async Task ExistingTranscriptIsSkippedWithoutRunning()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var target = folder.CreateFile("Лекция 12 — Медитация и дыхание.txt", "alt");
        var runner = new FakeWhisperRunner().Succeeds("neu");
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(runner);

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), progress, CancellationToken.None);

        Assert.Equal(JobOutcome.Skipped, result.Outcome);
        Assert.Equal(SkipReason.TargetExists, result.SkipReason);
        Assert.Empty(runner.Requests);
        Assert.Equal("alt", File.ReadAllText(target));
        Assert.Contains(progress.Items, u => u is JobMessageUpdate { Kind: JobMessageKind.SkippedExisting });
    }

    [Fact]
    public async Task ExistingTranscriptIsReplacedWhenOverwriteIsOn()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var target = folder.CreateFile("Лекция 12 — Медитация и дыхание.txt", "alt");
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds("neu"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: true), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        Assert.Equal("neu", File.ReadAllText(target));
    }

    [Fact]
    public async Task ExistingTranscriptOfOtherFormatDoesNotCauseSkip()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        folder.CreateFile("Лекция 12 — Медитация и дыхание.txt", "text");
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds("{}"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(OutputFormat.Json), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        Assert.Equal("{}", File.ReadAllText(Path.Combine(folder.Path, "Лекция 12 — Медитация и дыхание.json")));
    }

    [Fact]
    public async Task NonZeroExitCodeFailsWithLastErrorLineAndCleansUp()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var before = FileSnapshot.Take(media);
        var job = new TranscriptionJob(new FakeWhisperRunner().Fails(1, "RuntimeError: CUDA failed with error out of memory"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Failed, result.Outcome);
        Assert.Equal(JobError.ProcessFailed, result.Error);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("RuntimeError: CUDA failed with error out of memory", result.Detail);
        Assert.False(File.Exists(result.TargetPath));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
        Assert.Equal(before, FileSnapshot.Take(media));
    }

    [Fact]
    public async Task SuccessWithoutResultFileIsAnError()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var job = new TranscriptionJob(new FakeWhisperRunner().SucceedsWithoutResult("Operation finished in: 0:00:01"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Failed, result.Outcome);
        Assert.Equal(JobError.ResultMissing, result.Error);
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
    }

    [Fact]
    public async Task MissingExecutableIsReportedAsStartFailure()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var job = new TranscriptionJob(new FakeWhisperRunner().CannotStart());

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Failed, result.Outcome);
        Assert.Equal(JobError.StartFailed, result.Error);
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
    }

    [Fact]
    public async Task CancellationStopsJobCleansUpAndKeepsOriginal()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var before = FileSnapshot.Take(media);
        var runner = new FakeWhisperRunner().RunsUntilCancelled();
        var job = new TranscriptionJob(runner);
        using var cts = new CancellationTokenSource();

        var running = job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, cts.Token);
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        var result = await running.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(JobOutcome.Cancelled, result.Outcome);
        Assert.False(File.Exists(result.TargetPath));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
        Assert.Equal(before, FileSnapshot.Take(media));
    }

    [Fact]
    public async Task AlreadyCancelledTokenDoesNothing()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds();
        var job = new TranscriptionJob(runner);

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, new CancellationToken(canceled: true));

        Assert.Equal(JobOutcome.Cancelled, result.Outcome);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task HardLinkFailureFallsBackToCopyAndLogsIt()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var before = FileSnapshot.Take(media);
        var runner = new FakeWhisperRunner().Succeeds();
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(runner, new FailingHardLinker());

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), progress, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        Assert.Equal(File.ReadAllBytes(media), runner.InputContents[0]);
        Assert.Contains(progress.Items, u => u is JobMessageUpdate { Kind: JobMessageKind.CopiedInsteadOfHardLink, Detail: "Die Funktion ist ungültig." });
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
        Assert.Equal(before, FileSnapshot.Take(media));
    }

    [Fact]
    public async Task ReadOnlyMediaIsCopiedAndStaysReadOnly()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        File.SetAttributes(media, FileAttributes.ReadOnly);
        var before = FileSnapshot.Take(media);
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds());

        try
        {
            var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), progress, CancellationToken.None);

            Assert.Equal(JobOutcome.Completed, result.Outcome);
            Assert.Contains(progress.Items, u => u is JobMessageUpdate { Kind: JobMessageKind.CopiedReadOnlyMedia });
            Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
            Assert.Equal(before, FileSnapshot.Take(media));
        }
        finally
        {
            File.SetAttributes(media, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task OtherWorkFoldersInTempFolderAreLeftAlone()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var foreign = folder.CreateFile(Path.Combine(".wortlaut-tmp", "other", "job.mp4"));
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds());

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.True(File.Exists(foreign));
    }

    [Fact]
    public async Task SegmentLinesAreReportedAsProgress()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds(
            "ok",
            null,
            "Starting sequential inference to transcribe: job.mp4",
            "[00:00.000 --> 00:30.000]  Первый",
            "[00:30.000 --> 01:00.000]  Второй");
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(runner);

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false, KnownDuration: TimeSpan.FromMinutes(2)), progress, CancellationToken.None);

        var fractions = progress.Items.OfType<JobProgressUpdate>().Select(p => p.Fraction).ToList();
        Assert.Equal([0.25, 0.5], fractions);
        Assert.Equal(3, progress.Items.OfType<JobOutputUpdate>().Count());
        Assert.Contains(progress.Items, u => u is JobMessageUpdate { Kind: JobMessageKind.Starting });
    }

    [Fact]
    public async Task ProgressWithoutDurationHasNoFraction()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia(MediaName);
        var runner = new FakeWhisperRunner().Succeeds("ok", null, "[00:00.000 --> 00:30.000]  Первый");
        var progress = new CollectingProgress<JobUpdate>();
        var job = new TranscriptionJob(runner);

        await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), progress, CancellationToken.None);

        var update = Assert.Single(progress.Items.OfType<JobProgressUpdate>());
        Assert.Null(update.Fraction);
        Assert.Equal(TimeSpan.FromSeconds(30), update.Position);
    }

    [Fact]
    public async Task MissingMediaFails()
    {
        using var folder = new TempFolder();
        var runner = new FakeWhisperRunner().Succeeds();
        var job = new TranscriptionJob(runner);

        var result = await job.RunAsync(new TranscriptionRequest(Path.Combine(folder.Path, "fehlt.mp4"), TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Failed, result.Outcome);
        Assert.Equal(JobError.MediaNotFound, result.Error);
        Assert.Empty(runner.Requests);
    }
}
