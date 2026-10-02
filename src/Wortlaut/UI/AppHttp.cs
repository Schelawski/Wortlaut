using System.Reflection;

namespace Wortlaut.UI;

/// <summary>
/// The one HttpClient of the app (reusing it avoids exhausting sockets). GitHub's API requires a User-Agent.
/// </summary>
internal static class AppHttp
{
    public static HttpClient Client { get; } = Create();

    private static HttpClient Create()
    {
        // No overall timeout: large downloads take minutes. Stalled connections are detected by the downloader.
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Wortlaut/{version} (+https://github.com/Schelawski/Wortlaut)");
        return client;
    }
}
