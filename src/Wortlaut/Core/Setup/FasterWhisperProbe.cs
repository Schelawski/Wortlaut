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
        var output = await ProcessCapture.RunAsync(exePath, ["--version"], Timeout, cancellationToken).ConfigureAwait(false);
        var text = output?.StandardOutput.Trim() ?? string.Empty;
        return output is { ExitCode: 0 } && text.Length > 0 ? text.Split('\n')[0].Trim() : null;
    }
}
