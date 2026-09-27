using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Wortlaut.Core;

/// <summary>
/// Creates hard links. Abstracted so tests can simulate file systems without hard link support.
/// </summary>
public interface IHardLinker
{
    /// <summary>
    /// Tries to create <paramref name="linkPath"/> as a hard link to <paramref name="existingPath"/>.
    /// </summary>
    /// <param name="error">Reason for the failure, or <c>null</c> on success.</param>
    bool TryCreateHardLink(string linkPath, string existingPath, out string? error);
}

/// <summary>
/// Hard links via <c>CreateHardLinkW</c>.
/// </summary>
public sealed class NativeHardLinker : IHardLinker
{
    public bool TryCreateHardLink(string linkPath, string existingPath, out string? error)
    {
        if (NativeMethods.CreateHardLink(linkPath, existingPath, IntPtr.Zero))
        {
            error = null;
            return true;
        }

        error = new Win32Exception(Marshal.GetLastPInvokeError()).Message;
        return false;
    }
}
