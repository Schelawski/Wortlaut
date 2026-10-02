using System.Text.Json;

namespace Wortlaut.Core.Setup;

/// <summary>
/// Which Faster-Whisper-XXL release Wortlaut installed, stored as <c>wortlaut-install.json</c> next to
/// faster-whisper-xxl.exe. Only installations made by Wortlaut have this file; an installation chosen by the user
/// does not, so it is never treated as Wortlaut's own (e.g. for an update).
/// </summary>
/// <param name="Release">Release of the package, e.g. "r245.4".</param>
/// <param name="PackageFileName">E.g. "Faster-Whisper-XXL_r245.4_windows.7z".</param>
/// <param name="LibraryVersion">Output of <c>--version</c>, e.g. "faster-whisper-xxl.exe 1.1.1".</param>
/// <param name="InstalledAt">When the installation was finished.</param>
public sealed record InstalledRelease(string Release, string PackageFileName, string LibraryVersion, DateTimeOffset InstalledAt)
{
    public const string FileName = "wortlaut-install.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Writes the file into the folder of faster-whisper-xxl.exe.</summary>
    public static void Write(string installDirectory, InstalledRelease release) =>
        File.WriteAllText(Path.Combine(installDirectory, FileName), JsonSerializer.Serialize(release, JsonOptions));

    /// <summary>
    /// Reads the file next to <paramref name="exePath"/>; <c>null</c> for an installation not made by Wortlaut
    /// (or an unreadable file).
    /// </summary>
    public static InstalledRelease? Read(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || Path.GetDirectoryName(exePath.Trim()) is not { Length: > 0 } directory)
            return null;

        try
        {
            var path = Path.Combine(directory, FileName);
            if (!File.Exists(path))
                return null;

            var release = JsonSerializer.Deserialize<InstalledRelease>(File.ReadAllText(path));
            return string.IsNullOrWhiteSpace(release?.Release) ? null : release;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }
}
