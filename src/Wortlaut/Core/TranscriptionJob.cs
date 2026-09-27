using System.Diagnostics;

namespace Wortlaut.Core;

/// <summary>What to transcribe.</summary>
/// <param name="MediaPath">Video or audio file. It is only read, never renamed, moved or deleted.</param>
/// <param name="Settings">faster-whisper settings.</param>
/// <param name="Overwrite">Replace an existing transcript instead of skipping the file.</param>
/// <param name="KnownDuration">Audio duration if already known (used for the progress in percent).</param>
public sealed record TranscriptionRequest(
    string MediaPath,
    WhisperSettings Settings,
    bool Overwrite,
    TimeSpan? KnownDuration = null);

/// <summary>
/// Transcribes one media file:
/// <list type="number">
/// <item>Skip if the transcript exists and overwriting is off.</item>
/// <item>Create <c>.wortlaut-tmp\{jobId}\job{ext}</c> as a hard link to the media file (copy as fallback).</item>
/// <item>Run faster-whisper on that link with the work folder as output folder.</item>
/// <item>Move the result next to the media file under the media file's name.</item>
/// <item>Always delete the work folder.</item>
/// </list>
/// </summary>
public sealed class TranscriptionJob
{
    private const int CleanupAttempts = 10;
    private static readonly TimeSpan CleanupRetryDelay = TimeSpan.FromMilliseconds(300);

    private readonly IWhisperRunner _runner;
    private readonly IHardLinker _hardLinker;
    private readonly Func<string> _jobIdFactory;

    public TranscriptionJob(IWhisperRunner runner, IHardLinker? hardLinker = null, Func<string>? jobIdFactory = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _hardLinker = hardLinker ?? new NativeHardLinker();
        _jobIdFactory = jobIdFactory ?? (() => Guid.NewGuid().ToString("N")[..12]);
    }

