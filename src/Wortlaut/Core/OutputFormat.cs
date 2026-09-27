namespace Wortlaut.Core;

/// <summary>
/// Transcript formats offered by Wortlaut.
/// </summary>
public enum OutputFormat
{
    Text,
    Json,
    Srt,
    Vtt,
}

/// <summary>
/// Describes how an <see cref="OutputFormat"/> is passed to faster-whisper and how its transcript is named.
/// </summary>
/// <param name="Format">The format.</param>
/// <param name="CliName">Value for <c>--output_format</c>.</param>
/// <param name="Extension">Extension of the final transcript next to the media file.</param>
/// <param name="ProducedExtensions">
/// Extensions faster-whisper may use for the file it writes, in the order they are looked up.
/// </param>
public sealed record OutputFormatInfo(
    OutputFormat Format,
    string CliName,
    string Extension,
    IReadOnlyList<string> ProducedExtensions);

/// <summary>
/// The single place that maps an <see cref="OutputFormat"/> to its command-line value and file extension.
/// </summary>
public static class OutputFormats
{
    private static readonly OutputFormatInfo[] Infos =
    [
        // "text" is plain text without timestamps ("txt" would add timestamps to every line).
        // Faster-Whisper-XXL r239 writes it as ".text", older builds used ".txt".
        // Wortlaut always saves it as ".txt".
        new(OutputFormat.Text, "text", ".txt", [".text", ".txt"]),
        new(OutputFormat.Json, "json", ".json", [".json"]),
        new(OutputFormat.Srt, "srt", ".srt", [".srt"]),
        new(OutputFormat.Vtt, "vtt", ".vtt", [".vtt"]),
    ];

    /// <summary>All formats in display order.</summary>
    public static IReadOnlyList<OutputFormatInfo> All => Infos;

    public static OutputFormatInfo Get(OutputFormat format) =>
        Array.Find(Infos, info => info.Format == format)
        ?? throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown output format.");

    /// <summary>Extension of the final transcript, including the leading dot (e.g. ".txt").</summary>
    public static string GetExtension(this OutputFormat format) => Get(format).Extension;

    /// <summary>Value passed to faster-whisper's <c>--output_format</c>.</summary>
    public static string GetCliName(this OutputFormat format) => Get(format).CliName;
}
