using System.Net;
using System.Security.Cryptography;
using System.Text;
using Wortlaut.Core.Setup;

namespace Wortlaut.Tests;

public class ReleaseFinderTests
{
    private const string ReleaseJson = """
        {
          "tag_name": "Faster-Whisper-XXL",
          "assets": [
            { "name": "Faster-Whisper-XXL_r192.3.1_linux.7z", "size": 897880936, "digest": null,
              "browser_download_url": "https://github.com/x/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r192.3.1_linux.7z" },
            { "name": "Faster-Whisper-XXL_r192.3.4_windows.7z", "size": 1141843875, "digest": null,
              "browser_download_url": "https://github.com/x/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r192.3.4_windows.7z" },
            { "name": "Faster-Whisper-XXL_r245.4_windows.7z", "size": 1424256246, "digest": "sha256:ABCDEF0123",
              "browser_download_url": "https://github.com/x/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r245.4_windows.7z" },
            { "name": "Faster-Whisper-XXL_r245.1_windows.7z", "size": 1453508359,
              "browser_download_url": "https://github.com/x/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r245.1_windows.7z" },
            { "name": "Faster-Whisper-XXL_r245.4_linux.7z", "size": 1657690937,
              "browser_download_url": "https://github.com/x/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r245.4_linux.7z" }
          ]
        }
        """;

    [Fact]
    public void NewestWindowsPackageIsSelected()
    {
        var package = FasterWhisperReleaseFinder.SelectLatest(ReleaseJson);

        Assert.NotNull(package);
        Assert.Equal("Faster-Whisper-XXL_r245.4_windows.7z", package.FileName);
        Assert.Equal("r245.4", package.Version);
        Assert.Equal(1424256246, package.Size);
        Assert.Equal("abcdef0123", package.Sha256);
        Assert.EndsWith("/Faster-Whisper-XXL_r245.4_windows.7z", package.DownloadUrl.AbsoluteUri);
    }

    [Fact]
    public void ReleaseWithoutWindowsPackageYieldsNothing()
    {
        Assert.Null(FasterWhisperReleaseFinder.SelectLatest("""{ "assets": [ { "name": "x_linux.7z", "size": 1, "browser_download_url": "https://x/y" } ] }"""));
        Assert.Null(FasterWhisperReleaseFinder.SelectLatest("""{ "message": "Not Found" }"""));
    }

    [Theory]
    [InlineData("Faster-Whisper-XXL_r245.4_windows.7z", "r245.4")]
    [InlineData("Faster-Whisper-XXL_r192.3.4_windows.7z", "r192.3.4")]
    [InlineData("Faster-Whisper-XXL_r250_windows.7z", "r250")]
    public void VersionIsReadFromTheFileName(string fileName, string expected)
    {
        Assert.True(FasterWhisperReleaseFinder.TryParseVersion(fileName, out _, out var label));
        Assert.Equal(expected, label);
    }

    [Theory]
    [InlineData("Faster-Whisper-XXL_r245.4_linux.7z")]
    [InlineData("Whisper-Faster_r192.3_windows.zip")]
    [InlineData("Faster-Whisper-XXL_r245.4_windows.7z.sha256")]
    public void OtherFilesAreIgnored(string fileName)
    {
        Assert.False(FasterWhisperReleaseFinder.TryParseVersion(fileName, out _, out _));
    }

    [Fact]
    public void VersionsAreComparedNumerically()
    {
        FasterWhisperReleaseFinder.TryParseVersion("Faster-Whisper-XXL_r245.10_windows.7z", out var newer, out _);
        FasterWhisperReleaseFinder.TryParseVersion("Faster-Whisper-XXL_r245.4_windows.7z", out var older, out _);

        Assert.True(newer > older);
    }

    [Fact]
    public async Task ApiResponseIsUsedWhenAvailable()
    {
        using var http = new HttpClient(new FakeHttpHandler((_, _) => FakeHttpHandler.Text(ReleaseJson)));

        var (package, isFallback) = await new FasterWhisperReleaseFinder(http).FindLatestAsync(CancellationToken.None);

        Assert.False(isFallback);
        Assert.Equal("r245.4", package.Version);
    }