    /// <summary>
    /// Runs the job. Never throws for expected failures or cancellation; see <see cref="TranscriptionResult.Outcome"/>.
    /// </summary>
    public async Task<TranscriptionResult> RunAsync(
        TranscriptionRequest request,
        IProgress<JobUpdate>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var mediaPath = Path.GetFullPath(request.MediaPath);
        var targetPath = TranscriptionPaths.GetTargetPath(mediaPath, request.Settings.Format);

        TranscriptionResult Result(JobOutcome outcome) => new(outcome, mediaPath, targetPath, stopwatch.Elapsed);

        if (cancellationToken.IsCancellationRequested)
            return Result(JobOutcome.Cancelled);

        if (!File.Exists(mediaPath))
            return Result(JobOutcome.Failed) with { Error = JobError.MediaNotFound, Detail = mediaPath };

        if (TranscriptionPaths.ShouldSkip(targetPath, request.Overwrite))
            return Result(JobOutcome.Skipped) with { SkipReason = SkipReason.TargetExists };

        var tempRoot = TranscriptionPaths.GetTempRoot(mediaPath);
        var workDirectory = TranscriptionPaths.GetWorkDirectory(mediaPath, _jobIdFactory());
        var jobFile = Path.Combine(workDirectory, TranscriptionPaths.JobFileBaseName + Path.GetExtension(mediaPath));

        try
        {
            Directory.CreateDirectory(workDirectory);
            await ProvideJobFileAsync(mediaPath, jobFile, progress, cancellationToken).ConfigureAwait(false);

            var runRequest = new WhisperRunRequest(request.Settings, jobFile, workDirectory);
            progress?.Report(new JobMessageUpdate(
                JobMessageKind.Starting,
                WhisperCommandLine.ToDisplayString(
                    request.Settings.ExePath,
                    WhisperCommandLine.BuildArguments(request.Settings, jobFile, workDirectory))));

            var tracker = new ProgressTracker(request.KnownDuration, progress);
            WhisperRunResult run;
            try
            {
                run = await _runner.RunAsync(runRequest, tracker.OnOutput, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
                return Result(JobOutcome.Failed) with { Error = JobError.StartFailed, Detail = ex.Message };
            }

            if (cancellationToken.IsCancellationRequested)
                return Result(JobOutcome.Cancelled);

            if (run.ExitCode != 0)
            {
                return Result(JobOutcome.Failed) with
                {
                    Error = JobError.ProcessFailed,
                    ExitCode = run.ExitCode,
                    Detail = run.LastErrorLine,
                };
            }

            var producedFile = FindProducedFile(workDirectory, request.Settings.Format);
            if (producedFile is null)
            {
                return Result(JobOutcome.Failed) with
                {
                    Error = JobError.ResultMissing,
                    ExitCode = run.ExitCode,
                    Detail = run.LastErrorLine,
                };
            }

            // Same volume as the media file, so this is a rename. Replaces an existing transcript only when allowed.
            File.Move(producedFile, targetPath, overwrite: request.Overwrite);
            return Result(JobOutcome.Completed) with { ExitCode = run.ExitCode };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(JobOutcome.Cancelled);
        }
        catch (Exception ex)
        {
            // Job boundary: every failure is reported as a result so a folder run can continue with the next file.
            return Result(JobOutcome.Failed) with { Error = JobError.Unexpected, Detail = ex.Message };
        }
        finally
        {
            await CleanupAsync(workDirectory, jobFile, tempRoot, progress).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Makes the media file available as <paramref name="jobFile"/> without touching the original:
    /// a hard link if possible, otherwise a copy.
    /// </summary>
    private async Task ProvideJobFileAsync(
        string mediaPath,
        string jobFile,
        IProgress<JobUpdate>? progress,
        CancellationToken cancellationToken)
    {
        // Attributes belong to the file, not to the link. A read-only link could only be deleted by
        // clearing the attribute, which would change the original, so read-only media is copied.
        if (File.GetAttributes(mediaPath).HasFlag(FileAttributes.ReadOnly))
        {
            progress?.Report(new JobMessageUpdate(JobMessageKind.CopiedReadOnlyMedia));
            await CopyAsync(mediaPath, jobFile, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_hardLinker.TryCreateHardLink(jobFile, mediaPath, out var error))
            return;

        progress?.Report(new JobMessageUpdate(JobMessageKind.CopiedInsteadOfHardLink, error));
        await CopyAsync(mediaPath, jobFile, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyAsync(string source, string destination, CancellationToken cancellationToken)
    {
        const int bufferSize = 1 << 20;
        await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous))
        {
            await input.CopyToAsync(output, bufferSize, cancellationToken).ConfigureAwait(false);
        }

        // The copy is independent of the original, so its attributes can be reset safely.
        File.SetAttributes(destination, FileAttributes.Normal);
    }

    private static string? FindProducedFile(string workDirectory, OutputFormat format)
    {
        foreach (var extension in OutputFormats.Get(format).ProducedExtensions)
        {
            var candidate = Path.Combine(workDirectory, TranscriptionPaths.JobFileBaseName + extension);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Deletes the work folder and, if it is empty afterwards, the <c>.wortlaut-tmp</c> folder.
    /// Retries for a moment because a killed process may still hold file handles.
    /// </summary>
    private static async Task CleanupAsync(string workDirectory, string jobFile, string tempRoot, IProgress<JobUpdate>? progress)
    {
        for (var attempt = 1; attempt <= CleanupAttempts; attempt++)
        {
            try
            {
                // Remove the link first: deleting a hard link only removes this name, the original stays.
                if (File.Exists(jobFile))
                    File.Delete(jobFile);
                if (Directory.Exists(workDirectory))
                    Directory.Delete(workDirectory, recursive: true);
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == CleanupAttempts)
                {
                    progress?.Report(new JobMessageUpdate(JobMessageKind.CleanupFailed, $"{workDirectory}: {ex.Message}"));
                    return;
                }

                await Task.Delay(CleanupRetryDelay).ConfigureAwait(false);
            }
        }

        try
        {
            if (Directory.Exists(tempRoot) && !Directory.EnumerateFileSystemEntries(tempRoot).Any())
                Directory.Delete(tempRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Another job may have created a new work folder in the meantime; leave it alone.
        }
    }

    /// <summary>
    /// Turns faster-whisper output into <see cref="JobUpdate"/>s. Called on the runner's output threads.
    /// </summary>
    private sealed class ProgressTracker(TimeSpan? knownDuration, IProgress<JobUpdate>? progress)
    {
        private readonly object _sync = new();
        private TimeSpan? _duration = knownDuration;

        public void OnOutput(OutputLine line)
        {
            progress?.Report(new JobOutputUpdate(line.Text, line.IsError));

            JobProgressUpdate? update = null;
            lock (_sync)
            {
                if (WhisperOutputParser.TryParseDuration(line.Text, out var duration))
                {
                    _duration = duration;
                    update = new JobProgressUpdate(TimeSpan.Zero, _duration);
                }
                else if (WhisperOutputParser.TryParseSegmentEnd(line.Text, out var end))
                {
                    update = new JobProgressUpdate(end, _duration);
                }
            }

            if (update is not null)
                progress?.Report(update);
        }
    }
}
