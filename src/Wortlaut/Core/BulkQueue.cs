using System.Diagnostics;

namespace Wortlaut.Core;

/// <summary>A file of a folder run.</summary>
/// <param name="MediaPath">Video or audio file.</param>
/// <param name="Duration">Audio duration if known (for the progress in percent).</param>
public sealed record BulkItem(string MediaPath, TimeSpan? Duration = null);

/// <summary>Progress of a folder run. <see cref="ItemIndex"/> refers to the list passed to <see cref="BulkQueue.RunAsync"/>.</summary>
public abstract record BulkUpdate(int ItemIndex);

/// <summary>A file is being transcribed now.</summary>
/// <param name="Position">1-based position among the files that are actually transcribed.</param>
/// <param name="Total">Number of files that are actually transcribed (without skipped ones).</param>
public sealed record BulkItemStarted(int ItemIndex, int Position, int Total) : BulkUpdate(ItemIndex);

/// <summary>An update of the running job.</summary>
public sealed record BulkItemJobUpdate(int ItemIndex, JobUpdate Update) : BulkUpdate(ItemIndex);

/// <summary>A file is done (with any outcome).</summary>
/// <param name="Finished">
/// Number of files processed so far, including this one. Files skipped up front and cancelled files are not counted.
/// </param>
/// <param name="Total">Number of files that are actually transcribed.</param>
public sealed record BulkItemFinished(int ItemIndex, TranscriptionResult Result, int Finished, int Total) : BulkUpdate(ItemIndex);

/// <summary>Summary of a folder run.</summary>
public sealed record BulkSummary(int Completed, int Skipped, int Failed, int Cancelled, TimeSpan Elapsed)
{
    public int Total => Completed + Skipped + Failed + Cancelled;
}

/// <summary>
/// Transcribes several files one after another (one GPU) with the same settings.
/// A failed file does not stop the queue; cancelling stops the whole queue.
/// </summary>
public sealed class BulkQueue(TranscriptionJob job)
{
    private readonly TranscriptionJob _job = job ?? throw new ArgumentNullException(nameof(job));

    /// <param name="items">Files to transcribe, in order.</param>
    /// <param name="settings">Settings for every file.</param>
    /// <param name="skipExisting">Skip files whose transcript exists; otherwise existing transcripts are replaced.</param>
    /// <param name="progress">Receives <see cref="BulkUpdate"/>s.</param>
    /// <param name="cancellationToken">Cancels the running file and all remaining files.</param>
    public async Task<BulkSummary> RunAsync(
        IReadOnlyList<BulkItem> items,
        WhisperSettings settings,
        bool skipExisting,
        IProgress<BulkUpdate>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(settings);

        var stopwatch = Stopwatch.StartNew();
        int completed = 0, skipped = 0, failed = 0, cancelled = 0;

        void Count(TranscriptionResult result)
        {
            switch (result.Outcome)
            {
                case JobOutcome.Completed: completed++; break;
                case JobOutcome.Skipped: skipped++; break;
                case JobOutcome.Failed: failed++; break;
                case JobOutcome.Cancelled: cancelled++; break;
            }
        }

        // Files with an existing transcript are skipped up front, so "n of m" only counts real work.
        var pending = new List<int>();
        var skippedUpFront = new List<(int Index, TranscriptionResult Result)>();
        for (var index = 0; index < items.Count; index++)
        {
            var targetPath = TranscriptionPaths.GetTargetPath(items[index].MediaPath, settings.Format);
            if (skipExisting && File.Exists(targetPath))
            {
                var result = new TranscriptionResult(JobOutcome.Skipped, Path.GetFullPath(items[index].MediaPath), targetPath, TimeSpan.Zero)
                {
                    SkipReason = SkipReason.TargetExists,
                };
                skippedUpFront.Add((index, result));
            }
            else
            {
                pending.Add(index);
            }
        }

        var total = pending.Count;
        var finished = 0;

        foreach (var (index, result) in skippedUpFront)
        {
            Count(result);
            progress?.Report(new BulkItemFinished(index, result, finished, total));
        }
        var producedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var index in pending)
        {
            var item = items[index];
            TranscriptionResult result;

            if (cancellationToken.IsCancellationRequested)
            {
                result = new TranscriptionResult(
                    JobOutcome.Cancelled,
                    Path.GetFullPath(item.MediaPath),
                    TranscriptionPaths.GetTargetPath(item.MediaPath, settings.Format),
                    TimeSpan.Zero);
            }
            else
            {
                progress?.Report(new BulkItemStarted(index, finished + 1, total));

                var targetPath = TranscriptionPaths.GetTargetPath(item.MediaPath, settings.Format);
                if (producedTargets.Contains(targetPath))
                {
                    // E.g. "Talk.mp4" and "Talk.mp3": do not replace the transcript written a moment ago.
                    result = new TranscriptionResult(JobOutcome.Skipped, Path.GetFullPath(item.MediaPath), targetPath, TimeSpan.Zero)
                    {
                        SkipReason = SkipReason.DuplicateTarget,
                    };
                }
                else
                {
                    var itemProgress = progress is null
                        ? null
                        : new InlineProgress<JobUpdate>(update => progress.Report(new BulkItemJobUpdate(index, update)));

                    result = await _job.RunAsync(
                        new TranscriptionRequest(item.MediaPath, settings, Overwrite: !skipExisting, item.Duration),
                        itemProgress,
                        cancellationToken).ConfigureAwait(false);

                    if (result.Outcome == JobOutcome.Completed)
                        producedTargets.Add(result.TargetPath);
                }
            }

            if (result.Outcome != JobOutcome.Cancelled)
                finished++;
            Count(result);
            progress?.Report(new BulkItemFinished(index, result, finished, total));
        }

        return new BulkSummary(completed, skipped, failed, cancelled, stopwatch.Elapsed);
    }
}
