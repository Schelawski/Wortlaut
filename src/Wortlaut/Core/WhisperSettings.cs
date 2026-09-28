namespace Wortlaut.Core;

/// <summary>
/// Settings for one faster-whisper run. Shared by the single-file and the folder mode.
/// </summary>
public sealed record WhisperSettings
{
    /// <summary>Language value that lets faster-whisper detect the language (no <c>--language</c> argument).</summary>
    public const string AutoLanguage = "auto";

    public const string DefaultModel = "large-v2";
    public const string DefaultDevice = "cuda";
    public const string DefaultLanguage = "ru";

    /// <summary>Models offered in the UI. The model box is editable, so other names are allowed.</summary>
    public static IReadOnlyList<string> KnownModels { get; } =
        ["large-v2", "large-v3", "large-v3-turbo", "medium", "small"];

    public static IReadOnlyList<string> KnownDevices { get; } = ["cuda", "cpu"];

    /// <summary>Language codes offered in the UI. The language box is editable for other codes.</summary>
    public static IReadOnlyList<string> KnownLanguages { get; } = ["ru", "de", "en", AutoLanguage];

    /// <summary>Full path to faster-whisper-xxl.exe.</summary>
    public string ExePath { get; init; } = string.Empty;

    public string Model { get; init; } = DefaultModel;

    public string Device { get; init; } = DefaultDevice;

    /// <summary>Language code such as "ru", or <see cref="AutoLanguage"/>.</summary>
    public string Language { get; init; } = DefaultLanguage;

    public OutputFormat Format { get; init; } = OutputFormat.Text;

    /// <summary>
    /// Start every segment with a new sentence and keep sentences whole (<c>--sentence</c>).
    /// Faster-Whisper-XXL applies this to Text, SRT and VTT; JSON keeps the original segments.
    /// </summary>
    public bool WholeSentences { get; init; } = true;

    /// <summary>True when faster-whisper should detect the language itself.</summary>
    public bool IsAutoLanguage =>
        string.IsNullOrWhiteSpace(Language)
        || string.Equals(Language.Trim(), AutoLanguage, StringComparison.OrdinalIgnoreCase);
}
