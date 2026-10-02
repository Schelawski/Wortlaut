using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>
/// All user-visible texts, in German and Russian.
/// </summary>
/// <remarks>
/// Every text is written once with both translations side by side: <c>L("German", "Russian")</c>.
/// A missing translation is therefore a compile error, and both versions can be reviewed together.
/// The language is set once before the main window is created (see <see cref="UiLanguages.Apply"/>).
/// </remarks>
internal static class UiText
{
    /// <summary>Language of all texts.</summary>
    public static UiLanguage Language { get; set; } = UiLanguage.German;

    private static string L(string german, string russian) =>
        Language == UiLanguage.Russian ? russian : german;

    public const string AppTitle = "Wortlaut";

    // ----- Faster-Whisper settings -----

    public static string SettingsGroup => L("Faster-Whisper", "Faster-Whisper");
    public static string ExePathLabel => L("Programm (faster-whisper-xxl.exe)", "Программа (faster-whisper-xxl.exe)");
    public static string Browse => L("Durchsuchen…", "Обзор…");
    public static string ExeFound => L("gefunden", "найдена");
    public static string ExeNotFound => L("nicht gefunden", "не найдена");
    public static string ModelLabel => L("Modell", "Модель");
    public static string DeviceLabel => L("Gerät", "Устройство");
    public static string LanguageLabel => L("Sprache", "Язык");
    public static string FormatLabel => L("Format", "Формат");
    public static string WholeSentences => L("Ganze Sätze", "Целые предложения");

    public static string WholeSentencesTooltip => L(
        "Jedes Segment beginnt mit einem neuen Satz, Sätze werden nicht zerschnitten (--sentence).\n" +
        "Wirkt bei Text, SRT und VTT. JSON behält die ursprünglichen Segmente von Whisper.",
        "Каждый сегмент начинается с нового предложения, предложения не разрываются (--sentence).\n" +
        "Действует для текста, SRT и VTT. В JSON остаются исходные сегменты Whisper.");

    public static string ExeDialogTitle => L("faster-whisper-xxl.exe auswählen", "Выберите faster-whisper-xxl.exe");

    public static string ExeDialogFilter => L(
        "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Programme (*.exe)|*.exe",
        "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Программы (*.exe)|*.exe");

    public static string LanguageName(string code) => code switch
    {
        "ru" => L("Russisch (ru)", "Русский (ru)"),
        "de" => L("Deutsch (de)", "Немецкий (de)"),
        "en" => L("Englisch (en)", "Английский (en)"),
        WhisperSettings.AutoLanguage => L("Automatisch erkennen", "Определить автоматически"),
        _ => code,
    };

    public static string FormatName(OutputFormatInfo info)
    {
        var name = info.Format switch
        {
            OutputFormat.Text => L("Text", "Текст"),
            OutputFormat.Json => "JSON",
            OutputFormat.Srt => L("Untertitel", "Субтитры"),
            OutputFormat.Vtt => "WebVTT",
            _ => info.CliName,
        };
        return $"{name} ({info.Extension})";
    }

    // ----- Tabs -----

    public static string SingleFileTab => L("Einzelne Datei", "Один файл");
    public static string FolderTab => L("Ganzer Ordner", "Вся папка");

    // ----- Single file -----

    public static string MediaFileLabel => L(
        "Video- oder Audiodatei (auch per Drag & Drop)",
        "Видео- или аудиофайл (можно перетащить в окно)");

    public static string ChooseFile => L("Datei wählen…", "Выбрать файл…");
    public static string ResultCaption => L("Ergebnis:", "Результат:");
    public static string OverwriteExisting => L("Vorhandenes Transkript überschreiben", "Перезаписать существующую расшифровку");
    public static string Transcribe => L("Transkribieren", "Расшифровать");
    public static string Cancel => L("Abbrechen", "Отменить");
    public static string MediaDialogTitle => L("Video- oder Audiodatei auswählen", "Выберите видео- или аудиофайл");
    public static string TargetExistsWillSkip => L("vorhanden – wird übersprungen", "уже есть – будет пропущен");
    public static string TargetExistsWillOverwrite => L("vorhanden – wird überschrieben", "уже есть – будет перезаписан");

    public static string MediaDialogFilter =>
        $"{L("Video- und Audiodateien", "Видео- и аудиофайлы")}|{string.Join(';', MediaFiles.SupportedExtensions.Select(e => "*" + e))}|" +
        $"{L("Alle Dateien", "Все файлы")} (*.*)|*.*";

