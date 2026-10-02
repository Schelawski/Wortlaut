namespace Wortlaut.Core.Setup;

public enum SetupStage
{
    Downloading,
    Extracting,
    Verifying,
}

/// <summary>Progress of the setup.</summary>
/// <param name="Stage">Current step.</param>
/// <param name="Fraction">Progress of the step between 0 and 1, if known.</param>
/// <param name="Done">Bytes downloaded or extracted.</param>
/// <param name="Total">Total bytes of the step, if known.</param>
/// <param name="BytesPerSecond">Download speed (0 for other steps).</param>
/// <param name="Remaining">Estimated remaining download time.</param>
public sealed record SetupProgress(
    SetupStage Stage,
    double? Fraction,
    long Done = 0,
    long? Total = null,
    double BytesPerSecond = 0,
    TimeSpan? Remaining = null);

/// <summary>
/// Installs Faster-Whisper-XXL into <c>{root}\Faster-Whisper-XXL</c> (by default
/// <c>%LOCALAPPDATA%\Wortlaut</c>, so no administrator rights are needed):
/// download (resumable) → extract into a staging folder → check that the exe starts → move into place.
/// A half-extracted folder is never left behind; models of an earlier installation are kept.
/// </summary>
public sealed class FasterWhisperInstaller(ResumableDownloader downloader, IArchiveExtractor extractor, IFasterWhisperProbe probe)
{
    public const string FolderName = "Faster-Whisper-XXL";
    public const string DownloadsFolderName = "Downloads";
    private const string ModelsFolderName = "_models";

    /// <summary>Unpacked size relative to the archive, with some margin (r245.4: 1.36 GB → about 4.5 GB).</summary>
    private const double UnpackFactor = 3.6;

    /// <summary><c>%LOCALAPPDATA%\Wortlaut</c>.</summary>
    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wortlaut");

    public static string InstallDirectory(string root) => Path.Combine(root, FolderName);

    public static string ExePath(string root) => Path.Combine(InstallDirectory(root), FasterWhisperLocator.ExeName);

    public static string ArchivePath(string root, FasterWhisperPackage package) =>
        Path.Combine(root, DownloadsFolderName, package.FileName);

    /// <summary>Approximate size of the extracted package.</summary>
    public static long EstimateUnpackedSize(FasterWhisperPackage package) => (long)(package.Size * UnpackFactor);

    /// <summary>Free space needed for the remaining download plus the extracted files.</summary>
    public static long RequiredFreeSpace(string root, FasterWhisperPackage package)
    {
        var archive = ArchivePath(root, package);
        var downloaded = File.Exists(archive) ? new FileInfo(archive).Length
            : File.Exists(ResumableDownloader.PartPath(archive)) ? new FileInfo(ResumableDownloader.PartPath(archive)).Length
            : 0;
        return Math.Max(0, package.Size - downloaded) + EstimateUnpackedSize(package);
    }

    /// <summary>Free space on the drive of <paramref name="root"/>, or <c>null</c> if unknown.</summary>
    public static long? AvailableFreeSpace(string root)
    {
        try
        {
            return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(root))!).AvailableFreeSpace;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <returns>Full path of the installed faster-whisper-xxl.exe.</returns>
    /// <exception cref="SetupException">A step failed for a reason the user can act on.</exception>
    public async Task<string> InstallAsync(
        FasterWhisperPackage package,
        string root,
        IProgress<SetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        var required = RequiredFreeSpace(root, package);
        if (AvailableFreeSpace(root) is { } free && free < required)
            throw new SetupException(SetupError.NotEnoughSpace, $"{free} < {required}");

        // 1. Download (continues an interrupted download)
        var archive = ArchivePath(root, package);
        await downloader.DownloadAsync(
            package.DownloadUrl,
            archive,
            package.Size,
            package.Sha256,
            progress is null ? null : new InlineProgress<DownloadProgress>(p => progress.Report(
                new SetupProgress(SetupStage.Downloading, p.Fraction, p.Received, p.Total, p.BytesPerSecond, p.Remaining))),
            cancellationToken).ConfigureAwait(false);

        // 2. Extract into a staging folder
        var target = InstallDirectory(root);
        var staging = target + ".extracting";
        DeleteDirectory(staging);
        try
        {
            await extractor.ExtractAsync(
                archive,
                staging,
                progress is null ? null : new InlineProgress<ExtractProgress>(p => progress.Report(
                    new SetupProgress(SetupStage.Extracting, p.Fraction, p.Done, p.Total))),
                cancellationToken).ConfigureAwait(false);

            // The archive contains a top folder "Faster-Whisper-XXL"; be tolerant if that changes.
            var extractedExe = FindExe(staging)
                ?? throw new SetupException(SetupError.ExeMissing, staging);

            // 3. Check that it starts before replacing anything
            progress?.Report(new SetupProgress(SetupStage.Verifying, null));
            var libraryVersion = await probe.GetVersionAsync(extractedExe, cancellationToken).ConfigureAwait(false)
                ?? throw new SetupException(SetupError.ExeDoesNotStart, extractedExe);

            // Remember which release this is: "--version" only reports the faster-whisper library ("1.1.1"),
            // not the release ("r245.4") that an update check (issue #17) needs.
            InstalledRelease.Write(Path.GetDirectoryName(extractedExe)!, new InstalledRelease(package.Version, package.FileName, libraryVersion, DateTimeOffset.Now));

            // 4. Move into place
            MoveIntoPlace(Path.GetDirectoryName(extractedExe)!, target);
        }
        finally
        {
            DeleteDirectory(staging);
        }

        TryDeleteFile(archive);
        return ExePath(root);
    }

    private static string? FindExe(string directory) =>
        Directory.EnumerateFiles(directory, FasterWhisperLocator.ExeName, SearchOption.AllDirectories)
            .OrderBy(path => path.Length) // the shallowest one
            .FirstOrDefault();

    /// <summary>Replaces <paramref name="target"/> with <paramref name="extracted"/>, keeping downloaded models.</summary>
    private static void MoveIntoPlace(string extracted, string target)
    {
        if (Directory.Exists(target))
        {
            var oldModels = Path.Combine(target, ModelsFolderName);
            var newModels = Path.Combine(extracted, ModelsFolderName);
            if (Directory.Exists(oldModels) && !Directory.Exists(newModels))
                Directory.Move(oldModels, newModels);

            var old = target + ".old";
            DeleteDirectory(old);
            Directory.Move(target, old);
            Directory.Move(extracted, target);
            DeleteDirectory(old);
        }
        else
        {
            Directory.Move(extracted, target);
        }
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leftovers in %LOCALAPPDATA% are harmless and removed on the next attempt.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
