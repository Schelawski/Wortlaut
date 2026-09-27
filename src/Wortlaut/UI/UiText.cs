using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>
/// All user-visible texts. The UI is German; keep every string here so it can be reviewed and changed in one place.
/// </summary>
internal static class UiText
{
    public const string AppTitle = "Wortlaut";

    // ----- Faster-Whisper settings -----

    public const string SettingsGroup = "Faster-Whisper";
    public const string ExePathLabel = "Programm (faster-whisper-xxl.exe)";
    public const string Browse = "Durchsuchen…";
    public const string ExeFound = "gefunden";
    public const string ExeNotFound = "nicht gefunden";
    public const string ModelLabel = "Modell";
    public const string DeviceLabel = "Gerät";
    public const string LanguageLabel = "Sprache";
    public const string FormatLabel = "Format";
    public const string ExeDialogTitle = "faster-whisper-xxl.exe auswählen";
    public const string ExeDialogFilter = "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Programme (*.exe)|*.exe";

    public static string LanguageName(string code) => code switch
    {
        "ru" => "Russisch (ru)",
        "de" => "Deutsch (de)",
        "en" => "Englisch (en)",
        WhisperSettings.AutoLanguage => "Automatisch erkennen",
        _ => code,
    };

    public static string FormatName(OutputFormatInfo info)
    {
        var name = info.Format switch
        {
            OutputFormat.Text => "Text",
            OutputFormat.Json => "JSON",
            OutputFormat.Srt => "Untertitel",
            OutputFormat.Vtt => "WebVTT",
            _ => info.CliName,
        };
        return $"{name} ({info.Extension})";
    }

    // ----- Tabs -----

    public const string SingleFileTab = "Einzelne Datei";
    public const string FolderTab = "Ganzer Ordner";

    // ----- Single file -----

    public const string MediaFileLabel = "Video- oder Audiodatei (auch per Drag & Drop)";
    public const string ChooseFile = "Datei wählen…";
    public const string ResultCaption = "Ergebnis:";
    public const string OverwriteExisting = "Vorhandenes Transkript überschreiben";
    public const string Transcribe = "Transkribieren";
    public const string Cancel = "Abbrechen";
    public const string MediaDialogTitle = "Video- oder Audiodatei auswählen";
    public const string TargetExistsWillSkip = "vorhanden – wird übersprungen";
    public const string TargetExistsWillOverwrite = "vorhanden – wird überschrieben";

    public static string MediaDialogFilter =>
        $"Video- und Audiodateien|{string.Join(';', MediaFiles.SupportedExtensions.Select(e => "*" + e))}|Alle Dateien (*.*)|*.*";

    // ----- Run state (next to the progress bar) -----

    public const string StateReady = "bereit";
    public const string StateStarting = "startet…";
    public const string StateCancelling = "bricht ab…";
    public const string StateDone = "fertig";
    public const string StateSkipped = "übersprungen";
    public const string StateFailed = "Fehler";
    public const string StateCancelled = "abgebrochen";

    public static string StatePercent(double fraction) => $"{(int)Math.Floor(fraction * 100)} %";

    public static string StatePosition(TimeSpan position) => $"läuft… {FormatDuration(position)}";

    // ----- Folder -----

    public const string FolderLabel = "Ordner";
    public const string ChooseFolder = "Ordner wählen…";
    public const string Refresh = "Aktualisieren";
    public const string SkipExisting = "Dateien mit vorhandenem Transkript überspringen";
    public const string IncludeSubfolders = "Unterordner einbeziehen";
    public const string ColumnFile = "Datei";
    public const string ColumnDuration = "Länge";
    public const string ColumnStatus = "Status";
    public const string TranscribeAll = "Alle transkribieren";
    public const string FolderDialogTitle = "Ordner mit Video- oder Audiodateien auswählen";
    public const string ToggleAllTooltip = "Alle markieren oder alle Markierungen entfernen";

    public static string CountOf(int done, int total) => $"{done} von {total}";

    // Status column
    public const string RowWaiting = "wartet";
    public const string RowExistsWillSkip = "vorhanden, wird übersprungen";
    public const string RowDone = "fertig";
    public const string RowSkipped = "übersprungen";
    public const string RowFailed = "Fehler";
    public const string RowCancelled = "abgebrochen";

    public static string RowRunning(double? fraction) =>
        fraction is { } value ? $"läuft ({(int)Math.Floor(value * 100)} %)" : "läuft";

    // ----- Status bar -----

    public static string SettingsSummary(WhisperSettings settings) =>
        string.Join(" · ",
            string.IsNullOrWhiteSpace(settings.Model) ? "?" : settings.Model,
            string.IsNullOrWhiteSpace(settings.Device) ? "?" : settings.Device,
            settings.IsAutoLanguage ? WhisperSettings.AutoLanguage : settings.Language,
            settings.Format.GetExtension());

    public static string SettingsSavedIn(string location) => $"Einstellungen gespeichert in {location}";

    public static string SettingsNotSaved(string reason) => $"Einstellungen konnten nicht gespeichert werden: {reason}";

