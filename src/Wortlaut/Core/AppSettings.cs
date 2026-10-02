namespace Wortlaut.Core;

/// <summary>
/// Everything Wortlaut remembers between sessions. Stored as <c>Wortlaut.settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Full path to faster-whisper-xxl.exe.</summary>
    public string ExePath { get; set; } = string.Empty;

    public string Model { get; set; } = WhisperSettings.DefaultModel;

    public string Device { get; set; } = WhisperSettings.DefaultDevice;

    /// <summary>Language code such as "ru", or <see cref="WhisperSettings.AutoLanguage"/>.</summary>
    public string Language { get; set; } = WhisperSettings.DefaultLanguage;

    public OutputFormat Format { get; set; } = OutputFormat.Text;

    /// <summary>Keep sentences whole (<c>--sentence</c>). On by default, also for older settings files.</summary>
    public bool WholeSentences { get; set; } = true;

    /// <summary>Last file chosen in the single-file mode.</summary>
    public string LastFile { get; set; } = string.Empty;

    /// <summary>Last folder chosen in the folder mode.</summary>
    public string LastFolder { get; set; } = string.Empty;

    /// <summary>Folder mode: include subfolders.</summary>
    public bool IncludeSubfolders { get; set; }

    /// <summary>Folder mode: skip files whose transcript already exists.</summary>
    public bool SkipExisting { get; set; } = true;

    /// <summary>
    /// Language of the user interface as a code ("de", "ru"). Empty until the user picks one;
    /// then the Windows display language decides.
    /// </summary>
    public string UiLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Page of the welcome wizard that was open when it was closed before the end; it continues there on the
    /// next start. <c>null</c> once the wizard was finished (or never started).
    /// </summary>
    public Setup.WizardStep? WizardResumeStep { get; set; }

    public WhisperSettings ToWhisperSettings() => new()
    {
        ExePath = ExePath.Trim(),
        Model = Model.Trim(),
        Device = Device.Trim(),
        Language = Language.Trim(),
        Format = Format,
        WholeSentences = WholeSentences,
    };

    /// <summary>Replaces missing values (e.g. from a hand-edited file) with defaults.</summary>
    internal void Normalize()
    {
        ExePath ??= string.Empty;
        Model = string.IsNullOrWhiteSpace(Model) ? WhisperSettings.DefaultModel : Model;
        Device = string.IsNullOrWhiteSpace(Device) ? WhisperSettings.DefaultDevice : Device;
        Language = string.IsNullOrWhiteSpace(Language) ? WhisperSettings.DefaultLanguage : Language;
        if (!Enum.IsDefined(Format))
            Format = OutputFormat.Text;
        LastFile ??= string.Empty;
        LastFolder ??= string.Empty;
        UiLanguage ??= string.Empty;
        if (WizardResumeStep is { } step && !Enum.IsDefined(step))
            WizardResumeStep = null;
    }
}
