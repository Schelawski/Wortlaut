using System.Runtime.InteropServices;

namespace Wortlaut.Core.Setup;

/// <summary>
/// Optional last step of the wizard: copies Wortlaut.exe to <c>%LOCALAPPDATA%\Wortlaut</c>, so it does not stay in
/// the downloads folder, and creates shortcuts on the desktop and in the start menu.
/// </summary>
public static class LocalInstall
{
    public const string ExeName = "Wortlaut.exe";
    public const string ShortcutName = "Wortlaut.lnk";

    /// <summary>Where the copy goes: <c>%LOCALAPPDATA%\Wortlaut\Wortlaut.exe</c>, next to Faster-Whisper-XXL.</summary>
    public static string TargetPath(string root) => Path.Combine(root, ExeName);

    /// <summary>
    /// Offered only for the published Wortlaut.exe (not for a development build) that is not already there.
    /// </summary>
    public static bool CanOffer(string? processPath, string root) =>
        processPath is not null
        && string.Equals(Path.GetFileName(processPath), ExeName, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Path.GetFullPath(processPath), Path.GetFullPath(TargetPath(root)), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Pre-selected when Wortlaut runs from a place where programs usually do not stay:
    /// the downloads folder, a temporary folder or the desktop.
    /// </summary>
    public static bool IsTemporaryLocation(string processPath, IEnumerable<string> temporaryFolders)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(processPath)) ?? string.Empty;
        return temporaryFolders
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Select(folder => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)))
            .Any(folder => directory.Equals(folder, StringComparison.OrdinalIgnoreCase)
                || directory.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Downloads, desktop and temp folder of the current user.</summary>
    public static IReadOnlyList<string> DefaultTemporaryFolders()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return
        [
            KnownFolderPath(DownloadsFolderId) ?? Path.Combine(profile, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Path.GetTempPath(),
        ];
    }

    /// <summary>
    /// Copies the running exe and the settings file into <paramref name="root"/>.
    /// </summary>
    /// <returns>Path of the copied exe.</returns>
    /// <exception cref="IOException">E.g. a running copy blocks the target.</exception>
    /// <exception cref="UnauthorizedAccessException">The target is not writable.</exception>
    public static string Copy(string processPath, string root, string? settingsFile)
    {
        Directory.CreateDirectory(root);
        var target = TargetPath(root);
        File.Copy(processPath, target, overwrite: true);

        // The copy reads its settings from its own folder, so it starts with everything the wizard set up.
        if (settingsFile is not null && File.Exists(settingsFile))
            File.Copy(settingsFile, Path.Combine(root, SettingsStore.FileName), overwrite: true);
        return target;
    }

    /// <summary>Creates (or replaces) "Wortlaut" on the desktop and in the start menu.</summary>
    /// <returns>The shortcut files that were created.</returns>
    public static IReadOnlyList<string> CreateShortcuts(string exePath)
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        };

        var created = new List<string>();
        foreach (var folder in folders.Where(folder => !string.IsNullOrWhiteSpace(folder)))
        {
            var path = Path.Combine(folder, ShortcutName);
            CreateShortcut(path, exePath);
            created.Add(path);
        }

        return created;
    }

    /// <summary>Writes a .lnk file through the Windows shell (IShellLink).</summary>
    /// <exception cref="IOException">The shortcut could not be written.</exception>
    public static void CreateShortcut(string shortcutPath, string targetPath)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(targetPath);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath)!);
            link.SetIconLocation(targetPath, 0);
            link.SetDescription("Wortlaut");
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
            ((IPersistFile)link).Save(shortcutPath, true);
        }
        catch (COMException ex)
        {
            throw new IOException(ex.Message, ex);
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    // ----- Windows shell interop -----

    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    private static string? KnownFolderPath(Guid folderId)
    {
        if (SHGetKnownFolderPath(folderId, 0, IntPtr.Zero, out var pointer) != 0)
            return null;

        try
        {
            return Marshal.PtrToStringUni(pointer);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder file, int maxPath, IntPtr findData, uint flags);
        void GetIDList(out IntPtr idList);
        void SetIDList(IntPtr idList);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder name, int maxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder dir, int maxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder args, int maxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int showCmd);
        void SetShowCmd(int showCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder iconPath, int iconPathLength, out int iconIndex);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string relativePath, uint reserved);
        void Resolve(IntPtr hwnd, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid classId);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }
}
