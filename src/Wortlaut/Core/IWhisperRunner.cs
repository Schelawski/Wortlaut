namespace Wortlaut.Core;

/// <summary>
/// Runs faster-whisper for one input file. Abstracted so tests can run without the real executable.
/// </summary>
public interface IWhisperRunner
{
    /// <summary>
    /// Transcribes <see cref="WhisperRunRequest.InputPath"/> into <see cref="WhisperRunRequest.OutputDirectory"/>.
    /// </summary>
    /// <param name="request">What to run.</param>
    /// <param name="onOutput">Called for every stdout/stderr line, possibly from a background thread.</param>
    /// <param name="cancellationToken">Cancelling kills the process (including child processes).</param>
    /// <returns>Exit code and the last error line.</returns>
    /// <exception cref="OperationCanceledException">The run was cancelled; the process has exited.</exception>
    Task<WhisperRunResult> RunAsync(
        WhisperRunRequest request,
        Action<OutputLine> onOutput,
        CancellationToken cancellationToken);
}

/// <param name="Settings">faster-whisper settings, including the executable path.</param>
/// <param name="InputPath">Media file to transcribe.</param>
/// <param name="OutputDirectory">Folder for the output and working directory of the process.</param>
public sealed record WhisperRunRequest(WhisperSettings Settings, string InputPath, string OutputDirectory);

/// <param name="ExitCode">Process exit code.</param>
/// <param name="LastErrorLine">
/// Last non-empty stderr line, or the last stdout line if stderr stayed empty. <c>null</c> if there was no output.
/// </param>
public sealed record WhisperRunResult(int ExitCode, string? LastErrorLine);

/// <summary>One line of process output.</summary>
public readonly record struct OutputLine(string Text, bool IsError);
