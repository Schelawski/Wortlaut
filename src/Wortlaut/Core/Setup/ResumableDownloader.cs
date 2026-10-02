using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Wortlaut.Core.Setup;

/// <summary>Download progress.</summary>
/// <param name="Received">Bytes on disk so far (including a resumed part).</param>
/// <param name="Total">Total size, if known.</param>
/// <param name="BytesPerSecond">Current speed, 0 while unknown.</param>
public sealed record DownloadProgress(long Received, long? Total, double BytesPerSecond)
{
    public double? Fraction => Total is > 0 ? Math.Clamp((double)Received / Total.Value, 0, 1) : null;

    public TimeSpan? Remaining =>
        Total is { } total && BytesPerSecond > 0 ? TimeSpan.FromSeconds((total - Received) / BytesPerSecond) : null;
}

/// <summary>
/// Downloads a large file into <c>{target}.part</c> and renames it when it is complete. An interrupted download
/// (cancelled, connection lost, app closed) continues where it stopped, using an HTTP range request.
/// </summary>
public sealed class ResumableDownloader(HttpClient http)
{
    private const int BufferSize = 1 << 16;
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>Automatic retries after a lost connection (each one resumes).</summary>
    public int MaxAttempts { get; init; } = 4;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>A connection that delivers no data for this long counts as lost.</summary>
    public TimeSpan StallTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Path of the incomplete download for <paramref name="targetPath"/>.</summary>
    public static string PartPath(string targetPath) => targetPath + ".part";

    /// <exception cref="SetupException">The download failed after all retries, or the file is not the expected one.</exception>
    public async Task DownloadAsync(
        Uri url,
        string targetPath,
        long? expectedSize,
        string? expectedSha256,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetPath))!);

        // A finished download from an earlier attempt (e.g. extraction failed afterwards) is reused.
        if (expectedSize is { } size && File.Exists(targetPath) && new FileInfo(targetPath).Length == size)
        {
            progress?.Report(new DownloadProgress(size, size, 0));
            return;
        }

        var partPath = PartPath(targetPath);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await DownloadOnceAsync(url, partPath, expectedSize, progress, cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException && !cancellationToken.IsCancellationRequested)
            {
                if (attempt >= MaxAttempts || IsPermanent(ex))
                    throw new SetupException(SetupError.DownloadFailed, ex.Message, ex);

                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        var actualSize = new FileInfo(partPath).Length;
        if (expectedSize is { } expected && actualSize != expected)
        {
            File.Delete(partPath);
            throw new SetupException(SetupError.WrongFile, $"{actualSize} != {expected} bytes");
        }

        if (expectedSha256 is not null)
        {
            var actualHash = await ComputeSha256Async(partPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partPath);
                throw new SetupException(SetupError.WrongFile, $"SHA-256 {actualHash} != {expectedSha256}");
            }
        }

        File.Move(partPath, targetPath, overwrite: true);
    }

    private async Task DownloadOnceAsync(
        Uri url,
        string partPath,
        long? expectedSize,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
        if (expectedSize is { } size && existing >= size)
        {
            if (existing == size)
                return; // complete, only the rename was missing
            File.Delete(partPath);
            existing = 0;
        }

        // Restarted after every block of data; fires only when the connection stalls.
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stall.CancelAfter(StallTimeout);
        try
        {
            await TransferAsync(url, partPath, existing, expectedSize, progress, stall, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException("The connection stalled.");
        }
    }

    private async Task TransferAsync(
        Uri url,
        string partPath,
        long existing,
        long? expectedSize,
        IProgress<DownloadProgress>? progress,
        CancellationTokenSource stall,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (existing > 0)
            request.Headers.Range = new RangeHeaderValue(existing, null);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stall.Token).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            // The part file does not fit the server's file: start over.
            File.Delete(partPath);
            throw new IOException("Range not satisfiable; restarting the download.");
        }

        response.EnsureSuccessStatusCode();

        // 206 = the server continues where we stopped; 200 = it sends the whole file again.
        var append = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (!append)
            existing = 0;

        var total = response.Content.Headers.ContentLength is { } length ? existing + length : expectedSize;

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var target = new FileStream(partPath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, FileOptions.Asynchronous);

        var buffer = new byte[BufferSize];
        var received = existing;
        var speed = new SpeedMeter();
        var sinceReport = Stopwatch.StartNew();
        progress?.Report(new DownloadProgress(received, total, 0));

        int read;
        while ((read = await source.ReadAsync(buffer, stall.Token).ConfigureAwait(false)) > 0)
        {
            stall.CancelAfter(StallTimeout);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;
            speed.Add(read);

            if (sinceReport.Elapsed >= ReportInterval)
            {
                sinceReport.Restart();
                progress?.Report(new DownloadProgress(received, total, speed.BytesPerSecond));
            }
        }

        await target.FlushAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(new DownloadProgress(received, total, speed.BytesPerSecond));

        if (total is { } expectedTotal && received < expectedTotal)
            throw new IOException($"Connection closed after {received} of {expectedTotal} bytes.");
    }

    /// <summary>4xx errors (except timeouts and rate limits) do not get better by retrying.</summary>
    private static bool IsPermanent(Exception ex) =>
        ex is HttpRequestException { StatusCode: { } status }
        && (int)status is >= 400 and < 500
        && status is not (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests);

    internal static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Average speed over the last few seconds.</summary>
    private sealed class SpeedMeter
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);
        private readonly Queue<(TimeSpan Time, long Bytes)> _samples = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _bytesInWindow;

        public void Add(long bytes)
        {
            var now = _clock.Elapsed;
            _samples.Enqueue((now, bytes));
            _bytesInWindow += bytes;
            while (_samples.Count > 1 && now - _samples.Peek().Time > Window)
                _bytesInWindow -= _samples.Dequeue().Bytes;
        }

        public double BytesPerSecond
        {
            get
            {
                if (_samples.Count < 2)
                    return 0;
                var span = (_clock.Elapsed - _samples.Peek().Time).TotalSeconds;
                return span > 0.5 ? _bytesInWindow / span : 0;
            }
        }
    }
}
