using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wortlaut.Core.Setup;

/// <summary>A downloadable Faster-Whisper-XXL package for Windows.</summary>
/// <param name="FileName">E.g. <c>Faster-Whisper-XXL_r245.4_windows.7z</c>.</param>
/// <param name="Version">Release label, e.g. <c>r245.4</c>.</param>
/// <param name="Size">Exact size in bytes (used to verify the download).</param>
/// <param name="DownloadUrl">Official download address on GitHub.</param>
/// <param name="Sha256">SHA-256 as hex, if GitHub provides it.</param>
public sealed record FasterWhisperPackage(string FileName, string Version, long Size, Uri DownloadUrl, string? Sha256);

/// <summary>
/// Finds the newest Faster-Whisper-XXL package in the official GitHub release of
/// <c>Purfview/whisper-standalone-win</c>. All versions are attached to the same release (tag
/// <c>Faster-Whisper-XXL</c>); the version number is part of the file name.
/// </summary>
public sealed partial class FasterWhisperReleaseFinder(HttpClient http)
{
    public const string ReleaseApiUrl =
        "https://api.github.com/repos/Purfview/whisper-standalone-win/releases/tags/Faster-Whisper-XXL";

    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// SHA-256 of packages GitHub has no digest for (uploaded before mid-2025). Computed from the official download
    /// on 2026-10-02 and confirmed with two tools; a changed or damaged file is then rejected.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> KnownSha256 { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Faster-Whisper-XXL_r245.4_windows.7z"] = "237dee23939cdabfc96ef859fc5e584b842c3a5557e0d2ca744e1f87c14c5844",
    };

    /// <summary>Used when the GitHub API cannot be reached (offline, rate limit). Checked on 2026-10-02.</summary>
    public static FasterWhisperPackage KnownPackage { get; } = new(
        "Faster-Whisper-XXL_r245.4_windows.7z",
        "r245.4",
        1_424_256_246,
        new Uri("https://github.com/Purfview/whisper-standalone-win/releases/download/Faster-Whisper-XXL/Faster-Whisper-XXL_r245.4_windows.7z"),
        "237dee23939cdabfc96ef859fc5e584b842c3a5557e0d2ca744e1f87c14c5844");

    [GeneratedRegex(@"^Faster-Whisper-XXL_r(?<version>\d+(?:\.\d+)*)_windows\.7z$", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsPackageRegex();

    /// <summary>
    /// The newest Windows package, or <see cref="KnownPackage"/> if the release cannot be read.
    /// </summary>
    /// <returns>The package and whether it is the built-in fallback.</returns>
    public async Task<(FasterWhisperPackage Package, bool IsFallback)> FindLatestAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ApiTimeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseApiUrl);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await http.SendAsync(request, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            return SelectLatest(json) is { } package ? (package, false) : (KnownPackage, true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (KnownPackage, true); // API timeout
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return (KnownPackage, true);
        }
    }

    /// <summary>Picks the Windows package with the highest version from a GitHub release JSON.</summary>
    internal static FasterWhisperPackage? SelectLatest(string releaseJson)
    {
        using var document = JsonDocument.Parse(releaseJson);
        if (!document.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        FasterWhisperPackage? best = null;
        Version? bestVersion = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name is null || !TryParseVersion(name, out var version, out var label))
                continue;

            if (!asset.TryGetProperty("browser_download_url", out var url)
                || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var downloadUrl)
                || !asset.TryGetProperty("size", out var size))
            {
                continue;
            }

            if (bestVersion is null || version > bestVersion)
            {
                bestVersion = version;
                best = new FasterWhisperPackage(name, label, size.GetInt64(), downloadUrl, ParseSha256(asset) ?? KnownSha256.GetValueOrDefault(name));
            }
        }

        return best;
    }

    /// <summary>Reads "r245.4" from <c>Faster-Whisper-XXL_r245.4_windows.7z</c>.</summary>
    internal static bool TryParseVersion(string fileName, out Version version, out string label)
    {
        version = new Version();
        label = string.Empty;

        var match = WindowsPackageRegex().Match(fileName);
        if (!match.Success)
            return false;

        var digits = match.Groups["version"].Value;
        // System.Version needs at least two parts ("245" -> "245.0").
        if (!Version.TryParse(digits.Contains('.') ? digits : digits + ".0", out var parsed))
            return false;

        version = parsed;
        label = "r" + digits;
        return true;
    }

    /// <summary>GitHub provides <c>"digest": "sha256:…"</c> for assets uploaded since mid-2025, otherwise <c>null</c>.</summary>
    private static string? ParseSha256(JsonElement asset)
    {
        if (!asset.TryGetProperty("digest", out var digest) || digest.ValueKind != JsonValueKind.String)
            return null;

        var value = digest.GetString();
        const string prefix = "sha256:";
        return value is not null && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..].ToLowerInvariant()
            : null;
    }
}