    [Fact]
    public async Task KnownPackageIsUsedWhenTheApiFails()
    {
        using var rateLimited = new HttpClient(new FakeHttpHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Forbidden)));
        using var offline = new HttpClient(new FakeHttpHandler((_, _) => throw new HttpRequestException("No such host is known.")));

        var (fromRateLimit, fallback1) = await new FasterWhisperReleaseFinder(rateLimited).FindLatestAsync(CancellationToken.None);
        var (fromOffline, fallback2) = await new FasterWhisperReleaseFinder(offline).FindLatestAsync(CancellationToken.None);

        Assert.True(fallback1);
        Assert.True(fallback2);
        Assert.Equal(FasterWhisperReleaseFinder.KnownPackage, fromRateLimit);
        Assert.Equal(FasterWhisperReleaseFinder.KnownPackage, fromOffline);
    }
}

public class ResumableDownloaderTests
{
    private static readonly Uri Url = new("https://example.test/Faster-Whisper-XXL_r1_windows.7z");
    private static readonly byte[] Content = Enumerable.Range(0, 300_000).Select(i => (byte)(i % 251)).ToArray();

    private static ResumableDownloader Downloader(HttpClient http) => new(http) { RetryDelay = TimeSpan.Zero };

    [Fact]
    public async Task FileIsDownloadedAndRenamedWhenComplete()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "Downloads", "package.7z");
        var handler = FakeHttpHandler.ServingFile(Content);
        var progress = new CollectingProgress<DownloadProgress>();

        await Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, progress, CancellationToken.None);

        Assert.Equal(Content, File.ReadAllBytes(target));
        Assert.False(File.Exists(ResumableDownloader.PartPath(target)));
        Assert.Equal(1.0, progress.Items[^1].Fraction);
    }

    [Fact]
    public async Task InterruptedDownloadContinuesWithRangeRequest()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        File.WriteAllBytes(ResumableDownloader.PartPath(target), Content[..100_000]);
        var handler = FakeHttpHandler.ServingFile(Content);

        await Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, null, CancellationToken.None);

        Assert.Equal(Content, File.ReadAllBytes(target));
        Assert.Equal(100_000, Assert.Single(handler.RangeStarts));
    }

    [Fact]
    public async Task ServerWithoutRangeSupportRestartsFromTheBeginning()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        File.WriteAllBytes(ResumableDownloader.PartPath(target), Enumerable.Repeat((byte)0xFF, 100_000).ToArray());
        var handler = FakeHttpHandler.ServingFile(Content, supportsRange: false);

        await Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, null, CancellationToken.None);

        Assert.Equal(Content, File.ReadAllBytes(target));
    }

    [Fact]
    public async Task LostConnectionIsRetriedAndResumed()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        var handler = FakeHttpHandler.ServingFile(Content, failFirstResponseAfter: 120_000);

        await Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, null, CancellationToken.None);

        Assert.Equal(Content, File.ReadAllBytes(target));
        Assert.Equal(2, handler.Calls);
        Assert.Equal(120_000, Assert.Single(handler.RangeStarts));
    }

    [Fact]
    public async Task MissingFileFailsWithoutRetrying()
    {
        using var folder = new TempFolder();
        var handler = new FakeHttpHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            Downloader(new HttpClient(handler)).DownloadAsync(Url, Path.Combine(folder.Path, "x.7z"), 10, null, null, CancellationToken.None));

        Assert.Equal(SetupError.DownloadFailed, error.Error);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ServerErrorsAreRetriedThenReported()
    {
        using var folder = new TempFolder();
        var handler = new FakeHttpHandler((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            Downloader(new HttpClient(handler)).DownloadAsync(Url, Path.Combine(folder.Path, "x.7z"), 10, null, null, CancellationToken.None));

        Assert.Equal(SetupError.DownloadFailed, error.Error);
        Assert.Equal(4, handler.Calls);
    }

    [Fact]
    public async Task WrongSizeIsRejected()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        var handler = FakeHttpHandler.ServingFile(Content);

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length - 1, null, null, CancellationToken.None));

        Assert.Equal(SetupError.WrongFile, error.Error);
        Assert.False(File.Exists(target));
        Assert.False(File.Exists(ResumableDownloader.PartPath(target)));
    }

    [Fact]
    public async Task ChecksumIsVerifiedWhenKnown()
    {
        using var folder = new TempFolder();
        var good = Convert.ToHexStringLower(SHA256.HashData(Content));

        await Downloader(new HttpClient(FakeHttpHandler.ServingFile(Content)))
            .DownloadAsync(Url, Path.Combine(folder.Path, "good.7z"), Content.Length, good, null, CancellationToken.None);
        var error = await Assert.ThrowsAsync<SetupException>(() => Downloader(new HttpClient(FakeHttpHandler.ServingFile(Content)))
            .DownloadAsync(Url, Path.Combine(folder.Path, "bad.7z"), Content.Length, new string('0', 64), null, CancellationToken.None));

        Assert.True(File.Exists(Path.Combine(folder.Path, "good.7z")));
        Assert.Equal(SetupError.WrongFile, error.Error);
        Assert.False(File.Exists(Path.Combine(folder.Path, "bad.7z")));
    }

    [Fact]
    public async Task CompleteEarlierDownloadIsReused()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        File.WriteAllBytes(target, Content);
        var handler = FakeHttpHandler.ServingFile(Content);

        await Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, null, CancellationToken.None);

        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task CancelledDownloadKeepsThePartForLater()
    {
        using var folder = new TempFolder();
        var target = Path.Combine(folder.Path, "package.7z");
        using var cts = new CancellationTokenSource();
        var handler = FakeHttpHandler.ServingFile(Content, onBytesSent: sent => { if (sent >= 64 * 1024) cts.Cancel(); });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader(new HttpClient(handler)).DownloadAsync(Url, target, Content.Length, null, null, cts.Token));

        Assert.False(File.Exists(target));
        Assert.True(new FileInfo(ResumableDownloader.PartPath(target)).Length > 0);
    }
}

