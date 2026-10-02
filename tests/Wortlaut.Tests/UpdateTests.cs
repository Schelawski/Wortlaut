using System.Net;
using Wortlaut.Core;
using Wortlaut.Core.Updates;

namespace Wortlaut.Tests;

public class UpdateTests
{
    // ----- Versions -----

    [Theory]
    [InlineData("1.2.3", 1, 2, 3, null)]
    [InlineData("v1.2.3", 1, 2, 3, null)]
    [InlineData("v1.3.0-beta.1", 1, 3, 0, "beta.1")]
    [InlineData(" 10.0.12 ", 10, 0, 12, null)]
    public void VersionsAndTagsAreParsed(string text, int major, int minor, int patch, string? pre)
    {
        var parsed = AppVersion.Parse(text);

        Assert.NotNull(parsed);
        Assert.Equal(new Version(major, minor, patch), parsed.Value.Number);
        Assert.Equal(pre, parsed.Value.PreRelease);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.2")]
    [InlineData("latest")]
    [InlineData("v1.2.3.4")]
    [InlineData("1.2.3+abc")]
    public void InvalidVersionsAreRejected(string? text)
    {
        Assert.Null(AppVersion.Parse(text));
    }

    [Theory]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.1.0", "1.0.9", true)]
    [InlineData("2.0.0", "1.12.3", true)]
    [InlineData("1.10.0", "1.9.0", true)]     // numeric, not text comparison
    [InlineData("1.3.0", "1.3.0-beta.1", true)] // the final version after its pre-release
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.1", false)]
    [InlineData("1.3.0-beta.2", "1.3.0", false)]
    [InlineData("1.3.0-beta.2", "1.3.0-beta.1", false)] // pre-releases are not offered
    [InlineData("garbage", "1.0.0", false)]
    [InlineData("1.0.1", "garbage", false)]
    public void NewerVersionsAreRecognized(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, AppVersion.IsNewer(candidate, current));
    }

    [Fact]
    public void CurrentVersionHasNoCommitSuffix()
    {
        Assert.NotNull(AppVersion.Parse(AppVersion.Current));
        Assert.DoesNotContain("+", AppVersion.Current);
    }

    // ----- GitHub release -----

    [Fact]
    public void LatestReleaseIsParsed()
    {
        var release = UpdateChecker.ParseLatestRelease("""
            { "tag_name": "v1.3.0", "html_url": "https://github.com/Schelawski/Wortlaut/releases/tag/v1.3.0", "prerelease": false }
            """);

        Assert.Equal(new ReleaseInfo("1.3.0", new Uri("https://github.com/Schelawski/Wortlaut/releases/tag/v1.3.0")), release);
    }

    [Theory]
    [InlineData("""{ "tag_name": "v1.3.0", "html_url": "http://evil.example/download" }""")]
    [InlineData("""{ "tag_name": "v1.3.0", "html_url": "https://evil.example/download" }""")]
    [InlineData("""{ "tag_name": "v1.3.0" }""")]
    public void OnlyGitHubLinksAreOpened(string json)
    {
        var release = UpdateChecker.ParseLatestRelease(json);

        Assert.Equal(new Uri(UpdateChecker.ReleasesPageUrl), release!.PageUrl);
    }

    [Theory]
    [InlineData("""{ "message": "Not Found" }""")]
    [InlineData("""{ "tag_name": "nightly" }""")]
    [InlineData("[]")]
    public void UnusableReleasesAreIgnored(string json)
    {
        Assert.Null(UpdateChecker.ParseLatestRelease(json));
    }

    [Fact]
    public async Task NewerReleaseIsReported()
    {
        var checker = new UpdateChecker(new HttpClient(new FakeHandler(HttpStatusCode.OK, """{ "tag_name": "v9.0.0", "html_url": "https://github.com/Schelawski/Wortlaut/releases/tag/v9.0.0" }""")));

        var release = await checker.FindNewerReleaseAsync("1.0.0", CancellationToken.None);

        Assert.Equal("9.0.0", release?.Version);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{ "tag_name": "v1.0.0" }""")] // same version
    [InlineData(HttpStatusCode.NotFound, """{ "message": "Not Found" }""")] // nothing released yet
    [InlineData(HttpStatusCode.Forbidden, """{ "message": "API rate limit exceeded" }""")]
    [InlineData(HttpStatusCode.OK, "<html>captive portal</html>")]
    public async Task NoHintWithoutANewerRelease(HttpStatusCode status, string body)
    {
        var checker = new UpdateChecker(new HttpClient(new FakeHandler(status, body)));

        Assert.Null(await checker.FindNewerReleaseAsync("1.0.0", CancellationToken.None));
    }

    [Fact]
    public async Task OfflineIsNoError()
    {
        var checker = new UpdateChecker(new HttpClient(new FakeHandler(new HttpRequestException("No such host is known."))));

        Assert.Null(await checker.FindNewerReleaseAsync("1.0.0", CancellationToken.None));
    }

    [Fact]
    public void UpdateCheckIsOnByDefaultAlsoForOlderSettingsFiles()
    {
        using var folder = new TempFolder();
        folder.CreateFile(SettingsStore.FileName, "{ \"Model\": \"large-v2\" }");

        Assert.True(new SettingsStore(folder.Path, Path.Combine(folder.Path, "fallback")).Load().CheckForUpdates);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body = string.Empty;
        private readonly Exception? _error;

        public FakeHandler(HttpStatusCode status, string body) => (_status, _body) = (status, body);

        public FakeHandler(Exception error) => _error = error;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(UpdateChecker.LatestReleaseApiUrl, request.RequestUri?.AbsoluteUri);
            if (_error is not null)
                throw _error;
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
        }
    }
}
