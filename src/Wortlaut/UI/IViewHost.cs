using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>Whether a transcription is running and where.</summary>
internal enum RunState
{
    Idle,

    /// <summary>This view started the running transcription.</summary>
    RunningHere,

    /// <summary>Another view is running a transcription (one GPU: only one run at a time).</summary>
    RunningElsewhere,
}

/// <summary>
/// A tab that can start transcriptions.
/// </summary>
internal interface IRunView
{
    /// <summary>Enables/disables the view's controls for the given state.</summary>
    void SetRunState(RunState state);

    /// <summary>The shared faster-whisper settings changed (e.g. the format, which changes the transcript names).</summary>
    void OnWhisperSettingsChanged();
}

/// <summary>
/// Services the main window offers to its tabs.
/// </summary>
internal interface IViewHost
{
    /// <summary>Shared settings. Views may change their own fields and call <see cref="SettingsChanged"/>.</summary>
    AppSettings Settings { get; }

    TranscriptionJob Job { get; }

    IMediaDurationProbe DurationProbe { get; }

    bool IsRunning { get; }

    /// <summary>Schedules saving the settings.</summary>
    void SettingsChanged();

    /// <summary>
    /// Checks the faster-whisper settings and shows a message if something is missing.
    /// </summary>
    /// <returns>The settings for a run, or <c>null</c> if they are incomplete.</returns>
    WhisperSettings? ValidateWhisperSettings();

    /// <summary>
    /// Runs <paramref name="work"/> as the only transcription: locks the settings and the start buttons of all
    /// tabs until it has finished. The token is cancelled by "Abbrechen" or when the window closes.
    /// </summary>
    Task RunExclusiveAsync(IRunView view, Func<CancellationToken, Task> work);

    /// <summary>Cancels the running transcription.</summary>
    void CancelRun();

    void ShowWarning(string message);
}
