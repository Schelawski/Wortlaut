namespace Wortlaut.Core.Setup;

/// <summary>Why the setup failed. The UI turns this into a plain-language message.</summary>
public enum SetupError
{
    /// <summary>Not enough free disk space.</summary>
    NotEnoughSpace,

    /// <summary>The download failed (no internet, server error) even after retries.</summary>
    DownloadFailed,

    /// <summary>The downloaded file has the wrong size or checksum.</summary>
    WrongFile,

    /// <summary>The archive could not be extracted (damaged, disk full, access denied).</summary>
    ExtractFailed,

    /// <summary>faster-whisper-xxl.exe is missing after extraction (often removed by an antivirus program).</summary>
    ExeMissing,

    /// <summary>faster-whisper-xxl.exe exists but does not start.</summary>
    ExeDoesNotStart,
}

/// <summary>A setup step failed for a reason the user can act on.</summary>
public sealed class SetupException(SetupError error, string? detail = null, Exception? inner = null)
    : Exception(detail ?? error.ToString(), inner)
{
    public SetupError Error { get; } = error;

    /// <summary>Technical detail for the log.</summary>
    public string? Detail { get; } = detail;
}