    // ----- Run state (next to the progress bar) -----

    public static string StateReady => L("bereit", "ожидание");
    public static string StateStarting => L("startet…", "запуск…");
    public static string StateCancelling => L("bricht ab…", "отмена…");
    public static string StateDone => L("fertig", "готово");
    public static string StateSkipped => L("übersprungen", "пропущено");
    public static string StateFailed => L("Fehler", "ошибка");
    public static string StateCancelled => L("abgebrochen", "отменено");

    public static string StatePercent(double fraction) => $"{(int)Math.Floor(fraction * 100)} %";

    public static string StatePosition(TimeSpan position) =>
        L($"läuft… {FormatDuration(position)}", $"обработка… {FormatDuration(position)}");

    // ----- Folder -----

    public static string FolderLabel => L("Ordner", "Папка");
    public static string ChooseFolder => L("Ordner wählen…", "Выбрать папку…");
    public static string Refresh => L("Aktualisieren", "Обновить");
    public static string SkipExisting => L("Dateien mit vorhandenem Transkript überspringen", "Пропускать файлы, у которых уже есть расшифровка");
    public static string IncludeSubfolders => L("Unterordner einbeziehen", "Включая вложенные папки");
    public static string ColumnFile => L("Datei", "Файл");
    public static string ColumnDuration => L("Länge", "Длительность");
    public static string ColumnStatus => L("Status", "Статус");
    public static string TranscribeAll => L("Alle transkribieren", "Расшифровать все");
    public static string FolderDialogTitle => L("Ordner mit Video- oder Audiodateien auswählen", "Выберите папку с видео- или аудиофайлами");
    public static string ToggleAllTooltip => L("Alle markieren oder alle Markierungen entfernen", "Отметить все или снять все отметки");

    public static string CountOf(int done, int total) => L($"{done} von {total}", $"{done} из {total}");

    // Status column
    public static string RowWaiting => L("wartet", "в очереди");
    public static string RowExistsWillSkip => L("vorhanden, wird übersprungen", "уже есть, будет пропущен");
    public static string RowDone => L("fertig", "готово");
    public static string RowSkipped => L("übersprungen", "пропущен");
    public static string RowFailed => L("Fehler", "ошибка");
    public static string RowCancelled => L("abgebrochen", "отменён");

    public static string RowRunning(double? fraction) => fraction is { } value
        ? L($"läuft ({(int)Math.Floor(value * 100)} %)", $"обработка ({(int)Math.Floor(value * 100)} %)")
        : L("läuft", "обработка");

    // ----- Status bar -----

    public static string SettingsSummary(WhisperSettings settings) =>
        string.Join(" · ",
            string.IsNullOrWhiteSpace(settings.Model) ? "?" : settings.Model,
            string.IsNullOrWhiteSpace(settings.Device) ? "?" : settings.Device,
            settings.IsAutoLanguage ? WhisperSettings.AutoLanguage : settings.Language,
            settings.Format.GetExtension());

    public static string SettingsSavedIn(string location) =>
        L($"Einstellungen gespeichert in {location}", $"Настройки сохранены в {location}");

    public static string SettingsNotSaved(string reason) =>
        L($"Einstellungen konnten nicht gespeichert werden: {reason}", $"Не удалось сохранить настройки: {reason}");

    public static string UiLanguageTooltip => L("Sprache der Oberfläche", "Язык интерфейса");

    // ----- Log -----

    public static string LogReady => L("Bereit.", "Готов к работе.");
    public static string LogCancelling => L("Abbruch angefordert …", "Запрошена отмена…");

    public static string LogDurationUnknown => L(
        "Dauer unbekannt – der Fortschritt wird als Zeitmarke angezeigt.",
        "Длительность неизвестна – прогресс показывается отметкой времени.");

    public static string LogStartFile(string mediaPath) => L($"Transkription: {mediaPath}", $"Расшифровка: {mediaPath}");

    public static string LogDuration(TimeSpan duration) =>
        L($"Dauer: {FormatDuration(duration)}", $"Длительность: {FormatDuration(duration)}");

    public static string LogFolderLoaded(int count, string folder) =>
        L($"{count} {GermanFiles(count)} in {folder}", $"{count} {RussianFiles(count)} в папке {folder}");

