namespace Wortlaut.Core;

/// <summary>
/// Looks for faster-whisper-xxl.exe on first start.
/// </summary>
public static class FasterWhisperLocator
{
    public const string ExeName = "faster-whisper-xxl.exe";

    /// <summary>Name of the folder in the official Faster-Whisper-XXL archive.</summary>
    private const string ArchiveFolderName = "Faster-Whisper-XXL";

    /// <summary>
    /// Searches the folder of Wortlaut.exe, the current directory and the folder Wortlaut installs into
    /// (<c>%LOCALAPPDATA%\Wortlaut</c>), each also with a "Faster-Whisper-XXL" subfolder.
    /// </summary>
    public static string? FindDefault() =>
        Find([AppContext.BaseDirectory, Environment.CurrentDirectory, Setup.FasterWhisperInstaller.DefaultRoot]);

    /// <summary>Returns the first existing faster-whisper-xxl.exe in the given folders.</summary>
    public static string? Find(IEnumerable<string> directories)
    {
        foreach (var directory in directories.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            foreach (var candidate in new[]
                     {
                         Path.Combine(directory, ExeName),
                         Path.Combine(directory, ArchiveFolderName, ExeName),
                     })
            {
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    /// <summary>True when <paramref name="exePath"/> points to an existing file.</summary>
    public static bool Exists(string? exePath) =>
        !string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath.Trim());
}
