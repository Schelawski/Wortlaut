using System.Text.RegularExpressions;

namespace Wortlaut.Core.Gpu;

/// <summary>Why faster-whisper could not use the graphics card.</summary>
public enum CudaProblem
{
    None,

    /// <summary>No usable NVIDIA card (none present, wrong number, or too old for faster-whisper).</summary>
    NoDevice,

    /// <summary>The graphics driver is too old for the CUDA version of faster-whisper.</summary>
    DriverTooOld,

    /// <summary>The model does not fit into the graphics memory.</summary>
    OutOfMemory,

    /// <summary>A CUDA library (cuBLAS, cuDNN …) is missing or cannot be loaded.</summary>
    LibraryMissing,

    /// <summary>Any other CUDA error.</summary>
    Other,
}

/// <summary>Recognizes typical CUDA error messages in faster-whisper output.</summary>
/// <remarks>
/// faster-whisper reports these as a Python exception (e.g. <c>RuntimeError: CUDA failed with error
/// no CUDA-capable device is detected</c>), followed by a generic "Failed to execute script" line. The useful line is
/// therefore not the last one, so every output line is checked.
/// </remarks>
public static partial class CudaErrors
{
    [GeneratedRegex(@"driver version is insufficient|CUDA driver.{0,40}(too old|older than)|cudaErrorInsufficientDriver", RegexOptions.IgnoreCase)]
    private static partial Regex DriverTooOldRegex();

    [GeneratedRegex(
        @"no CUDA-capable device|invalid device ordinal|CUDA_ERROR_NO_DEVICE|not compiled with CUDA support|" +
        @"found no NVIDIA driver|float16 compute type, but the target device or backend do not support",
        RegexOptions.IgnoreCase)]
    private static partial Regex NoDeviceRegex();

    [GeneratedRegex(@"(CUDA|cuBLAS|cuDNN).{0,80}out of memory|out of memory.{0,80}CUDA|CUBLAS_STATUS_ALLOC_FAILED", RegexOptions.IgnoreCase)]
    private static partial Regex OutOfMemoryRegex();

    [GeneratedRegex(@"\b(cublas|cublasLt|cudnn|cudart|cufft)\w*\.dll", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryRegex();

    [GeneratedRegex(@"CUDA failed with error|CUDA error|cuBLAS failed|cuDNN failed|CUBLAS_STATUS_|CUDNN_STATUS_", RegexOptions.IgnoreCase)]
    private static partial Regex OtherRegex();

    /// <summary>Classifies one output line; <see cref="CudaProblem.None"/> if it is no CUDA error.</summary>
    public static CudaProblem Classify(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return CudaProblem.None;

        // A transcribed segment may contain anything, e.g. a lecture about "CUDA errors".
        if (WhisperOutputParser.TryParseSegmentEnd(line, out _))
            return CudaProblem.None;

        if (DriverTooOldRegex().IsMatch(line))
            return CudaProblem.DriverTooOld;
        if (NoDeviceRegex().IsMatch(line))
            return CudaProblem.NoDevice;
        if (OutOfMemoryRegex().IsMatch(line))
            return CudaProblem.OutOfMemory;
        if (LibraryRegex().IsMatch(line))
            return CudaProblem.LibraryMissing;
        if (OtherRegex().IsMatch(line))
            return CudaProblem.Other;
        return CudaProblem.None;
    }

    /// <summary>True for <c>cuda</c> and <c>cuda:1</c>.</summary>
    public static bool UsesCuda(string? device) =>
        device is not null && device.Trim().StartsWith("cuda", StringComparison.OrdinalIgnoreCase);
}
