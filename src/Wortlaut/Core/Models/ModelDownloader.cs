using System.Text.Json;
using Wortlaut.Core.Setup;

namespace Wortlaut.Core.Models;

/// <summary>A file of a model repository.</summary>
/// <param name="Path">Path inside the repository, e.g. "model.bin".</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="Sha256">SHA-256 for large files stored with Git LFS (the model weights), otherwise <c>null</c>.</param>
public sealed record ModelFile(string Path, long Size, string? Sha256);

/// <summary>
/// Downloads a Whisper model from Hugging Face into the folder Faster-Whisper-XXL reads it from.
/// </summary>
/// <remarks>
/// All files go into <c>faster-whisper-{name}.download</c> first. Only when every file is complete and verified is
/// the folder renamed to <c>faster-whisper-{name}</c>, so Faster-Whisper-XXL never sees a half-downloaded model.
/// An interrupted download continues where it stopped.
/// </remarks>
public sealed class ModelDownloader(HttpClient http, ResumableDownloader downloader)
{
    public const string HuggingFaceUrl = "https://huggingface.co";
    private const string IncompleteSuffix = ".download";
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(20);

    // Repository files that are not part of the model.
    private static readonly HashSet<string> IgnoredFiles = new(StringComparer.OrdinalIgnoreCase) { ".gitattributes", "README.md" };

    public static string IncompleteDirectory(string modelsDirectory, string name) =>
        WhisperModels.ModelDirectory(modelsDirectory, name) + IncompleteSuffix;

    /// <summary>Removes a started but unfinished download.</summary>
    public static void DeleteIncomplete(string modelsDirectory, string name)
    {
        var incomplete = IncompleteDirectory(modelsDirectory, name);
        if (Directory.Exists(incomplete))
            Directory.Delete(incomplete, recursive: true);
    }

    /// <summary>Lists the model files of a repository (branch "main").</summary>
    /// <exception cref="SetupException">Hugging Face cannot be reached or the answer is not usable.</exception>
    public async Task<IReadOnlyList<ModelFile>> ListFilesAsync(string repository, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ApiTimeout);
        try
        {
            var json = await http.GetStringAsync($"{HuggingFaceUrl}/api/models/{repository}/tree/main", timeout.Token).ConfigureAwait(false);
            var files = ParseFileList(json);
            if (!WhisperModels.RequiredFiles.All(required => files.Any(file => file.Path == required)))
                throw new SetupException(SetupError.WrongFile, $"{repository} does not contain all model files.");
            return files;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SetupException(SetupError.DownloadFailed, "Hugging Face did not answer in time.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            throw new SetupException(SetupError.DownloadFailed, ex.Message, ex);
        }
    }

    /// <summary>Reads the file list of <c>/api/models/{repo}/tree/main</c>.</summary>
    internal static IReadOnlyList<ModelFile> ParseFileList(string json)
    {
        using var document = JsonDocument.Parse(json);
        var files = new List<ModelFile>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            if (entry.TryGetProperty("type", out var type) && type.GetString() != "file")
                continue;

            var path = entry.GetProperty("path").GetString();
            if (string.IsNullOrEmpty(path) || IgnoredFiles.Contains(path) || path.Contains('/'))
                continue;

            string? sha256 = null;
            if (entry.TryGetProperty("lfs", out var lfs) && lfs.ValueKind == JsonValueKind.Object && lfs.TryGetProperty("oid", out var oid))
                sha256 = oid.GetString();

            files.Add(new ModelFile(path, entry.GetProperty("size").GetInt64(), sha256));
        }

        return files;
    }

    /// <summary>Total download size of a model, from Hugging Face.</summary>
    public async Task<long> GetDownloadSizeAsync(WhisperModelInfo model, CancellationToken cancellationToken) =>
        (await ListFilesAsync(model.Repository, cancellationToken).ConfigureAwait(false)).Sum(file => file.Size);

    /// <summary>Downloads <paramref name="model"/> into <paramref name="modelsDirectory"/>.</summary>
    /// <exception cref="SetupException">Download failed, wrong file or not enough disk space.</exception>
    public async Task DownloadAsync(
        WhisperModelInfo model,
        string modelsDirectory,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var files = await ListFilesAsync(model.Repository, cancellationToken).ConfigureAwait(false);
        var total = files.Sum(file => file.Size);
        var incomplete = IncompleteDirectory(modelsDirectory, model.Name);
        Directory.CreateDirectory(incomplete);

        var remaining = files.Sum(file => Math.Max(0, file.Size - DownloadedBytes(incomplete, file)));
        if (FasterWhisperInstaller.AvailableFreeSpace(modelsDirectory) is { } free && free < remaining)
            throw new SetupException(SetupError.NotEnoughSpace, $"{free} < {remaining}");

        // Bytes of the files that are already finished, added to the progress of the current file.
        long finished = 0;
        foreach (var file in files)
        {
            var target = Path.Combine(incomplete, file.Path);
            var offset = finished;
            var fileProgress = progress is null
                ? null
                : new InlineProgress<DownloadProgress>(p => progress.Report(new DownloadProgress(offset + p.Received, total, p.BytesPerSecond)));

            var url = new Uri($"{HuggingFaceUrl}/{model.Repository}/resolve/main/{Uri.EscapeDataString(file.Path)}");
            await downloader.DownloadAsync(url, target, file.Size, file.Sha256, fileProgress, cancellationToken).ConfigureAwait(false);
            finished += file.Size;
        }

        progress?.Report(new DownloadProgress(total, total, 0));

        // Complete: make it visible to Faster-Whisper-XXL in one step.
        var directory = WhisperModels.ModelDirectory(modelsDirectory, model.Name);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true); // an incomplete copy faster-whisper started itself
        Directory.Move(incomplete, directory);
    }

    private static long DownloadedBytes(string directory, ModelFile file)
    {
        var path = Path.Combine(directory, file.Path);
        if (File.Exists(path))
            return new FileInfo(path).Length;
        var part = ResumableDownloader.PartPath(path);
        return File.Exists(part) ? new FileInfo(part).Length : 0;
    }
}
