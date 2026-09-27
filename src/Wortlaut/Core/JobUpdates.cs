namespace Wortlaut.Core;

/// <summary>
/// Something that happened during a transcription job. The UI turns these into log lines and progress.
/// </summary>
public abstract record JobUpdate;

/// <summary>A line printed by faster-whisper.</summary>
public sealed record JobOutputUpdate(string Line, bool IsError) : JobUpdate;

/// <summary>A notable step of the job. The UI formats <see cref="Kind"/> into a localized text.</summary>
/// <param name="Kind">What happened.</param>
/// <param name="Detail">Optional value, e.g. a path, a command line or an error message.</param>
public sealed record JobMessageUpdate(JobMessageKind Kind, string? Detail = null) : JobUpdate;

/// <summary>Transcription progress.</summary>
/// <param name="Position">End time of the last transcribed segment.</param>
/// <param name="Duration">Total audio duration, if known.</param>
public sealed record JobProgressUpdate(TimeSpan Position, TimeSpan? Duration) : JobUpdate
{
    /// <summary>Progress between 0 and 1, or <c>null</c> when the duration is unknown.</summary>
    public double? Fraction =>
        Duration is { } duration && duration > TimeSpan.Zero
            ? Math.Clamp(Position / duration, 0d, 1d)
            : null;
}

public enum JobMessageKind
{
    /// <summary>The transcript already exists and overwriting is off. Detail: transcript path.</summary>
    SkippedExisting,

    /// <summary>A hard link could not be created, the media file was copied instead. Detail: reason.</summary>
    CopiedInsteadOfHardLink,

    /// <summary>The media file is read-only, so it was copied instead of linked.</summary>
    CopiedReadOnlyMedia,

    /// <summary>faster-whisper is being started. Detail: command line for display.</summary>
    Starting,

    /// <summary>The temporary work folder could not be removed. Detail: path and reason.</summary>
    CleanupFailed,
}

public enum JobOutcome
{
    Completed,
    Skipped,
    Failed,
    Cancelled,
}

public enum JobError
{
    None,

    /// <summary>The media file does not exist.</summary>
    MediaNotFound,

    /// <summary>faster-whisper could not be started. Detail: reason.</summary>
    StartFailed,

    /// <summary>faster-whisper exited with a non-zero exit code. Detail: last error line.</summary>
    ProcessFailed,

    /// <summary>faster-whisper reported success but did not write the expected file. Detail: last output line.</summary>
    ResultMissing,

    /// <summary>Any other error, e.g. a file system error. Detail: exception message.</summary>
    Unexpected,
}

public enum SkipReason
{
    /// <summary>The transcript already existed and overwriting was off.</summary>
    TargetExists,

    /// <summary>Another file in the same folder run already produced this transcript (e.g. "a.mp4" and "a.mp3").</summary>
    DuplicateTarget,
}

/// <summary>
/// Result of a transcription job.
/// </summary>
public sealed record TranscriptionResult(JobOutcome Outcome, string MediaPath, string TargetPath, TimeSpan Elapsed)
{
    public JobError Error { get; init; } = JobError.None;

    public SkipReason? SkipReason { get; init; }

    /// <summary>faster-whisper exit code, if the process ran to the end.</summary>
    public int? ExitCode { get; init; }

    /// <summary>Error details, see <see cref="JobError"/>.</summary>
    public string? Detail { get; init; }
}
