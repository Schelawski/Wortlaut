namespace Wortlaut.Core.Setup;

/// <summary>Pages of the welcome wizard, in order.</summary>
public enum WizardStep
{
    /// <summary>Greeting, UI language, "your recordings stay on your computer".</summary>
    Welcome,

    /// <summary>What will be downloaded, then download and extract Faster-Whisper-XXL.</summary>
    Install,

    /// <summary>Check the graphics card and apply the suggested device and model.</summary>
    Graphics,

    /// <summary>Download the selected model.</summary>
    Model,

    /// <summary>Done: "transcribe the first file".</summary>
    Done,
}

/// <summary>
/// Navigation rules of the welcome wizard. Kept free of UI code so the rules can be tested.
/// </summary>
public static class SetupWizardFlow
{
    /// <summary>Number of pages shown in "Schritt n von m".</summary>
    public static int StepCount => Enum.GetValues<WizardStep>().Length;

    /// <summary>
    /// The wizard opens by itself when no faster-whisper-xxl.exe is found, and when an earlier run of the wizard
    /// was closed after the installation (it then continues with the graphics card or the model).
    /// Closed before that and the program chosen by hand in the main window: the user went their own way.
    /// </summary>
    public static bool ShouldShowAtStartup(WizardStep? resumeStep, bool exeFound) =>
        !exeFound || resumeStep > WizardStep.Install;

    /// <summary>Where the wizard starts: at the saved step, but never behind a missing installation.</summary>
    public static WizardStep StartStep(WizardStep? resumeStep, bool exeFound) => resumeStep switch
    {
        null or WizardStep.Welcome => WizardStep.Welcome,
        > WizardStep.Install when !exeFound => WizardStep.Install,
        WizardStep.Done => WizardStep.Done,
        { } step => step,
    };

    /// <summary>The page after <paramref name="current"/>; pages with nothing left to do are skipped.</summary>
    public static WizardStep Next(WizardStep current, bool exeFound, bool modelInstalled) => current switch
    {
        WizardStep.Welcome => exeFound ? WizardStep.Graphics : WizardStep.Install,
        WizardStep.Install => WizardStep.Graphics,
        WizardStep.Graphics => modelInstalled ? WizardStep.Done : WizardStep.Model,
        _ => WizardStep.Done,
    };

    /// <summary>The page before <paramref name="current"/>, skipping the same pages as <see cref="Next"/>.</summary>
    public static WizardStep Previous(WizardStep current, bool exeFound, bool modelInstalled) => current switch
    {
        WizardStep.Install => WizardStep.Welcome,
        WizardStep.Graphics => exeFound ? WizardStep.Welcome : WizardStep.Install,
        WizardStep.Model => WizardStep.Graphics,
        WizardStep.Done => modelInstalled ? WizardStep.Graphics : WizardStep.Model,
        _ => WizardStep.Welcome,
    };

    /// <summary>
    /// Rough duration of a download of <paramref name="bytes"/> plus extracting, for a fast (100 Mbit/s) and a slow
    /// (16 Mbit/s) internet connection. Shown as "about 3–14 minutes".
    /// </summary>
    public static (TimeSpan Fast, TimeSpan Slow) EstimateDuration(long bytes, bool includesExtracting)
    {
        const double fastBytesPerSecond = 100_000_000 / 8d;
        const double slowBytesPerSecond = 16_000_000 / 8d;
        var extractFast = includesExtracting ? TimeSpan.FromMinutes(1) : TimeSpan.Zero;
        var extractSlow = includesExtracting ? TimeSpan.FromMinutes(2) : TimeSpan.Zero;

        static TimeSpan RoundUp(TimeSpan time) => TimeSpan.FromMinutes(Math.Max(1, Math.Ceiling(time.TotalMinutes)));
        return (
            RoundUp(TimeSpan.FromSeconds(bytes / fastBytesPerSecond) + extractFast),
            RoundUp(TimeSpan.FromSeconds(bytes / slowBytesPerSecond) + extractSlow));
    }
}
