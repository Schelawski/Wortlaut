using System.Diagnostics;
using System.Text;

namespace Wortlaut.Core;

/// <summary>
/// Starts faster-whisper-xxl.exe directly (no cmd.exe, no batch file) and streams its output.
/// </summary>
public sealed class WhisperRunner : IWhisperRunner
{
    public async Task<WhisperRunResult> RunAsync(
        WhisperRunRequest request,
        Action<OutputLine> onOutput,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onOutput);
        cancellationToken.ThrowIfCancellationRequested();

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Settings.ExePath,
            WorkingDirectory = request.OutputDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = utf8,
            StandardErrorEncoding = utf8,
        };

        // Each argument is passed separately and quoted by .NET, so spaces and Cyrillic characters are safe.
        foreach (var argument in WhisperCommandLine.BuildArguments(request.Settings, request.InputPath, request.OutputDirectory))
            startInfo.ArgumentList.Add(argument);

        // Make Python write UTF-8 to the pipes and flush every line.
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var sync = new object();
        string? lastError = null;
        string? lastOutput = null;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            if (!string.IsNullOrWhiteSpace(e.Data))
                lock (sync) lastOutput = e.Data.Trim();
            onOutput(new OutputLine(e.Data, IsError: false));
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            if (!string.IsNullOrWhiteSpace(e.Data))
                lock (sync) lastError = e.Data.Trim();
            onOutput(new OutputLine(e.Data, IsError: true));
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            // Wait until the process is really gone so the work folder can be deleted.
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        // Make sure the asynchronous output handlers have processed everything.
        process.WaitForExit();

        lock (sync)
            return new WhisperRunResult(process.ExitCode, lastError ?? lastOutput);
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited in the meantime.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process is already terminating.
        }
    }
}
