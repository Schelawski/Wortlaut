using System.Globalization;
using System.Text.RegularExpressions;

namespace Wortlaut.Core.Gpu;

/// <summary>An NVIDIA graphics card as reported by <c>nvidia-smi</c>.</summary>
/// <param name="Name">E.g. "NVIDIA GeForce GTX 1650".</param>
/// <param name="MemoryMiB">Graphics memory in MiB, or <c>null</c> if unknown.</param>
public sealed record GpuInfo(string Name, long? MemoryMiB);

/// <summary>Result of the graphics card check.</summary>
/// <param name="CudaDeviceCount">
/// Number of CUDA devices faster-whisper can use (<c>--checkcuda</c>), or <c>null</c> if the check failed.
/// </param>
/// <param name="Gpus">Cards found by <c>nvidia-smi</c>; empty if it is not installed. Only used for display.</param>
public sealed record GpuCheckResult(int? CudaDeviceCount, IReadOnlyList<GpuInfo> Gpus)
{
    /// <summary>The card with the most memory, used for the display and the memory hint.</summary>
    public GpuInfo? MainGpu => Gpus.OrderByDescending(gpu => gpu.MemoryMiB ?? 0).FirstOrDefault();
}

/// <summary>What the check means for the user.</summary>
public enum GpuVerdict
{
    /// <summary>A usable NVIDIA card: fast recognition with <c>cuda</c>.</summary>
    Cuda,

    /// <summary>A usable NVIDIA card with little memory: the large models may not fit.</summary>
    CudaLowMemory,

    /// <summary>No usable card: recognition runs on the processor (slower).</summary>
    Cpu,

    /// <summary>The check itself failed (e.g. faster-whisper did not start); nothing is suggested.</summary>
    Unknown,
}

/// <summary>Suggested settings. <see cref="Device"/> and <see cref="Model"/> are <c>null</c> for <see cref="GpuVerdict.Unknown"/>.</summary>
public sealed record GpuRecommendation(GpuVerdict Verdict, string? Device, string? Model);

/// <summary>Parses the check output and turns it into a suggestion.</summary>
public static partial class GpuAdvisor
{
    /// <summary>Model for a fast card with enough memory: the most accurate one for Russian.</summary>
    public const string GpuModel = "large-v2";

    /// <summary>
    /// Model without a usable card, and for cards with little memory. Almost as accurate as large-v2 but much faster,
    /// and on the processor also faster than "medium" (it has far fewer decoder layers).
    /// </summary>
    public const string SmallerModel = "large-v3-turbo";

    /// <summary>
    /// Below this the large models may not fit into the graphics memory. 4 GB cards report about 4096 MiB,
    /// some a little less, so the limit sits below that.
    /// </summary>
    public const long LowMemoryLimitMiB = 3800;

    /// <summary><c>CUDA device: 1</c>, printed by <c>faster-whisper-xxl.exe --checkcuda</c>.</summary>
    [GeneratedRegex(@"^\s*CUDA device:\s*(?<count>\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex CudaDeviceRegex();

    /// <summary>Reads the number of CUDA devices; <c>null</c> for unexpected output.</summary>
    public static int? ParseCudaDeviceCount(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        var match = CudaDeviceRegex().Match(output);
        return match.Success && int.TryParse(match.Groups["count"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : null;
    }

    /// <summary>
    /// Parses <c>nvidia-smi --query-gpu=name,memory.total --format=csv,noheader,nounits</c>,
    /// one line per card, e.g. <c>NVIDIA GeForce GTX 1650, 4096</c>.
    /// </summary>
    public static IReadOnlyList<GpuInfo> ParseNvidiaSmi(string? output)
    {
        var gpus = new List<GpuInfo>();
        if (string.IsNullOrEmpty(output))
            return gpus;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            // The memory is the last value; split there in case a name ever contains a comma.
            var comma = line.LastIndexOf(',');
            if (comma <= 0)
                continue;

            var name = line[..comma].Trim();
            if (name.Length == 0)
                continue;

            long? memory = long.TryParse(line[(comma + 1)..].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var mib) && mib > 0
                ? mib
                : null; // e.g. "[N/A]"
            gpus.Add(new GpuInfo(name, memory));
        }

        return gpus;
    }

    public static GpuRecommendation Recommend(GpuCheckResult result) => result.CudaDeviceCount switch
    {
        null => new GpuRecommendation(GpuVerdict.Unknown, null, null),
        0 => new GpuRecommendation(GpuVerdict.Cpu, "cpu", SmallerModel),
        _ when result.MainGpu?.MemoryMiB is { } memory && memory < LowMemoryLimitMiB =>
            new GpuRecommendation(GpuVerdict.CudaLowMemory, "cuda", SmallerModel),
        _ => new GpuRecommendation(GpuVerdict.Cuda, "cuda", GpuModel),
    };
}

/// <summary>Checks which graphics card faster-whisper can use.</summary>
public interface IGpuProbe
{
    Task<GpuCheckResult> CheckAsync(string exePath, CancellationToken cancellationToken);
}

/// <summary>Runs <c>faster-whisper-xxl.exe --checkcuda</c> and, if available, <c>nvidia-smi</c>.</summary>
public sealed class GpuProbe : IGpuProbe
{
    // Starting faster-whisper can take a while (antivirus scan after the setup).
    private static readonly TimeSpan CheckCudaTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan NvidiaSmiTimeout = TimeSpan.FromSeconds(20);

    public async Task<GpuCheckResult> CheckAsync(string exePath, CancellationToken cancellationToken)
    {
        // nvidia-smi only provides the name and the memory; whether faster-whisper can use the card
        // is decided by --checkcuda alone.
        var checkCuda = ProcessCapture.RunAsync(exePath, ["--checkcuda"], CheckCudaTimeout, cancellationToken);
        var nvidiaSmi = ProcessCapture.RunAsync(
            "nvidia-smi.exe",
            ["--query-gpu=name,memory.total", "--format=csv,noheader,nounits"],
            NvidiaSmiTimeout,
            cancellationToken);

        var cuda = await checkCuda.ConfigureAwait(false);
        var smi = await nvidiaSmi.ConfigureAwait(false);

        var count = cuda is null ? null : GpuAdvisor.ParseCudaDeviceCount(cuda.StandardOutput + "\n" + cuda.StandardError);
        var gpus = smi is { ExitCode: 0 } ? GpuAdvisor.ParseNvidiaSmi(smi.StandardOutput) : [];
        return new GpuCheckResult(count, gpus);
    }
}
