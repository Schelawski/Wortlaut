using System.Collections.Concurrent;
using System.Security.Cryptography;
using Wortlaut.Core;

namespace Wortlaut.Tests;

/// <summary>A unique temporary folder that is deleted after the test.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Wortlaut.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>Creates a file with the given content (relative path, subfolders are created).</summary>
    public string CreateFile(string relativePath, string content = "media")
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    /// <summary>Creates a fake media file with some binary content and fixed timestamps.</summary>
    public string CreateMedia(string relativePath, int size = 64 * 1024)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        var bytes = new byte[size];
        new Random(size).NextBytes(bytes);
        File.WriteAllBytes(fullPath, bytes);

        var timestamp = new DateTime(2024, 5, 17, 8, 30, 0, DateTimeKind.Utc);
        File.SetCreationTimeUtc(fullPath, timestamp);
        File.SetLastWriteTimeUtc(fullPath, timestamp);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; the OS cleans the temp folder eventually.
        }
    }
}

/// <summary>Everything that identifies the state of a file, to prove the original stays untouched.</summary>
internal sealed record FileSnapshot(string FullName, long Length, DateTime CreationTimeUtc, DateTime LastWriteTimeUtc, FileAttributes Attributes, string Sha256)
{
    public static FileSnapshot Take(string path)
    {
        var info = new FileInfo(path);
        return new FileSnapshot(
            info.FullName,
            info.Length,
            info.CreationTimeUtc,
            info.LastWriteTimeUtc,
            info.Attributes,
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
    }
}

/// <summary>Collects progress reports synchronously.</summary>
internal sealed class CollectingProgress<T> : IProgress<T>
{
    private readonly ConcurrentQueue<T> _items = new();

    public IReadOnlyList<T> Items => _items.ToArray();

    public void Report(T value) => _items.Enqueue(value);
}

/// <summary>
/// Stands in for faster-whisper-xxl.exe. Each call runs the next configured behavior
/// (or the last one when there are more calls than behaviors).
/// </summary>
internal sealed class FakeWhisperRunner : IWhisperRunner
{
    private readonly List<Func<WhisperRunRequest, Action<OutputLine>, CancellationToken, Task<WhisperRunResult>>> _behaviors = [];
    private int _calls;

    public List<WhisperRunRequest> Requests { get; } = [];

    /// <summary>Content of the input file as seen by the runner, per call.</summary>
    public List<byte[]> InputContents { get; } = [];

    /// <summary>Completes when a run that waits for cancellation has started.</summary>
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Writes <c>job{extension}</c> into the output folder and exits with 0.</summary>
    public FakeWhisperRunner Succeeds(string content = "Transkript", string? producedExtension = null, params string[] outputLines)
    {
        _behaviors.Add(async (request, onOutput, _) =>
        {
            foreach (var line in outputLines)
                onOutput(new OutputLine(line, IsError: false));

            var extension = producedExtension ?? OutputFormats.Get(request.Settings.Format).ProducedExtensions[0];
            await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "job" + extension), content);
            return new WhisperRunResult(0, null);
        });
        return this;
    }

    /// <summary>Leaves a partial file behind and exits with an error code.</summary>
    public FakeWhisperRunner Fails(int exitCode, string errorLine)
    {
        _behaviors.Add(async (request, onOutput, _) =>
        {
            onOutput(new OutputLine(errorLine, IsError: true));
            await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "partial.tmp"), "partial");
            return new WhisperRunResult(exitCode, errorLine);
        });
        return this;
    }

    /// <summary>
    /// Writes the result, then crashes while shutting down (like Faster-Whisper-XXL with 0xC0000409).
    /// </summary>
    /// <param name="reportCompletion">Print the completion lines before crashing.</param>
    public FakeWhisperRunner WritesResultThenCrashes(int exitCode, bool reportCompletion, string content = "Transkript")
    {
        _behaviors.Add(async (request, onOutput, _) =>
        {
            var extension = OutputFormats.Get(request.Settings.Format).ProducedExtensions[0];
            await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "job" + extension), content);

            var lastLine = "[19:32.320 --> 19:32.480]  Аминь.";
            onOutput(new OutputLine(lastLine, IsError: false));
            if (reportCompletion)
            {
                onOutput(new OutputLine($"Subtitles are written to '{request.OutputDirectory}' directory.", IsError: false));
                lastLine = "Operation finished in:  0:08:32.858 ";
                onOutput(new OutputLine(lastLine, IsError: false));
            }

            return new WhisperRunResult(exitCode, lastLine.Trim());
        });
        return this;
    }

    /// <summary>Exits with 0 without writing a result.</summary>
    public FakeWhisperRunner SucceedsWithoutResult(string lastLine)
    {
        _behaviors.Add((_, _, _) => Task.FromResult(new WhisperRunResult(0, lastLine)));
        return this;
    }

    /// <summary>Runs until cancelled, like a long transcription.</summary>
    public FakeWhisperRunner RunsUntilCancelled()
    {
        _behaviors.Add(async (request, onOutput, cancellationToken) =>
        {
            onOutput(new OutputLine("[00:00.000 --> 00:05.000]  Начало", IsError: false));
            await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "partial.tmp"), "partial", CancellationToken.None);
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new WhisperRunResult(0, null);
        });
        return this;
    }

    /// <summary>Simulates a missing executable.</summary>
    public FakeWhisperRunner CannotStart()
    {
        _behaviors.Add((_, _, _) => throw new System.ComponentModel.Win32Exception(2, "Das System kann die angegebene Datei nicht finden."));
        return this;
    }

    public async Task<WhisperRunResult> RunAsync(WhisperRunRequest request, Action<OutputLine> onOutput, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        InputContents.Add(File.Exists(request.InputPath) ? await File.ReadAllBytesAsync(request.InputPath, cancellationToken) : []);

        var index = Math.Min(_calls++, _behaviors.Count - 1);
        return await _behaviors[index](request, onOutput, cancellationToken);
    }
}

/// <summary>Simulates a file system without hard link support (e.g. FAT32).</summary>
internal sealed class FailingHardLinker : IHardLinker
{
    public bool TryCreateHardLink(string linkPath, string existingPath, out string? error)
    {
        error = "Die Funktion ist ungültig.";
        return false;
    }
}

internal static class TestSettings
{
    public static WhisperSettings Create(OutputFormat format = OutputFormat.Text, string language = "ru") => new()
    {
        ExePath = @"C:\Tools\Faster-Whisper-XXL\faster-whisper-xxl.exe",
        Model = "large-v2",
        Device = "cuda",
        Language = language,
        Format = format,
    };
}