    public static string LogOrphanedTempFolder(string path) => L(
        $"Hinweis: Verwaister Arbeitsordner gefunden (Rest eines abgebrochenen Laufs?): {path}. " +
        "Wortlaut löscht ihn nicht automatisch – bitte prüfen und bei Bedarf von Hand löschen.",
        $"Внимание: найдена оставшаяся рабочая папка (после прерванного запуска?): {path}. " +
        "Wortlaut не удаляет её автоматически – проверьте её и при необходимости удалите вручную.");

    public static string LogBulkStart(int files) => L(
        $"Starte Ordner-Transkription: {files} {GermanFiles(files)}.",
        $"Запуск расшифровки папки: {files} {RussianFiles(files)}.");

    public static string LogBulkItem(int position, int total, string fileName) => $"── [{position}/{total}] {fileName} ──";

    public static string LogBulkSummary(BulkSummary summary) => L(
        $"Zusammenfassung: {summary.Completed} fertig · {summary.Skipped} übersprungen · " +
        $"{summary.Failed} Fehler · {summary.Cancelled} abgebrochen · Gesamtdauer {FormatDuration(summary.Elapsed)}",
        $"Итог: готово: {summary.Completed} · пропущено: {summary.Skipped} · " +
        $"ошибок: {summary.Failed} · отменено: {summary.Cancelled} · общее время: {FormatDuration(summary.Elapsed)}");

    public static string LogFolderError(string folder, string reason) =>
        L($"Ordner kann nicht gelesen werden: {folder} ({reason})", $"Не удаётся прочитать папку: {folder} ({reason})");

    public static string LogMessage(JobMessageUpdate message) => message.Kind switch
    {
        JobMessageKind.CopiedInsteadOfHardLink => L(
            $"Hardlink nicht möglich ({message.Detail}). Die Datei wird in den Arbeitsordner kopiert.",
            $"Жёсткая ссылка невозможна ({message.Detail}). Файл копируется в рабочую папку."),
        JobMessageKind.CopiedReadOnlyMedia => L(
            "Die Datei ist schreibgeschützt. Sie wird in den Arbeitsordner kopiert (statt Hardlink).",
            "Файл защищён от записи. Он копируется в рабочую папку (вместо жёсткой ссылки)."),
        JobMessageKind.Starting => L($"Starte: {message.Detail}", $"Запуск: {message.Detail}"),
        JobMessageKind.CleanupFailed => L(
            $"Arbeitsordner konnte nicht gelöscht werden: {message.Detail}",
            $"Не удалось удалить рабочую папку: {message.Detail}"),
        _ => message.Kind.ToString(),
    };

    public static string LogResult(TranscriptionResult result) => result.Outcome switch
    {
        JobOutcome.Completed when result.CrashedAfterCompletion =>
            $"{CompletedText(result)}. {CrashedAfterCompletionNote(result)}",
        JobOutcome.Completed => CompletedText(result),
        JobOutcome.Skipped when result.SkipReason == SkipReason.DuplicateTarget => L(
            $"Übersprungen: {result.TargetPath} wurde in diesem Lauf schon aus einer anderen Datei erzeugt.",
            $"Пропущено: {result.TargetPath} уже создан в этом запуске из другого файла."),
        JobOutcome.Skipped => L(
            $"Übersprungen, Transkript vorhanden: {result.TargetPath}",
            $"Пропущено, расшифровка уже есть: {result.TargetPath}"),
        JobOutcome.Cancelled => L(
            $"Abgebrochen: {Path.GetFileName(result.MediaPath)}",
            $"Отменено: {Path.GetFileName(result.MediaPath)}"),
        _ => ErrorText(result),
    };

    private static string CompletedText(TranscriptionResult result) => L(
        $"Fertig nach {FormatDuration(result.Elapsed)}: {result.TargetPath}",
        $"Готово за {FormatDuration(result.Elapsed)}: {result.TargetPath}");

    /// <summary>Error description for the log and the status column tooltip.</summary>
    public static string ErrorText(TranscriptionResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.Detail)
            ? string.Empty
            : L($" Letzte Meldung: {result.Detail}", $" Последнее сообщение: {result.Detail}");
        var exitCode = FormatExitCode(result.ExitCode);

