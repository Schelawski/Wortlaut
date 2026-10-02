using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Wortlaut.Core;

/// <summary>Output of a short helper process.</summary>
public sealed record CapturedOutput(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Runs a short command-line tool without a window and collects its output, e.g.
/// <c>faster-whisper-xxl.exe --version</c> or <c>nvidia-smi</c>.
/// </summary>
public static class ProcessCapture
{
    /// <returns>The output, or <c>null</c> if the program could not be started or did not finish in time.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public static async Task<CapturedOutput?> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (Path.GetDirectoryName(fileName) is { Length: > 0 } directory)
            startInfo.WorkingDirectory = directory;
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);
            var error = process.StandardError.ReadToEndAsync(timeoutSource.Token);
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            return new CapturedOutput(process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
        }
        catch (Win32Exception)
        {
            return null; // not installed, or blocked by an antivirus program
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // hangs
        }
        finally
        {
            try
            {
                if (process is { HasExited: false })
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Exited in the meantime.
            }

            process?.Dispose();
        }
    }
}
