using System.Net;
using System.Security.Cryptography;
using System.Text;
using Wortlaut.Core.Models;
using Wortlaut.Core.Setup;

namespace Wortlaut.Tests;

public class WhisperModelsTests
{
    [Theory]
    [InlineData("large-v2", "Systran/faster-whisper-large-v2")]
    [InlineData("large-v3", "Systran/faster-whisper-large-v3")]
    [InlineData("large-v3-turbo", "Purfview/faster-whisper-large-v3-turbo")] // XXL uses Purfview's conversion
    [InlineData("medium", "Systran/faster-whisper-medium")]
    [InlineData("small", "Systran/faster-whisper-small")]
    [InlineData(" LARGE-V2 ", "Systran/faster-whisper-large-v2")]
    public void KnownModelsMapToTheRepositoryFasterWhisperXxlUses(string name, string repository)
    {
        Assert.Equal(repository, WhisperModels.Find(name)?.Repository);
    }

    [Fact]
    public void EveryModelOfferedInTheUiCanBeDownloaded()
    {
        Assert.All(Core.WhisperSettings.KnownModels, name => Assert.NotNull(WhisperModels.Find(name)));
    }

    [Fact]
    public void UnknownNamesAreNotInTheCatalog()
    {
        Assert.Null(WhisperModels.Find("large-v4"));
        Assert.Null(WhisperModels.Find(null));
    }

    [Fact]
    public void ModelsLiveNextToTheExecutable()
    {
        var models = WhisperModels.ModelsDirectory(@"C:\Tools\Faster-Whisper-XXL\faster-whisper-xxl.exe");

        Assert.Equal(@"C:\Tools\Faster-Whisper-XXL\_models", models);
        Assert.Equal(@"C:\Tools\Faster-Whisper-XXL\_models\faster-whisper-large-v3-turbo", WhisperModels.ModelDirectory(models, "large-v3-turbo"));
    }

    [Fact]
    public void ModelCountsAsInstalledOnlyWithAllRequiredFiles()
    {
        using var folder = new TempFolder();
        folder.CreateFile(Path.Combine("faster-whisper-small", "config.json"), "{}");
        folder.CreateFile(Path.Combine("faster-whisper-small", "model.bin"), "weights");

        Assert.False(WhisperModels.IsInstalled(folder.Path, "small"));

        folder.CreateFile(Path.Combine("faster-whisper-small", "tokenizer.json"), "{}");
        Assert.True(WhisperModels.IsInstalled(folder.Path, "small"));
        Assert.True(WhisperModels.SizeOnDisk(folder.Path, "small") > 0);
    }

    [Fact]
    public void DeleteRemovesTheModelAndAnUnfinishedDownload()
    {
        using var folder = new TempFolder();
        folder.CreateFile(Path.Combine("faster-whisper-small", "model.bin"), "weights");
        folder.CreateFile(Path.Combine("faster-whisper-small.download", "model.bin.part"), "wei");

        WhisperModels.Delete(folder.Path, "small");

        Assert.False(Directory.Exists(Path.Combine(folder.Path, "faster-whisper-small")));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "faster-whisper-small.download")));
    }
}

public class ModelDownloaderTests
{
    private static readonly WhisperModelInfo Model = new("small", "Systran/faster-whisper-small", 1000);

    private static readonly Dictionary<string, byte[]> Files = new()
    {
        ["config.json"] = Encoding.UTF8.GetBytes("""{ "alignment_heads": [] }"""),
        ["model.bin"] = Enumerable.Range(0, 200_000).Select(i => (byte)(i % 199)).ToArray(),
        ["tokenizer.json"] = Encoding.UTF8.GetBytes("""{ "version": "1.0" }"""),
        ["vocabulary.txt"] = Encoding.UTF8.GetBytes("a\nb\nc"),
    };

    private static string TreeJson(string? modelSha = null)
    {
        var entries = new List<string>
        {
            """{"type":"file","oid":"c7d9","size":1477,"path":".gitattributes"}""",
            """{"type":"file","oid":"1651","size":1998,"path":"README.md"}""",
        };
        foreach (var (path, content) in Files)
        {
            var lfs = path == "model.bin"
                ? $$""","lfs":{"oid":"{{modelSha ?? Convert.ToHexStringLower(SHA256.HashData(content))}}","size":{{content.Length}},"pointerSize":134}"""
                : string.Empty;
            entries.Add($$"""{"type":"file","oid":"504a","size":{{content.Length}}{{lfs}},"path":"{{path}}"}""");
        }

        return "[" + string.Join(",", entries) + "]";
    }

    /// <summary>A small Hugging Face: the tree API and "resolve" downloads with range support.</summary>
    private static FakeHttpHandler HuggingFace(string treeJson, Action<long>? onModelBytes = null, List<string>? requested = null) =>
        new((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            requested?.Add(path);
            if (path == "/api/models/Systran/faster-whisper-small/tree/main")
                return FakeHttpHandler.Text(treeJson);

            const string prefix = "/Systran/faster-whisper-small/resolve/main/";
            if (!path.StartsWith(prefix) || !Files.TryGetValue(path[prefix.Length..], out var content))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var from = request.Headers.Range?.Ranges.FirstOrDefault()?.From ?? 0;
            var body = content[(int)from..];
            Stream stream = new MemoryStream(body);
            if (onModelBytes is not null && path.EndsWith("model.bin"))
                stream = new ThrottledStream(stream, onModelBytes);
            var response = new HttpResponseMessage(from > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = new StreamContent(stream) };
            response.Content.Headers.ContentLength = body.Length;
            return response;
        });

