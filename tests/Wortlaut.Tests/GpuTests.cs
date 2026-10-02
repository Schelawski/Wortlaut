using Wortlaut.Core;
using Wortlaut.Core.Gpu;

namespace Wortlaut.Tests;

public class GpuTests
{
    // Real output of Faster-Whisper-XXL r239.1 when CUDA cannot be used (captured with --device cuda:3
    // and with CUDA_VISIBLE_DEVICES=-1).
    private static readonly string[] InvalidDeviceOutput =
    [
        "Standalone Faster-Whisper-XXL r239.1 running on: CUDA",
        "",
        "Traceback (most recent call last):",
        "  File \"D:\\whisper-fast-XXL\\__main__.py\", line 2084, in <module>",
        "  File \"D:\\whisper-fast-XXL\\__main__.py\", line 1841, in cli",
        "  File \"faster_whisper\\transcribe.py\", line 781, in __init__",
        "RuntimeError: CUDA failed with error invalid device ordinal",
        "[PYI-6592:ERROR] Failed to execute script '__main__' due to unhandled exception!",
    ];

    private static readonly string[] NoDeviceOutput =
    [
        "Standalone Faster-Whisper-XXL r239.1 running on: CUDA",
        "Traceback (most recent call last):",
        "  File \"faster_whisper\\transcribe.py\", line 781, in __init__",
        "RuntimeError: CUDA failed with error no CUDA-capable device is detected",
        "[PYI-23968:ERROR] Failed to execute script '__main__' due to unhandled exception!",
    ];

    // ----- --checkcuda -----

    [Theory]
    [InlineData("CUDA device: 1\r\n", 1)]
    [InlineData("CUDA device: 0\n", 0)]
    [InlineData("CUDA device: 2", 2)]
    [InlineData("Standalone Faster-Whisper-XXL r239.1\nCUDA device: 1\n", 1)] // extra lines around it
    [InlineData("  CUDA device:   3  ", 3)]
    public void CheckCudaOutputIsParsed(string output, int expected)
    {
        Assert.Equal(expected, GpuAdvisor.ParseCudaDeviceCount(output));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("usage: faster-whisper-xxl [-h] ... error: unrecognized arguments: --checkcuda")]
    [InlineData("CUDA device: one")]
    [InlineData("CUDA device: -1")]
    [InlineData("Number of CUDA device: 1 (old)")] // not the expected line format
    public void UnexpectedCheckCudaOutputGivesNoCount(string? output)
    {
        Assert.Null(GpuAdvisor.ParseCudaDeviceCount(output));
    }

    // ----- nvidia-smi -----

    [Fact]
    public void NvidiaSmiOutputIsParsed()
    {
        var gpus = GpuAdvisor.ParseNvidiaSmi("NVIDIA GeForce GTX 1650, 4096\r\nNVIDIA RTX A2000, 12288\r\n");

        Assert.Equal([new GpuInfo("NVIDIA GeForce GTX 1650", 4096), new GpuInfo("NVIDIA RTX A2000", 12288)], gpus);
    }

