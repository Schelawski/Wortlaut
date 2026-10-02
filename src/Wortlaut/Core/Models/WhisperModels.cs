namespace Wortlaut.Core.Models;

/// <summary>A Whisper model Wortlaut knows how to download.</summary>
/// <param name="Name">Name passed to <c>--model</c>, e.g. "large-v2".</param>
/// <param name="Repository">Hugging Face repository Faster-Whisper-XXL downloads it from.</param>
/// <param name="ApproximateSize">Download size in bytes (shown before the download).</param>
public sealed record WhisperModelInfo(string Name, string Repository, long ApproximateSize);

/// <summary>
/// The models offered in Wortlaut and where Faster-Whisper-XXL keeps them.
/// </summary>
/// <remarks>
/// Faster-Whisper-XXL stores a model in <c>{folder of the exe}\_models\faster-whisper-{name}</c> and treats it as
/// present when <c>config.json</c>, <c>model.bin</c> and <c>tokenizer.json</c> exist there (checked in r245.4).
/// Repositories and sizes checked on Hugging Face on 2026-10-02.
/// </remarks>
public static class WhisperModels
{
    public const string ModelsFolderName = "_models";

    /// <summary>Files Faster-Whisper-XXL checks before it decides to download a model itself.</summary>
    public static IReadOnlyList<string> RequiredFiles { get; } = ["config.json", "model.bin", "tokenizer.json"];

    public static IReadOnlyList<WhisperModelInfo> All { get; } =
    [
        new("large-v2", "Systran/faster-whisper-large-v2", 3_089_381_313),
        new("large-v3", "Systran/faster-whisper-large-v3", 3_090_838_756),
        new("large-v3-turbo", "Purfview/faster-whisper-large-v3-turbo", 1_621_418_256),
        new("medium", "Systran/faster-whisper-medium", 1_530_372_137),
        new("small", "Systran/faster-whisper-small", 486_212_548),
    ];

    /// <summary>The known model with this name, or <c>null</c> for a name typed by the user.</summary>
    public static WhisperModelInfo? Find(string? name) =>
        All.FirstOrDefault(model => string.Equals(model.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary><c>{folder of faster-whisper-xxl.exe}\_models</c>.</summary>
    public static string ModelsDirectory(string exePath) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(exePath))!, ModelsFolderName);

    public static string ModelDirectory(string modelsDirectory, string name) =>
        Path.Combine(modelsDirectory, "faster-whisper-" + name);

    /// <summary>True when Faster-Whisper-XXL will find the model and not download it again.</summary>
    public static bool IsInstalled(string modelsDirectory, string name)
    {
        var directory = ModelDirectory(modelsDirectory, name);
        return RequiredFiles.All(file => File.Exists(Path.Combine(directory, file)));
    }

    /// <summary>Bytes the model occupies on disk (0 if it is not there).</summary>
    public static long SizeOnDisk(string modelsDirectory, string name)
    {
        var directory = ModelDirectory(modelsDirectory, name);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length)
            : 0;
    }

    /// <summary>Removes a model to free disk space.</summary>
    public static void Delete(string modelsDirectory, string name)
    {
        var directory = ModelDirectory(modelsDirectory, name);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
        ModelDownloader.DeleteIncomplete(modelsDirectory, name);
    }
}
