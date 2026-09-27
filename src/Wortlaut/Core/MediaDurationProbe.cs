using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Wortlaut.Core;

/// <summary>
/// Reads the duration of a media file.
/// </summary>
public interface IMediaDurationProbe
{
    /// <returns>The duration, or <c>null</c> if it cannot be determined.</returns>
    Task<TimeSpan?> GetDurationAsync(string mediaPath, CancellationToken cancellationToken);
}

/// <summary>
/// Reads the duration from the Windows property system (fast, no extra tools). If Windows has no
/// property handler for the format (e.g. some .ogg/.webm files), falls back to the ffmpeg.exe that ships
/// with Faster-Whisper-XXL.
/// </summary>
public sealed partial class MediaDurationProbe(Func<string?> ffmpegPathProvider) : IMediaDurationProbe
{
    private static readonly TimeSpan FfmpegTimeout = TimeSpan.FromSeconds(20);

    [GeneratedRegex(@"Duration:\s*(?<h>\d+):(?<m>\d{2}):(?<s>\d{2}(?:\.\d+)?)")]
    private static partial Regex FfmpegDurationRegex();

    /// <summary>ffmpeg.exe next to faster-whisper-xxl.exe, or <c>null</c> if there is none.</summary>
    public static string? FindFfmpegNextTo(string? fasterWhisperExePath)
    {
        if (string.IsNullOrWhiteSpace(fasterWhisperExePath))
            return null;

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(fasterWhisperExePath));
            if (directory is null)
                return null;

            var ffmpeg = Path.Combine(directory, "ffmpeg.exe");
            return File.Exists(ffmpeg) ? ffmpeg : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    public async Task<TimeSpan?> GetDurationAsync(string mediaPath, CancellationToken cancellationToken)
    {
        var fromShell = await Task.Run(() => TryGetFromPropertySystem(mediaPath), cancellationToken).ConfigureAwait(false);
        if (fromShell is not null)
            return fromShell;

        var ffmpeg = ffmpegPathProvider();
        return ffmpeg is null ? null : await TryGetFromFfmpegAsync(ffmpeg, mediaPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads <c>PKEY_Media_Duration</c> through <c>IPropertyStore</c>.</summary>
    internal static TimeSpan? TryGetFromPropertySystem(string mediaPath)
    {
        NativeMethods.IPropertyStore? store = null;
        try
        {
            // The shell only parses backslash paths.
            var parsingName = Path.GetFullPath(mediaPath);
            var iid = NativeMethods.IID_IPropertyStore;
            var hr = NativeMethods.SHGetPropertyStoreFromParsingName(parsingName, IntPtr.Zero, NativeMethods.GPS_DEFAULT, ref iid, out store);
            if (hr != 0 || store is null)
                return null;

            var key = NativeMethods.PKEY_Media_Duration;
            if (store.GetValue(ref key, out var value) != 0)
                return null;

            try
            {
                return value.VarType == NativeMethods.VT_UI8 && value.UInt64Value > 0
                    ? TimeSpan.FromTicks((long)value.UInt64Value) // 100 ns units, same as TimeSpan ticks
                    : null;
            }
            finally
            {
                NativeMethods.PropVariantClear(ref value);
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or ArgumentException or NotSupportedException)
        {
            // NotSupportedException: built-in COM is disabled (trimmed/AOT hosts). The caller falls back to ffmpeg.
            return null;
        }
        finally
        {
            // Release right away: the property handler keeps the media file open.
            if (store is not null)
                Marshal.FinalReleaseComObject(store);
        }
    }

    /// <summary>Runs <c>ffmpeg -i file</c> and reads "Duration: hh:mm:ss.xx" from its output.</summary>
    internal static async Task<TimeSpan?> TryGetFromFfmpegAsync(string ffmpegPath, string mediaPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-nostdin");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(mediaPath);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(FfmpegTimeout);

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return null;

            // ffmpeg prints the media information to stderr and exits with 1 because no output is given.
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }

            return ParseFfmpegDuration(await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null; // timeout
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    /// <summary>Parses the "Duration: 00:48:12.34" line of <c>ffmpeg -i</c>.</summary>
    public static TimeSpan? ParseFfmpegDuration(string? ffmpegOutput)
    {
        if (string.IsNullOrEmpty(ffmpegOutput))
            return null;

        var match = FfmpegDurationRegex().Match(ffmpegOutput);
        if (!match.Success)
            return null;

        var hours = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var seconds = double.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
        var duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        return duration > TimeSpan.Zero ? duration : null;
    }
}