    [Fact]
    public void NvidiaSmiWithUnknownMemoryKeepsTheName()
    {
        var gpus = GpuAdvisor.ParseNvidiaSmi("NVIDIA GeForce GT 710, [N/A]\n");

        Assert.Equal([new GpuInfo("NVIDIA GeForce GT 710", null)], gpus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NVIDIA-SMI has failed because it couldn't communicate with the NVIDIA driver.")]
    [InlineData(", 4096")]
    public void UnusableNvidiaSmiOutputGivesNoCards(string? output)
    {
        Assert.Empty(GpuAdvisor.ParseNvidiaSmi(output));
    }

    // ----- Recommendation -----

    [Fact]
    public void UsableCardSuggestsCudaWithLargeV2()
    {
        var result = new GpuCheckResult(1, [new GpuInfo("NVIDIA GeForce GTX 1650", 4096)]);

        Assert.Equal(new GpuRecommendation(GpuVerdict.Cuda, "cuda", "large-v2"), GpuAdvisor.Recommend(result));
    }

    [Fact]
    public void UsableCardWithoutNvidiaSmiSuggestsCudaWithLargeV2()
    {
        Assert.Equal(new GpuRecommendation(GpuVerdict.Cuda, "cuda", "large-v2"), GpuAdvisor.Recommend(new GpuCheckResult(1, [])));
    }

    [Fact]
    public void SeveralCardsSuggestCuda()
    {
        var result = new GpuCheckResult(2, [new GpuInfo("Small", 2048), new GpuInfo("Big", 8192)]);

        // The card with the most memory decides; the small one does not trigger the memory hint.
        Assert.Equal(new GpuRecommendation(GpuVerdict.Cuda, "cuda", "large-v2"), GpuAdvisor.Recommend(result));
        Assert.Equal("Big", result.MainGpu!.Name);
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    public void CardWithLittleMemorySuggestsSmallerModel(long memoryMiB)
    {
        var result = new GpuCheckResult(1, [new GpuInfo("NVIDIA GeForce GTX 1050", memoryMiB)]);

        Assert.Equal(new GpuRecommendation(GpuVerdict.CudaLowMemory, "cuda", "large-v3-turbo"), GpuAdvisor.Recommend(result));
    }

    [Fact]
    public void NoCudaDeviceSuggestsCpuWithSmallerModel()
    {
        // nvidia-smi may still list a card that faster-whisper cannot use (e.g. too old).
        var result = new GpuCheckResult(0, [new GpuInfo("NVIDIA GeForce GT 710", 2048)]);

        Assert.Equal(new GpuRecommendation(GpuVerdict.Cpu, "cpu", "large-v3-turbo"), GpuAdvisor.Recommend(result));
    }

    [Fact]
    public void FailedCheckSuggestsNothing()
    {
        Assert.Equal(new GpuRecommendation(GpuVerdict.Unknown, null, null), GpuAdvisor.Recommend(new GpuCheckResult(null, [])));
    }

    [Fact]
    public void SuggestedModelsAreKnownModels()
    {
        Assert.NotNull(Core.Models.WhisperModels.Find(GpuAdvisor.GpuModel));
        Assert.NotNull(Core.Models.WhisperModels.Find(GpuAdvisor.SmallerModel));
    }

    // ----- CUDA errors in the faster-whisper output -----

    [Theory]
    [InlineData("RuntimeError: CUDA failed with error no CUDA-capable device is detected", CudaProblem.NoDevice)]
    [InlineData("RuntimeError: CUDA failed with error invalid device ordinal", CudaProblem.NoDevice)]
    [InlineData("ValueError: This CTranslate2 package was not compiled with CUDA support", CudaProblem.NoDevice)]
    [InlineData("ValueError: Requested float16 compute type, but the target device or backend do not support efficient float16 computation.", CudaProblem.NoDevice)]
    [InlineData("RuntimeError: CUDA failed with error CUDA driver version is insufficient for CUDA runtime version", CudaProblem.DriverTooOld)]
    [InlineData("RuntimeError: CUDA failed with error out of memory", CudaProblem.OutOfMemory)]
    [InlineData("torch.cuda.OutOfMemoryError: CUDA out of memory. Tried to allocate 20.00 MiB", CudaProblem.OutOfMemory)]
    [InlineData("RuntimeError: cuBLAS failed with status CUBLAS_STATUS_ALLOC_FAILED", CudaProblem.OutOfMemory)]
    [InlineData("Could not load library cudnn_ops_infer64_8.dll. Error code 126", CudaProblem.LibraryMissing)]
    [InlineData("RuntimeError: Library cublas64_12.dll is not found or cannot be loaded", CudaProblem.LibraryMissing)]
    [InlineData("RuntimeError: cuBLAS failed with status CUBLAS_STATUS_NOT_SUPPORTED", CudaProblem.Other)]
    [InlineData("RuntimeError: CUDA failed with error unspecified launch failure", CudaProblem.Other)]
    public void CudaErrorsAreRecognized(string line, CudaProblem expected)
    {
        Assert.Equal(expected, CudaErrors.Classify(line));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Standalone Faster-Whisper-XXL r239.1 running on: CUDA")]
    [InlineData("[PYI-6592:ERROR] Failed to execute script '__main__' due to unhandled exception!")]
    [InlineData("Transcription speed: 3.72 audio seconds/s")]
    [InlineData("[00:03.120 --> 00:08.240]  Ошибка CUDA error out of memory – так он сказал.")] // transcribed speech
    [InlineData("MemoryError: out of memory")] // not the graphics card
    public void OtherLinesAreNoCudaErrors(string? line)
    {
        Assert.Equal(CudaProblem.None, CudaErrors.Classify(line));
    }

    [Theory]
    [InlineData("cuda", true)]
    [InlineData("cuda:1", true)]
    [InlineData(" CUDA ", true)]
    [InlineData("cpu", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void UsesCudaRecognizesDevices(string? device, bool expected)
    {
        Assert.Equal(expected, CudaErrors.UsesCuda(device));
    }

    // ----- Job and queue -----

    [Fact]
    public async Task FailedJobReportsCudaProblemWithTheUsefulLine()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia("Лекция.mp4");
        var job = new TranscriptionJob(new FakeWhisperRunner().FailsWithOutput(1, NoDeviceOutput));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Failed, result.Outcome);
        Assert.Equal(JobError.ProcessFailed, result.Error);
        Assert.Equal(CudaProblem.NoDevice, result.CudaProblem);
        // Not the generic "Failed to execute script" line that comes last.
        Assert.Equal("RuntimeError: CUDA failed with error no CUDA-capable device is detected", result.Detail);
    }

    [Fact]
    public async Task SpecificCudaErrorWinsOverEarlierGenericOne()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia("a.mp4");
        var job = new TranscriptionJob(new FakeWhisperRunner().FailsWithOutput(
            1,
            "Warning: CUDA error during warm-up",
            "RuntimeError: CUDA failed with error CUDA driver version is insufficient for CUDA runtime version",
            "RuntimeError: CUDA failed with error unspecified launch failure"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(CudaProblem.DriverTooOld, result.CudaProblem);
        Assert.Contains("driver version is insufficient", result.Detail);
    }

    [Fact]
    public async Task OtherFailuresHaveNoCudaProblem()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia("a.mp4");
        var job = new TranscriptionJob(new FakeWhisperRunner().Fails(1, "ValueError: Invalid input file"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(CudaProblem.None, result.CudaProblem);
        Assert.Equal("ValueError: Invalid input file", result.Detail);
    }

    [Fact]
    public async Task SuccessfulJobIgnoresCudaWords()
    {
        using var folder = new TempFolder();
        var media = folder.CreateMedia("a.mp4");
        var job = new TranscriptionJob(new FakeWhisperRunner().Succeeds("ok", null, "Note: CUDA error handling enabled"));

        var result = await job.RunAsync(new TranscriptionRequest(media, TestSettings.Create(), Overwrite: false), null, CancellationToken.None);

        Assert.Equal(JobOutcome.Completed, result.Outcome);
        Assert.Equal(CudaProblem.None, result.CudaProblem);
    }

    [Fact]
    public async Task CudaErrorStopsTheFolderRun()
    {
        using var folder = new TempFolder();
        var items = new[]
        {
            new BulkItem(folder.CreateMedia("Лекция 1.mp4")),
            new BulkItem(folder.CreateMedia("Лекция 2.mp4")),
            new BulkItem(folder.CreateMedia("Лекция 3.mp4")),
        };
        var runner = new FakeWhisperRunner().Succeeds("eins").FailsWithOutput(1, InvalidDeviceOutput).Succeeds("drei");
        var queue = new BulkQueue(new TranscriptionJob(runner));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: true, null, CancellationToken.None);

        Assert.Equal(2, runner.Requests.Count); // the third file was not started
        Assert.Equal((1, 0, 1, 1), (summary.Completed, summary.Skipped, summary.Failed, summary.Cancelled));
        Assert.Equal(CudaProblem.NoDevice, summary.CudaProblem);
        Assert.False(File.Exists(Path.Combine(folder.Path, "Лекция 3.txt")));
    }

    [Fact]
    public async Task OtherErrorsDoNotStopTheFolderRun()
    {
        using var folder = new TempFolder();
        var items = new[] { new BulkItem(folder.CreateMedia("1.mp4")), new BulkItem(folder.CreateMedia("2.mp4")) };
        var runner = new FakeWhisperRunner().Fails(1, "ValueError: Invalid input file").Succeeds("zwei");
        var queue = new BulkQueue(new TranscriptionJob(runner));

        var summary = await queue.RunAsync(items, TestSettings.Create(), skipExisting: true, null, CancellationToken.None);

        Assert.Equal((1, 1, 0), (summary.Completed, summary.Failed, summary.Cancelled));
        Assert.Equal(CudaProblem.None, summary.CudaProblem);
    }
}