    private static ModelDownloader Downloader(FakeHttpHandler handler)
    {
        var http = new HttpClient(handler);
        return new ModelDownloader(http, new ResumableDownloader(http) { RetryDelay = TimeSpan.Zero });
    }

    [Fact]
    public void FileListSkipsRepositoryFilesAndReadsTheChecksum()
    {
        var files = ModelDownloader.ParseFileList(TreeJson());

        Assert.Equal(["config.json", "model.bin", "tokenizer.json", "vocabulary.txt"], files.Select(f => f.Path));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Files["model.bin"])), files.Single(f => f.Path == "model.bin").Sha256);
        Assert.Null(files.Single(f => f.Path == "config.json").Sha256);
    }

    [Fact]
    public async Task ModelIsDownloadedIntoTheFolderFasterWhisperReads()
    {
        using var folder = new TempFolder();
        var progress = new CollectingProgress<DownloadProgress>();
        var total = Files.Values.Sum(content => content.Length);

        await Downloader(HuggingFace(TreeJson())).DownloadAsync(Model, folder.Path, progress, CancellationToken.None);

        Assert.True(WhisperModels.IsInstalled(folder.Path, "small"));
        foreach (var (path, content) in Files)
            Assert.Equal(content, File.ReadAllBytes(Path.Combine(folder.Path, "faster-whisper-small", path)));
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "faster-whisper-small.download")));
        Assert.Equal(total, progress.Items[^1].Received);
        Assert.Equal(total, progress.Items[^1].Total);
        Assert.True(progress.Items.Select(p => p.Received).SequenceEqual(progress.Items.Select(p => p.Received).Order()), "progress must not go backwards");
    }

    [Fact]
    public async Task WrongChecksumIsRejectedAndTheModelIsNotInstalled()
    {
        using var folder = new TempFolder();

        var error = await Assert.ThrowsAsync<SetupException>(() =>
            Downloader(HuggingFace(TreeJson(modelSha: new string('0', 64)))).DownloadAsync(Model, folder.Path, null, CancellationToken.None));

        Assert.Equal(SetupError.WrongFile, error.Error);
        Assert.False(WhisperModels.IsInstalled(folder.Path, "small"));
    }

    [Fact]
    public async Task CancelledDownloadIsNotSeenAsInstalledAndResumesLater()
    {
        using var folder = new TempFolder();
        using var cts = new CancellationTokenSource();
        var requested = new List<string>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader(HuggingFace(TreeJson(), sent => { if (sent >= 50_000) cts.Cancel(); })).DownloadAsync(Model, folder.Path, null, cts.Token));

        Assert.False(WhisperModels.IsInstalled(folder.Path, "small"));
        Assert.True(Directory.Exists(ModelDownloader.IncompleteDirectory(folder.Path, "small")));

        var handler = HuggingFace(TreeJson(), requested: requested);
        await Downloader(handler).DownloadAsync(Model, folder.Path, null, CancellationToken.None);

        Assert.True(WhisperModels.IsInstalled(folder.Path, "small"));
        Assert.Equal(Files["model.bin"], File.ReadAllBytes(Path.Combine(folder.Path, "faster-whisper-small", "model.bin")));
        Assert.NotEmpty(handler.RangeStarts); // model.bin continued, not started over
        Assert.DoesNotContain(requested, path => path.EndsWith("/config.json")); // finished files are not loaded again
    }

    [Fact]
    public async Task IncompleteCopyFromFasterWhisperIsReplaced()
    {
        using var folder = new TempFolder();
        folder.CreateFile(Path.Combine("faster-whisper-small", "config.json"), "half");

        await Downloader(HuggingFace(TreeJson())).DownloadAsync(Model, folder.Path, null, CancellationToken.None);

        Assert.Equal(Files["config.json"], File.ReadAllBytes(Path.Combine(folder.Path, "faster-whisper-small", "config.json")));
    }

    [Fact]
    public async Task RepositoryWithoutModelFilesIsRejected()
    {
        using var folder = new TempFolder();
        var handler = new FakeHttpHandler((_, _) => FakeHttpHandler.Text("""[{"type":"file","size":10,"path":"README.md"}]"""));

        var error = await Assert.ThrowsAsync<SetupException>(() => Downloader(handler).DownloadAsync(Model, folder.Path, null, CancellationToken.None));

        Assert.Equal(SetupError.WrongFile, error.Error);
    }

    [Fact]
    public async Task UnreachableHuggingFaceIsReportedAsDownloadFailure()
    {
        using var folder = new TempFolder();
        var handler = new FakeHttpHandler((_, _) => throw new HttpRequestException("No such host is known."));

        var error = await Assert.ThrowsAsync<SetupException>(() => Downloader(handler).DownloadAsync(Model, folder.Path, null, CancellationToken.None));

        Assert.Equal(SetupError.DownloadFailed, error.Error);
    }

    /// <summary>Hands out data in small pieces and reports the bytes sent so far.</summary>
    private sealed class ThrottledStream(Stream inner, Action<long> onBytes) : Stream
    {
        private long _read;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = inner.Read(buffer, offset, Math.Min(count, 8 * 1024));
            _read += n;
            onBytes(_read);
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
