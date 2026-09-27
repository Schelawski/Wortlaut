using Wortlaut.Core;

namespace Wortlaut.Tests;

public class BulkQueueTests
{
    [Fact]
    public async Task FailedFileDoesNotStopTheQueue()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("Лекция 1.mp4")),
            new BulkItem(folder.CreateMedia("Лекция 2.mp4")),
            new BulkItem(folder.CreateMedia("Лекция 3.mp4")),
        };
        var runner = new FakeWhisperRunner().Succeeds("eins").Fails(1, "Fehler").Succeeds("drei");
        var queue = new BulkQueue(new TranscriptionJob(runner));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: true, null, CancellationToken.None);

        Assert.Equal(new BulkSummary(2, 0, 1, 0, summary.Elapsed), summary);
        Assert.True(File.Exists(Path.Combine(folder.Path, "Лекция 1.txt")));
        Assert.False(File.Exists(Path.Combine(folder.Path, "Лекция 2.txt")));
        Assert.Equal("drei", File.ReadAllText(Path.Combine(folder.Path, "Лекция 3.txt")));
    }

    [Fact]
    public async Task ExistingTranscriptsAreSkippedAndNotCountedAsWork()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("A.mp4")),
            new BulkItem(folder.CreateMedia("B.mov")),
            new BulkItem(folder.CreateMedia("C.m4a")),
        };
        folder.CreateFile("A.txt", "vorhanden");
        var runner = new FakeWhisperRunner().Succeeds("neu");
        var progress = new CollectingProgress<BulkUpdate>();
        var queue = new BulkQueue(new TranscriptionJob(runner));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: true, progress, CancellationToken.None);

        Assert.Equal(1, summary.Skipped);
        Assert.Equal(2, summary.Completed);
        Assert.Equal(2, runner.Requests.Count);
        Assert.Equal("vorhanden", File.ReadAllText(Path.Combine(folder.Path, "A.txt")));

        var started = progress.Items.OfType<BulkItemStarted>().ToList();
        Assert.Equal([(1, 1, 2), (2, 2, 2)], started.Select(s => (s.ItemIndex, s.Position, s.Total)));

        var skipped = Assert.Single(progress.Items.OfType<BulkItemFinished>(), f => f.Result.Outcome == JobOutcome.Skipped);
        Assert.Equal(0, skipped.ItemIndex);
    }

    [Fact]
    public async Task ExistingTranscriptsAreReplacedWhenSkippingIsOff()
    {
        using var folder = new TempFolder();
        var items = new[] { new BulkItem(folder.CreateMedia("A.mp4")) };
        folder.CreateFile("A.txt", "alt");
        var queue = new BulkQueue(new TranscriptionJob(new FakeWhisperRunner().Succeeds("neu")));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: false, null, CancellationToken.None);

        Assert.Equal(1, summary.Completed);
        Assert.Equal("neu", File.ReadAllText(Path.Combine(folder.Path, "A.txt")));
    }

    [Fact]
    public async Task CancellationStopsCurrentAndRemainingFiles()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("1.mp4")),
            new BulkItem(folder.CreateMedia("2.mp4")),
            new BulkItem(folder.CreateMedia("3.mp4")),
        };
        var originals = items.Select(i => FileSnapshot.Take(i.MediaPath)).ToList();
        var runner = new FakeWhisperRunner().Succeeds().RunsUntilCancelled();
        var progress = new CollectingProgress<BulkUpdate>();
        var queue = new BulkQueue(new TranscriptionJob(runner));
        using var cts = new CancellationTokenSource();

        var running = queue.RunAsync(items, TestSettings.Create(), skipExisting: true, progress, cts.Token);
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        var summary = await running.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(new BulkSummary(1, 0, 0, 2, summary.Elapsed), summary);
        Assert.Equal(2, runner.Requests.Count); // the third file was never started
        Assert.Equal(
            [JobOutcome.Completed, JobOutcome.Cancelled, JobOutcome.Cancelled],
            progress.Items.OfType<BulkItemFinished>().OrderBy(f => f.ItemIndex).Select(f => f.Result.Outcome));
        // "n of m" must not count cancelled files as done.
        Assert.Equal((1, 3), progress.Items.OfType<BulkItemFinished>().Select(f => (f.Finished, f.Total)).Last());
        Assert.False(Directory.Exists(Path.Combine(folder.Path, ".wortlaut-tmp")));
        Assert.Equal(originals, items.Select(i => FileSnapshot.Take(i.MediaPath)));
    }

    [Fact]
    public async Task SameTargetFromTwoMediaFilesIsNotOverwritten()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("Talk.mp4")),
            new BulkItem(folder.CreateMedia("Talk.mp3")),
        };
        var runner = new FakeWhisperRunner().Succeeds("video").Succeeds("audio");
        var progress = new CollectingProgress<BulkUpdate>();
        var queue = new BulkQueue(new TranscriptionJob(runner));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: false, progress, CancellationToken.None);

        Assert.Equal(1, summary.Completed);
        Assert.Equal(1, summary.Skipped);
        Assert.Equal("video", File.ReadAllText(Path.Combine(folder.Path, "Talk.txt")));
        Assert.Contains(progress.Items.OfType<BulkItemFinished>(), f => f.Result.SkipReason == SkipReason.DuplicateTarget);
    }

    [Fact]
    public async Task JobUpdatesAreTaggedWithTheirItem()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("1.mp4")),
            new BulkItem(folder.CreateMedia("2.mp4"), TimeSpan.FromMinutes(1)),
        };
        var runner = new FakeWhisperRunner().Succeeds("ok", null, "[00:00.000 --> 00:30.000]  Текст");
        var progress = new CollectingProgress<BulkUpdate>();
        var queue = new BulkQueue(new TranscriptionJob(runner));

        await queue.RunAsync(items, TestSettings.Create(), skipExisting: true, progress, CancellationToken.None);

        var progressUpdates = progress.Items.OfType<BulkItemJobUpdate>()
            .Where(u => u.Update is JobProgressUpdate)
            .Select(u => (u.ItemIndex, ((JobProgressUpdate)u.Update).Fraction))
            .ToList();
        Assert.Equal([(0, (double?)null), (1, 0.5)], progressUpdates);
    }
}
