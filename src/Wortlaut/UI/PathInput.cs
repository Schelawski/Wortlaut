namespace Wortlaut.UI;

/// <summary>
/// Normalizes paths typed or pasted into text boxes.
/// </summary>
internal static class PathInput
{
    /// <summary>
    /// Removes surrounding whitespace and quotes. Explorer's "Copy as path" puts quotes around the path.
    /// </summary>
    public static string Clean(string text) => text.Trim().Trim('"').Trim();
}
