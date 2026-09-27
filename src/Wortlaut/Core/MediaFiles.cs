namespace Wortlaut.Core;

/// <summary>
/// Finds supported media files and leftover work folders.
/// </summary>
public static class MediaFiles
{
    /// <summary>Supported media extensions (compared case-insensitively).</summary>
    public static IReadOnlyList<string> SupportedExtensions { get; } =
        [".mp4", ".mp3", ".ogg", ".m4a", ".mov", ".avi", ".wmv", ".webm", ".mpeg", ".m2p", ".mpg"];

    private static readonly HashSet<string> SupportedExtensionSet =
        new(SupportedExtensions, StringComparer.OrdinalIgnoreCase);

    /// <summary>Sorts paths like Windows Explorer does ("Лекция 2" before "Лекция 12").</summary>
    public static IComparer<string> NaturalOrder { get; } =
        Comparer<string>.Create((x, y) => NativeMethods.StrCmpLogicalW(x, y));

    public static bool IsSupported(string path) =>
        SupportedExtensionSet.Contains(Path.GetExtension(path));

    /// <summary>
    /// Lists the supported media files of <paramref name="folder"/>, sorted by name (relative path when
    /// subfolders are included). <c>.wortlaut-tmp</c> folders are ignored.
    /// </summary>
    public static IReadOnlyList<string> FindInFolder(string folder, bool includeSubfolders)
    {
        var root = Path.GetFullPath(folder);
        var files = new List<string>();

        foreach (var directory in EnumerateDirectories(root, includeSubfolders))
        {
            files.AddRange(Directory
                .EnumerateFiles(directory, "*", new EnumerationOptions { IgnoreInaccessible = true })
                .Where(IsSupported));
        }

        return files
            .OrderBy(path => Path.GetRelativePath(root, path), NaturalOrder)
            .ToList();
    }

    /// <summary>
    /// Lists <c>.wortlaut-tmp</c> folders below <paramref name="folder"/>. When no job is running, these are
    /// leftovers of an interrupted run. They are only reported, never deleted automatically.
    /// </summary>
    public static IReadOnlyList<string> FindTempFolders(string folder, bool includeSubfolders)
    {
        var root = Path.GetFullPath(folder);
        return EnumerateDirectories(root, includeSubfolders)
            .Select(directory => Path.Combine(directory, TranscriptionPaths.TempFolderName))
            .Where(Directory.Exists)
            .ToList();
    }

    /// <summary>The root folder and, if requested, all subfolders except <c>.wortlaut-tmp</c>.</summary>
    private static IEnumerable<string> EnumerateDirectories(string root, bool recursive)
    {
        yield return root;
        if (!recursive)
            yield break;

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            // Skip hidden/system folders and junctions (avoids loops).
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        };

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(current, "*", options))
            {
                if (string.Equals(Path.GetFileName(child), TranscriptionPaths.TempFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                yield return child;
                pending.Push(child);
            }
        }
    }
}
