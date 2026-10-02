using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Wortlaut.Core.Setup;

/// <summary>Checks whether a faster-whisper-xxl.exe actually starts.</summary>
public interface IFasterWhisperProbe
{
    /// <returns>The version line (e.g. "faster-whisper-xxl.exe 1.1.0"), or <c>null</c> if it does not start properly.</returns>
    Task<string?> GetVersionAsync(string exePath, CancellationToken cancellationToken);
}

/// <summary>Runs <c>faster-whisper-xxl.exe --version</c>.</summary>
public sealed class FasterWhisperProbe : IFasterWhisperProbe
{
    // The first start after extraction can be slow while an antivirus program scans the files.
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public async Task<string?> GetVersionAsync(string exePath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("--version");
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var text = (await output.ConfigureAwait(false)).Trim();
            await error.ConfigureAwait(false);

            return process.ExitCode == 0 && text.Length > 0 ? text.Split('\n')[0].Trim() : null;
        }
        catch (Win32Exception)
        {
            return null; // e.g. blocked by an antivirus program
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // hangs
        }
        finally
        {
            if (process is { HasExited: false })
                process.Kill(entireProcessTree: true);
            process?.Dispose();
        }
    }
}