public class SevenZipExtractorTests
{
    private static string SampleArchive => Path.Combine(AppContext.BaseDirectory, "TestData", "faster-whisper-sample.7z");

    [Fact]
    public async Task ArchiveIsExtractedWithFoldersAndUnicodeNames()
    {
        using var folder = new TempFolder();
        var progress = new CollectingProgress<ExtractProgress>();

        await new SevenZipExtractor().ExtractAsync(SampleArchive, folder.Path, progress, CancellationToken.None);

        var root = Path.Combine(folder.Path, "Faster-Whisper-XXL");
        Assert.Equal("fake exe", File.ReadAllText(Path.Combine(root, "faster-whisper-xxl.exe")));
        Assert.Equal(50_000, new FileInfo(Path.Combine(root, "_xxl_data", "lib", "data.bin")).Length);
        Assert.True(File.Exists(Path.Combine(root, "Лицензия.txt")));
        Assert.Equal(1.0, progress.Items[^1].Fraction);
    }

    [Fact]
    public async Task DamagedArchiveIsReported()
    {
        using var folder = new TempFolder();
        var damaged = Path.Combine(folder.Path, "damaged.7z");
        var bytes = File.ReadAllBytes(SampleArchive);
        File.WriteAllBytes(damaged, bytes[..(bytes.Length / 2)]);

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            new SevenZipExtractor().ExtractAsync(damaged, Path.Combine(folder.Path, "out"), null, CancellationToken.None));

        Assert.Equal(SetupError.ExtractFailed, error.Error);
    }

    [Theory]
    [InlineData(@"..\evil.exe")]
    [InlineData("../../evil.exe")]
    [InlineData(@"Faster-Whisper-XXL\..\..\evil.exe")]
    public void EntriesOutsideTheTargetFolderAreRejected(string entry)
    {
        var error = Assert.Throws<SetupException>(() => SevenZipExtractor.SafePath(@"C:\Ziel", entry));

        Assert.Equal(SetupError.ExtractFailed, error.Error);
    }

    [Fact]
    public void NormalEntriesStayInsideTheTargetFolder()
    {
        Assert.Equal(@"C:\Ziel\Faster-Whisper-XXL\_xxl_data\a.dll", SevenZipExtractor.SafePath(@"C:\Ziel", "Faster-Whisper-XXL/_xxl_data/a.dll"));
    }
}

