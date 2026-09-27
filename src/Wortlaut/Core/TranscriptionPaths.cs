namespace Wortlaut.Core;

/// <summary>
/// Path rules for transcripts and temporary work folders.
/// </summary>
public static class TranscriptionPaths
{
    /// <summary>Name of the folder that holds temporary work folders, created next to the media file.</summary>
    public const string TempFolderName = ".wortlaut-tmp";

    /// <summary>File name (without extension) of the hard link that faster-whisper transcribes.</summary>
    public const string JobFileBaseName = "job";

    /// <summary>
    /// Transcript path: same folder and name as the media file, with the extension of the format.
    /// Example: <c>D:\Videos\Лекция 12.mp4</c> → <c>D:\Videos\Лекция 12.txt</c>.
    /// </summary>
    public static string GetTargetPath(string mediaPath, OutputFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaPath);

        var fullPath = Path.GetFullPath(mediaPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The media path has no parent folder.", nameof(mediaPath));

        return Path.Combine(directory, Path.GetFileNameWithoutExtension(fullPath) + format.GetExtension());
    }

    /// <summary>The <c>.wortlaut-tmp</c> folder next to the media file.</summary>
    public static string GetTempRoot(string mediaPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(mediaPath))
            ?? throw new ArgumentException("The media path has no parent folder.", nameof(mediaPath));

        return Path.Combine(directory, TempFolderName);
    }

    /// <summary>Work folder of one job: <c>{media folder}\.wortlaut-tmp\{jobId}</c>.</summary>
    public static string GetWorkDirectory(string mediaPath, string jobId) =>
        Path.Combine(GetTempRoot(mediaPath), jobId);

    /// <summary>
    /// True when the transcript must not be created because it already exists and overwriting is off.
    /// </summary>
    public static bool ShouldSkip(string targetPath, bool overwrite) =>
        !overwrite && File.Exists(targetPath);
}
