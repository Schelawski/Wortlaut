using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace Wortlaut.Core.Setup;

/// <summary>Extraction progress in uncompressed bytes.</summary>
public sealed record ExtractProgress(long Done, long Total)
{
    public double? Fraction => Total > 0 ? Math.Clamp((double)Done / Total, 0, 1) : null;
}

/// <summary>Extracts an archive into a folder.</summary>
public interface IArchiveExtractor
{
    /// <exception cref="SetupException">The archive is damaged or cannot be written.</exception>
    Task ExtractAsync(string archivePath, string targetDirectory, IProgress<ExtractProgress>? progress, CancellationToken cancellationToken);
}

/// <summary>
/// Extracts .7z archives with SharpCompress (managed code, so Wortlaut stays a single executable).
/// </summary>
public sealed class SevenZipExtractor : IArchiveExtractor
{
    private const int BufferSize = 1 << 20;
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(250);

    public Task ExtractAsync(string archivePath, string targetDirectory, IProgress<ExtractProgress>? progress, CancellationToken cancellationToken) =>
        Task.Run(() => Extract(archivePath, targetDirectory, progress, cancellationToken), cancellationToken);

    private static void Extract(string archivePath, string targetDirectory, IProgress<ExtractProgress>? progress, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(targetDirectory);
        Directory.CreateDirectory(root);

        try
        {
            using var archive = SevenZipArchive.OpenArchive(archivePath, new ReaderOptions());
            var total = archive.Entries.Where(entry => !entry.IsDirectory).Sum(entry => entry.Size);
            var done = 0L;
            var lastReport = DateTime.MinValue;
            var buffer = new byte[BufferSize];

            // Solid 7z archives must be read front to back; the reader does that in one pass.
            using var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = reader.Entry;
                if (entry.Key is null)
                    continue;

                var path = SafePath(root, entry.Key);
                if (entry.IsDirectory)
                {
                    Directory.CreateDirectory(path);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var input = reader.OpenEntryStream())
                using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
                {
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        output.Write(buffer, 0, read);
                        done += read;

                        if (DateTime.UtcNow - lastReport >= ReportInterval)
                        {
                            lastReport = DateTime.UtcNow;
                            progress?.Report(new ExtractProgress(done, total));
                        }
                    }
                }

                if (entry.LastModifiedTime is { } modified)
                    File.SetLastWriteTime(path, modified);
            }

            progress?.Report(new ExtractProgress(total, total));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SetupException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Damaged archive, unsupported compression, disk full, access denied, …
            throw new SetupException(SetupError.ExtractFailed, ex.Message, ex);
        }
    }

    /// <summary>Resolves an archive path below <paramref name="root"/> and rejects entries like "..\..\x.exe".</summary>
    internal static string SafePath(string root, string entryKey)
    {
        var path = Path.GetFullPath(Path.Combine(root, entryKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new SetupException(SetupError.ExtractFailed, $"Archive entry outside the target folder: {entryKey}");

        return path;
    }
}