        return result.Error switch
        {
            JobError.MediaNotFound => L(
                $"Fehler: Datei nicht gefunden: {result.Detail}",
                $"Ошибка: файл не найден: {result.Detail}"),
            JobError.StartFailed => L(
                $"Fehler: faster-whisper konnte nicht gestartet werden: {result.Detail}",
                $"Ошибка: не удалось запустить faster-whisper: {result.Detail}"),
            JobError.ProcessFailed => L(
                $"Fehler: faster-whisper wurde mit Code {exitCode} beendet.{detail}",
                $"Ошибка: faster-whisper завершился с кодом {exitCode}.{detail}"),
            JobError.ResultMissing => L(
                $"Fehler: faster-whisper hat keine Ergebnisdatei geschrieben.{detail}",
                $"Ошибка: faster-whisper не создал файл с результатом.{detail}"),
            _ => L($"Fehler: {result.Detail}", $"Ошибка: {result.Detail}"),
        };
    }

    /// <summary>Note for a transcript that is complete although faster-whisper crashed while shutting down.</summary>
    public static string CrashedAfterCompletionNote(TranscriptionResult result)
    {
        var exitCode = FormatExitCode(result.ExitCode);
        return L(
            $"Hinweis: faster-whisper ist erst nach dem Schreiben des Ergebnisses abgestürzt (Code {exitCode}, " +
            "bekanntes Problem beim Beenden). Das Transkript ist vollständig und wurde übernommen.",
            $"Примечание: faster-whisper аварийно завершился уже после записи результата (код {exitCode}, " +
            "известная проблема при выходе). Расшифровка полная и сохранена.");
    }

    // ----- Messages -----

    public static string ExeMissing => L(
        "faster-whisper-xxl.exe wurde nicht gefunden. Bitte den Pfad unter „Programm“ angeben oder über „Durchsuchen…“ auswählen.",
        "faster-whisper-xxl.exe не найден. Укажите путь в поле «Программа» или выберите файл через «Обзор…».");

    public static string ModelMissing => L("Bitte ein Modell angeben.", "Укажите модель.");
    public static string DeviceMissing => L("Bitte ein Gerät auswählen.", "Выберите устройство.");
    public static string MediaMissing => L("Bitte eine vorhandene Video- oder Audiodatei wählen.", "Выберите существующий видео- или аудиофайл.");
    public static string FolderMissing => L("Bitte einen vorhandenen Ordner wählen.", "Выберите существующую папку.");
    public static string NothingSelected => L("Es ist keine Datei markiert.", "Не отмечено ни одного файла.");

    public static string NothingToDo => L(
        "Alle markierten Dateien haben bereits ein Transkript und werden übersprungen.",
        "У всех отмеченных файлов уже есть расшифровка – они будут пропущены.");

    public static string ConfirmClose => L(
        "Eine Transkription läuft noch. Abbrechen und Wortlaut beenden?",
        "Расшифровка ещё идёт. Отменить её и закрыть Wortlaut?");

    public static string MediaNotSupported(string extension)
    {
        var supported = string.Join(' ', MediaFiles.SupportedExtensions);
        return L(
            $"Dateien vom Typ „{extension}“ werden nicht unterstützt. Unterstützt: {supported}",
            $"Файлы типа «{extension}» не поддерживаются. Поддерживаются: {supported}");
    }

    public static string UnexpectedError(string message) =>
        L($"Unerwarteter Fehler: {message}", $"Непредвиденная ошибка: {message}");

    // ----- Formatting (language-neutral) -----

    /// <summary>Windows status codes are negative; the hex form (e.g. 0xC0000409) is the one to search for.</summary>
    public static string FormatExitCode(int? exitCode) => exitCode switch
    {
        null => "?",
        < 0 or > 255 => $"{exitCode} (0x{exitCode:X8})",
        _ => $"{exitCode}",
    };

    /// <summary>"48:12" below one hour, "1:02:40" above.</summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
    }

    // ----- Plurals -----

    private static string GermanFiles(int count) => count == 1 ? "Datei" : "Dateien";

    private static string RussianFiles(int count) => RussianPlural(count, "файл", "файла", "файлов");

    /// <summary>Russian plural: 1 файл, 2–4 файла, 5–20 файлов, 21 файл, 22 файла, …</summary>
    internal static string RussianPlural(int count, string one, string few, string many)
    {
        var lastTwo = Math.Abs(count) % 100;
        if (lastTwo is >= 11 and <= 14)
            return many;

        return (lastTwo % 10) switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
