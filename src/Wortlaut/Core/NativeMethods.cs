using System.Runtime.InteropServices;

namespace Wortlaut.Core;

/// <summary>
/// Win32 functions used by the core. All string parameters are marshalled as UTF-16,
/// so Cyrillic and other non-ANSI paths work.
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// Creates a hard link <paramref name="lpFileName"/> that points to <paramref name="lpExistingFileName"/>.
    /// Only works on NTFS/ReFS and within one volume.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    /// <summary>Compares two strings the way Windows Explorer sorts file names ("2" before "12").</summary>
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int StrCmpLogicalW(string psz1, string psz2);

    // ----- Windows property system (used to read the media duration) -----

    internal const int GPS_DEFAULT = 0;
    internal const ushort VT_UI8 = 21;

    /// <summary>PKEY_Media_Duration: duration in 100 ns units (VT_UI8).</summary>
    internal static readonly PropertyKey PKEY_Media_Duration =
        new(new Guid("64440490-4C8B-11D1-8B70-080036B11A03"), 3);

    internal static readonly Guid IID_IPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int SHGetPropertyStoreFromParsingName(
        string pszPath,
        IntPtr pbc,
        int flags,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? ppv);

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropVariant pvar);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    /// <summary>
    /// Minimal PROPVARIANT: only the numeric part is read. The size covers the x64 layout (24 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct PropVariant
    {
        [FieldOffset(0)] public ushort VarType;
        [FieldOffset(8)] public ulong UInt64Value;
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PropertyKey pkey);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant pv);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant propvar);
        [PreserveSig] int Commit();
    }
}