    // ----- Log -----

    public const string LogReady = "Bereit.";
    public const string LogCancelling = "Abbruch angefordert …";
    public const string LogDurationUnknown = "Dauer unbekannt – der Fortschritt wird als Zeitmarke angezeigt.";

    public static string LogStartFile(string mediaPath) => $"Transkription: {mediaPath}";

    public static string LogDuration(TimeSpan duration) => $"Dauer: {FormatDuration(duration)}";

    public static string LogFolderLoaded(int count, string folder) => $"{count} Datei(en) in {folder}";

    public static string LogOrphanedTempFolder(string path) =>
        $"Hinweis: Verwaister Arbeitsordner gefunden (Rest eines abgebrochenen Laufs?): {path}. " +
        "Wortlaut löscht ihn nicht automatisch – bitte prüfen und bei Bedarf von Hand löschen.";

    public static string LogBulkStart(int files) => $"Starte Ordner-Transkription: {files} Datei(en).";

    public static string LogBulkItem(int position, int total, string fileName) => $"── [{position}/{total}] {fileName} ──";

    public static string LogBulkSummary(BulkSummary summary) =>
        $"Zusammenfassung: {summary.Completed} fertig · {summary.Skipped} übersprungen · " +
        $"{summary.Failed} Fehler · {summary.Cancelled} abgebrochen · Gesamtdauer {FormatDuration(summary.Elapsed)}";

    public static string LogFolderError(string folder, string reason) => $"Ordner kann nicht gelesen werden: {folder} ({reason})";

    public static string LogMessage(JobMessageUpdate message) => message.Kind switch
    {
        JobMessageKind.SkippedExisting => $"Transkript ist bereits vorhanden, übersprungen: {message.Detail}",
        JobMessageKind.CopiedInsteadOfHardLink =>
            $"Hardlink nicht möglich ({message.Detail}). Die Datei wird in den Arbeitsordner kopiert.",
        JobMessageKind.CopiedReadOnlyMedia =>
            "Die Datei ist schreibgeschützt. Sie wird in den Arbeitsordner kopiert (statt Hardlink).",
        JobMessageKind.Starting => $"Starte: {message.Detail}",
        JobMessageKind.CleanupFailed => $"Arbeitsordner konnte nicht gelöscht werden: {message.Detail}",
        _ => message.Kind.ToString(),
    };

    public static string LogResult(TranscriptionResult result) => result.Outcome switch
    {
        JobOutcome.Completed => $"Fertig nach {FormatDuration(result.Elapsed)}: {result.TargetPath}",
        JobOutcome.Skipped when result.SkipReason == SkipReason.DuplicateTarget =>
            $"Übersprungen: {result.TargetPath} wurde in diesem Lauf schon aus einer anderen Datei erzeugt.",
        JobOutcome.Skipped => $"Übersprungen, Transkript vorhanden: {result.TargetPath}",
        JobOutcome.Cancelled => $"Abgebrochen: {Path.GetFileName(result.MediaPath)}",
        _ => ErrorText(result),
    };

    /// <summary>Error description for the log and the status column tooltip.</summary>
    public static string ErrorText(TranscriptionResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.Detail) ? string.Empty : $" Letzte Meldung: {result.Detail}";
        return result.Error switch
        {
            JobError.MediaNotFound => $"Fehler: Datei nicht gefunden: {result.Detail}",
            JobError.StartFailed => $"Fehler: faster-whisper konnte nicht gestartet werden: {result.Detail}",
            JobError.ProcessFailed => $"Fehler: faster-whisper wurde mit Code {result.ExitCode} beendet.{detail}",
            JobError.ResultMissing => $"Fehler: faster-whisper hat keine Ergebnisdatei geschrieben.{detail}",
            _ => $"Fehler: {result.Detail}",
        };
    }

    // ----- Messages -----

    public const string ExeMissing =
        "faster-whisper-xxl.exe wurde nicht gefunden. Bitte den Pfad unter „Programm“ angeben oder über „Durchsuchen…“ auswählen.";
    public const string ModelMissing = "Bitte ein Modell angeben.";
    public const string DeviceMissing = "Bitte ein Gerät auswählen.";
    public const string MediaMissing = "Bitte eine vorhandene Video- oder Audiodatei wählen.";
    public const string FolderMissing = "Bitte einen vorhandenen Ordner wählen.";
    public const string NothingSelected = "Es ist keine Datei markiert.";
    public const string NothingToDo = "Alle markierten Dateien haben bereits ein Transkript und werden übersprungen.";
    public const string ConfirmClose = "Eine Transkription läuft noch. Abbrechen und Wortlaut beenden?";

    public static string MediaNotSupported(string extension) =>
        $"Dateien vom Typ „{extension}“ werden nicht unterstützt. Unterstützt: {string.Join(' ', MediaFiles.SupportedExtensions)}";

    public static string UnexpectedError(string message) => $"Unerwarteter Fehler: {message}";

    // ----- Formatting -----

    /// <summary>"48:12" below one hour, "1:02:40" above.</summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
    }
}