public class FasterWhisperInstallerTests
{
    private static readonly byte[] Archive =
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "faster-whisper-sample.7z"));

    private static readonly FasterWhisperPackage Package =
        new("Faster-Whisper-XXL_r1_windows.7z", "r1", Archive.Length, new Uri("https://example.test/p.7z"), null);

    private static FasterWhisperInstaller Installer(IFasterWhisperProbe probe) =>
        new(new ResumableDownloader(new HttpClient(FakeHttpHandler.ServingFile(Archive))) { RetryDelay = TimeSpan.Zero }, new SevenZipExtractor(), probe);

    [Fact]
    public async Task PackageIsInstalledAndTheArchiveRemoved()
    {
        using var folder = new TempFolder();
        var stages = new CollectingProgress<SetupProgress>();

        var exe = await Installer(new FakeProbe("faster-whisper-xxl.exe 1.1.1")).InstallAsync(Package, folder.Path, stages, CancellationToken.None);

        Assert.Equal(Path.Combine(folder.Path, "Faster-Whisper-XXL", "faster-whisper-xxl.exe"), exe);
        Assert.True(File.Exists(exe));
        Assert.True(File.Exists(Path.Combine(folder.Path, "Faster-Whisper-XXL", "_xxl_data", "lib", "data.bin")));
        Assert.False(File.Exists(FasterWhisperInstaller.ArchivePath(folder.Path, Package)));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "Faster-Whisper-XXL.extracting")));
        Assert.Equal(
            [SetupStage.Downloading, SetupStage.Extracting, SetupStage.Verifying],
            stages.Items.Select(s => s.Stage).Distinct());
    }

    [Fact]
    public async Task ExeThatDoesNotStartIsNotInstalled()
    {
        using var folder = new TempFolder();

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            Installer(new FakeProbe(null)).InstallAsync(Package, folder.Path, null, CancellationToken.None));

        Assert.Equal(SetupError.ExeDoesNotStart, error.Error);
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "Faster-Whisper-XXL")));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "Faster-Whisper-XXL.extracting")));
        // The download is kept so "try again" does not download 1.4 GB again.
        Assert.True(File.Exists(FasterWhisperInstaller.ArchivePath(folder.Path, Package)));
    }

    [Fact]
    public async Task ReinstallKeepsDownloadedModels()
    {
        using var folder = new TempFolder();
        var model = folder.CreateFile(Path.Combine("Faster-Whisper-XXL", "_models", "faster-whisper-large-v2", "model.bin"), "model");
        folder.CreateFile(Path.Combine("Faster-Whisper-XXL", "faster-whisper-xxl.exe"), "old exe");

        var exe = await Installer(new FakeProbe("1.1.1")).InstallAsync(Package, folder.Path, null, CancellationToken.None);

        Assert.Equal("fake exe", File.ReadAllText(exe));
        Assert.Equal("model", File.ReadAllText(model));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "Faster-Whisper-XXL.old")));
    }

    [Fact]
    public void RequiredSpaceShrinksWithTheDownloadedPart()
    {
        using var folder = new TempFolder();
        var package = Package with { Size = 1_000_000 };
        var before = FasterWhisperInstaller.RequiredFreeSpace(folder.Path, package);

        folder.CreateFile(Path.Combine("Downloads", package.FileName + ".part"), new string('x', 400_000));

        Assert.Equal(before - 400_000, FasterWhisperInstaller.RequiredFreeSpace(folder.Path, package));
    }

    private sealed class FakeProbe(string? version) : IFasterWhisperProbe
    {
        public Task<string?> GetVersionAsync(string exePath, CancellationToken cancellationToken) => Task.FromResult(version);
    }
}

/// <summary>Stands in for the internet in the setup tests.</summary>
internal sealed class FakeHttpHandler(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
{
    private int _calls;

    public int Calls => _calls;

    /// <summary>Start offsets of the range requests that were received.</summary>
    public List<long> RangeStarts { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Range?.Ranges.FirstOrDefault()?.From is { } from)
            RangeStarts.Add(from);
        return Task.FromResult(respond(request, Interlocked.Increment(ref _calls)));
    }

    public static HttpResponseMessage Text(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8) };

    /// <summary>A server for one file that understands range requests (unless told otherwise).</summary>
    public static FakeHttpHandler ServingFile(byte[] content, bool supportsRange = true, int? failFirstResponseAfter = null, Action<long>? onBytesSent = null) =>
        new((request, call) =>
        {
            var from = supportsRange && request.Headers.Range?.Ranges.FirstOrDefault()?.From is { } start ? start : 0;
            var body = content[(int)from..];
            Stream stream = new MemoryStream(body);
            if (call == 1 && failFirstResponseAfter is { } failAfter)
                stream = new FailingStream(stream, failAfter);
            if (onBytesSent is not null)
                stream = new ObservedStream(stream, onBytesSent);

            var response = new HttpResponseMessage(from > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentLength = body.Length;
            return response;
        });

    /// <summary>Throws like a dropped connection after a number of bytes.</summary>
    private sealed class FailingStream(Stream inner, long failAfter) : Stream
    {
        private long _read;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_read >= failAfter)
                throw new IOException("The connection was reset.");
            var n = inner.Read(buffer, offset, (int)Math.Min(count, failAfter - _read));
            _read += n;
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>Reports how many bytes have been read, in small chunks.</summary>
    private sealed class ObservedStream(Stream inner, Action<long> onBytesSent) : Stream
    {
        private long _read;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = inner.Read(buffer, offset, Math.Min(count, 16 * 1024));
            _read += n;
            onBytesSent(_read);
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
