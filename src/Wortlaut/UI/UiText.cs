using System.Globalization;
using Wortlaut.Core;
using Wortlaut.Core.Gpu;

namespace Wortlaut.UI;

/// <summary>
/// All user-visible texts, in German, Russian and English.
/// </summary>
/// <remarks>
/// Every text is written once with all translations side by side: <c>L("German", "Russian", "English")</c>.
/// A missing translation is therefore a compile error, and the versions can be reviewed together.
/// The language is set once before the main window is created (see <see cref="UiLanguages.Apply"/>).
/// </remarks>
internal static class UiText
{
    /// <summary>Language of all texts.</summary>
    public static UiLanguage Language { get; set; } = UiLanguage.German;

    private static string L(string german, string russian, string english) => Language switch
    {
        UiLanguage.Russian => russian,
        UiLanguage.English => english,
        _ => german,
    };

    /// <summary>Culture for numbers in texts, e.g. "1,36 GB" in German and "1.36 GB" in English.</summary>
    private static CultureInfo NumberCulture => CultureInfo.GetCultureInfo(Language switch
    {
        UiLanguage.Russian => "ru-RU",
        UiLanguage.English => "en-US",
        _ => "de-DE",
    });

    public const string AppTitle = "Wortlaut";

    // ----- Faster-Whisper settings -----

    public static string SettingsGroup => L("Faster-Whisper", "Faster-Whisper", "Faster-Whisper");
    public static string ExePathLabel => L("Programm (faster-whisper-xxl.exe)", "Программа (faster-whisper-xxl.exe)", "Program (faster-whisper-xxl.exe)");
    public static string Browse => L("Durchsuchen…", "Обзор…", "Browse…");
    public static string ExeFound => L("gefunden", "найдена", "found");
    public static string ExeNotFound => L("nicht gefunden", "не найдена", "not found");
    public static string ModelLabel => L("Modell", "Модель", "Model");
    public static string DeviceLabel => L("Gerät", "Устройство", "Device");
    public static string LanguageLabel => L("Sprache", "Язык", "Language");
    public static string FormatLabel => L("Format", "Формат", "Format");
    public static string WholeSentences => L("Ganze Sätze", "Целые предложения", "Whole sentences");

    // ----- Help -----

    public static string HelpButton => L("Hilfe", "Справка", "Help");
    public static string HelpTitle => L("Wortlaut – Hilfe", "Wortlaut – справка", "Wortlaut – Help");
    public static string HelpButtonTooltip => L("Hilfe öffnen (F1)", "Открыть справку (F1)", "Open the help (F1)");
    public static string HelpClickForMore => L("Klicken Sie auf das „?“ für mehr.", "Нажмите «?», чтобы узнать больше.", "Click the “?” to learn more.");

    public static string HelpTipModel => L(
        "Das „Gehirn“ der Spracherkennung. Größere Modelle sind genauer, aber langsamer. Für russische Vorträge empfehlen wir large-v2.",
        "«Мозг» распознавания речи. Большие модели точнее, но медленнее. Для лекций на русском рекомендуем large-v2.",
        "The “brain” of the speech recognition. Larger models are more accurate but slower. For Russian talks we recommend large-v2.");

    public static string HelpTipDevice => L(
        "cuda nutzt die NVIDIA-Grafikkarte und ist viel schneller. cpu nutzt den Prozessor: Das geht auf jedem Computer, dauert aber länger.",
        "cuda использует видеокарту NVIDIA и работает намного быстрее. cpu использует процессор: работает на любом компьютере, но дольше.",
        "cuda uses the NVIDIA graphics card and is much faster. cpu uses the processor: it works on every computer but takes longer.");

    public static string HelpTipLanguage => L(
        "Die Sprache, in der in der Aufnahme gesprochen wird. Eine feste Sprache ist zuverlässiger als „Automatisch erkennen“.",
        "Язык, на котором говорят в записи. Конкретный язык надёжнее, чем «Определить автоматически».",
        "The language spoken in the recording. A fixed language is more reliable than “Detect automatically”.");

    public static string HelpTipFormat => L(
        "Text zum Lesen und Bearbeiten, Untertitel (.srt, .vtt) für Videos, JSON für Programme.",
        "Текст – для чтения и редактирования, субтитры (.srt, .vtt) – для видео, JSON – для программ.",
        "Text for reading and editing, subtitles (.srt, .vtt) for videos, JSON for programs.");

    public static string WholeSentencesTooltip => L(
        "Jeder Abschnitt beginnt mit einem neuen Satz, Sätze werden nicht geteilt. Beim Format Text steht ein Satz pro Zeile. Wirkt nicht bei JSON.",
        "Каждый фрагмент начинается с нового предложения, предложения не разрываются. В формате «Текст» – одно предложение на строку. Не действует для JSON.",
        "Every segment starts with a new sentence, and sentences are not split. With the Text format there is one sentence per line. Does not apply to JSON.");

    public static string ExeDialogTitle => L("faster-whisper-xxl.exe auswählen", "Выберите faster-whisper-xxl.exe", "Select faster-whisper-xxl.exe");

    public static string ExeDialogFilter => L(
        "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Programme (*.exe)|*.exe",
        "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Программы (*.exe)|*.exe",
        "faster-whisper-xxl.exe|faster-whisper-xxl.exe|Programs (*.exe)|*.exe");

    public static string LanguageName(string code) => code switch
    {
        "ru" => L("Russisch (ru)", "Русский (ru)", "Russian (ru)"),
        "de" => L("Deutsch (de)", "Немецкий (de)", "German (de)"),
        "en" => L("Englisch (en)", "Английский (en)", "English (en)"),
        WhisperSettings.AutoLanguage => L("Automatisch erkennen", "Определить автоматически", "Detect automatically"),
        _ => code,
    };

    public static string FormatName(OutputFormatInfo info)
    {
        var name = info.Format switch
        {
            OutputFormat.Text => L("Text", "Текст", "Text"),
            OutputFormat.Json => "JSON",
            OutputFormat.Srt => L("Untertitel", "Субтитры", "Subtitles"),
            OutputFormat.Vtt => "WebVTT",
            _ => info.CliName,
        };
        return $"{name} ({info.Extension})";
    }

    // ----- Tabs -----

    public static string SingleFileTab => L("Einzelne Datei", "Один файл", "Single file");
    public static string FolderTab => L("Ganzer Ordner", "Вся папка", "Whole folder");

    // ----- Single file -----

    public static string MediaFileLabel => L(
        "Video- oder Audiodatei (auch per Drag & Drop)",
        "Видео- или аудиофайл (можно перетащить в окно)",
        "Video or audio file (drag & drop works too)");

    public static string ChooseFile => L("Datei wählen…", "Выбрать файл…", "Choose file…");
    public static string ResultCaption => L("Ergebnis:", "Результат:", "Result:");
    public static string OverwriteExisting => L("Vorhandenes Transkript überschreiben", "Перезаписать существующую расшифровку", "Overwrite existing transcript");
    public static string Transcribe => L("Transkribieren", "Расшифровать", "Transcribe");
    public static string Cancel => L("Abbrechen", "Отменить", "Cancel");
    public static string MediaDialogTitle => L("Video- oder Audiodatei auswählen", "Выберите видео- или аудиофайл", "Select a video or audio file");
    public static string TargetExistsWillSkip => L("vorhanden – wird übersprungen", "уже есть – будет пропущен", "exists – will be skipped");
    public static string TargetExistsWillOverwrite => L("vorhanden – wird überschrieben", "уже есть – будет перезаписан", "exists – will be overwritten");

    public static string MediaDialogFilter =>
        $"{L("Video- und Audiodateien", "Видео- и аудиофайлы", "Video and audio files")}|{string.Join(';', MediaFiles.SupportedExtensions.Select(e => "*" + e))}|" +
        $"{L("Alle Dateien", "Все файлы", "All files")} (*.*)|*.*";

    // ----- Run state (next to the progress bar) -----

    public static string StateReady => L("bereit", "ожидание", "ready");
    public static string StateStarting => L("startet…", "запуск…", "starting…");
    public static string StateCancelling => L("bricht ab…", "отмена…", "cancelling…");
    public static string StateDone => L("fertig", "готово", "done");
    public static string StateSkipped => L("übersprungen", "пропущено", "skipped");
    public static string StateFailed => L("Fehler", "ошибка", "error");
    public static string StateCancelled => L("abgebrochen", "отменено", "cancelled");

    public static string StatePercent(double fraction) => $"{(int)Math.Floor(fraction * 100)} %";

    public static string StatePosition(TimeSpan position) =>
        L($"läuft… {FormatDuration(position)}", $"обработка… {FormatDuration(position)}", $"running… {FormatDuration(position)}");

    // ----- Folder -----

    public static string FolderLabel => L("Ordner", "Папка", "Folder");
    public static string ChooseFolder => L("Ordner wählen…", "Выбрать папку…", "Choose folder…");
    public static string Refresh => L("Aktualisieren", "Обновить", "Refresh");
    public static string SkipExisting => L("Dateien mit vorhandenem Transkript überspringen", "Пропускать файлы, у которых уже есть расшифровка", "Skip files that already have a transcript");
    public static string IncludeSubfolders => L("Unterordner einbeziehen", "Включая вложенные папки", "Include subfolders");
    public static string ColumnFile => L("Datei", "Файл", "File");
    public static string ColumnDuration => L("Länge", "Длительность", "Length");
    public static string ColumnStatus => L("Status", "Статус", "Status");
    public static string TranscribeAll => L("Alle transkribieren", "Расшифровать все", "Transcribe all");
    public static string FolderDialogTitle => L("Ordner mit Video- oder Audiodateien auswählen", "Выберите папку с видео- или аудиофайлами", "Select a folder with video or audio files");
    public static string ToggleAllTooltip => L("Alle markieren oder alle Markierungen entfernen", "Отметить все или снять все отметки", "Tick all or untick all");

    public static string CountOf(int done, int total) => L($"{done} von {total}", $"{done} из {total}", $"{done} of {total}");

    // Status column
    public static string RowWaiting => L("wartet", "в очереди", "waiting");
    public static string RowExistsWillSkip => L("vorhanden, wird übersprungen", "уже есть, будет пропущен", "exists, will be skipped");
    public static string RowDone => L("fertig", "готово", "done");
    public static string RowSkipped => L("übersprungen", "пропущен", "skipped");
    public static string RowFailed => L("Fehler", "ошибка", "error");
    public static string RowCancelled => L("abgebrochen", "отменён", "cancelled");

    public static string RowRunning(double? fraction) => fraction is { } value
        ? L($"läuft ({(int)Math.Floor(value * 100)} %)", $"обработка ({(int)Math.Floor(value * 100)} %)", $"running ({(int)Math.Floor(value * 100)} %)")
        : L("läuft", "обработка", "running");

    // ----- Status bar -----

    public static string SettingsSummary(WhisperSettings settings) =>
        string.Join(" · ",
            string.IsNullOrWhiteSpace(settings.Model) ? "?" : settings.Model,
            string.IsNullOrWhiteSpace(settings.Device) ? "?" : settings.Device,
            settings.IsAutoLanguage ? WhisperSettings.AutoLanguage : settings.Language,
            settings.Format.GetExtension());

    public static string SettingsSavedIn(string location) =>
        L($"Einstellungen gespeichert in {location}", $"Настройки сохранены в {location}", $"Settings saved in {location}");

    public static string SettingsNotSaved(string reason) =>
        L($"Einstellungen konnten nicht gespeichert werden: {reason}", $"Не удалось сохранить настройки: {reason}", $"Settings could not be saved: {reason}");

    public static string UiLanguageTooltip => L("Sprache der Oberfläche", "Язык интерфейса", "Language of the user interface");

    // ----- Log -----

    public static string LogReady => L("Bereit.", "Готов к работе.", "Ready.");
    public static string LogCancelling => L("Abbruch angefordert …", "Запрошена отмена…", "Cancel requested …");

    public static string LogDurationUnknown => L(
        "Dauer unbekannt – der Fortschritt wird als Zeitmarke angezeigt.",
        "Длительность неизвестна – прогресс показывается отметкой времени.",
        "Length unknown – the progress is shown as a time mark.");

    public static string LogStartFile(string mediaPath) => L($"Transkription: {mediaPath}", $"Расшифровка: {mediaPath}", $"Transcription: {mediaPath}");

    public static string LogDuration(TimeSpan duration) =>
        L($"Dauer: {FormatDuration(duration)}", $"Длительность: {FormatDuration(duration)}", $"Length: {FormatDuration(duration)}");

    public static string LogFolderLoaded(int count, string folder) =>
        L($"{count} {GermanFiles(count)} in {folder}", $"{count} {RussianFiles(count)} в папке {folder}", $"{count} {EnglishFiles(count)} in {folder}");

    public static string LogOrphanedTempFolder(string path) => L(
        $"Hinweis: Verwaister Arbeitsordner gefunden (Rest eines abgebrochenen Laufs?): {path}. " +
        "Wortlaut löscht ihn nicht automatisch – bitte prüfen und bei Bedarf von Hand löschen.",
        $"Внимание: найдена оставшаяся рабочая папка (после прерванного запуска?): {path}. " +
        "Wortlaut не удаляет её автоматически – проверьте её и при необходимости удалите вручную.",
        $"Note: leftover work folder found (from an interrupted run?): {path}. " +
        "Wortlaut does not delete it automatically – please check it and delete it by hand if needed.");

    public static string LogBulkStart(int files) => L(
        $"Starte Ordner-Transkription: {files} {GermanFiles(files)}.",
        $"Запуск расшифровки папки: {files} {RussianFiles(files)}.",
        $"Starting folder transcription: {files} {EnglishFiles(files)}.");

    public static string LogBulkItem(int position, int total, string fileName) => $"── [{position}/{total}] {fileName} ──";

    public static string LogBulkSummary(BulkSummary summary) => L(
        $"Zusammenfassung: {summary.Completed} fertig · {summary.Skipped} übersprungen · " +
        $"{summary.Failed} Fehler · {summary.Cancelled} abgebrochen · Gesamtdauer {FormatDuration(summary.Elapsed)}",
        $"Итог: готово: {summary.Completed} · пропущено: {summary.Skipped} · " +
        $"ошибок: {summary.Failed} · отменено: {summary.Cancelled} · общее время: {FormatDuration(summary.Elapsed)}",
        $"Summary: {summary.Completed} done · {summary.Skipped} skipped · " +
        $"{summary.Failed} errors · {summary.Cancelled} cancelled · total time {FormatDuration(summary.Elapsed)}");

    public static string LogBulkStoppedByCuda => L(
        "Ordner-Transkription angehalten: Die Grafikkarte kann nicht verwendet werden, jede weitere Datei würde ebenso " +
        "scheitern. Die übrigen Dateien wurden nicht bearbeitet.",
        "Расшифровка папки остановлена: видеокарту нельзя использовать, и каждый следующий файл завершился бы " +
        "той же ошибкой. Остальные файлы не обработаны.",
        "Folder transcription stopped: the graphics card cannot be used, and every further file would fail the same way. " +
        "The remaining files were not processed.");

    public static string LogFolderError(string folder, string reason) =>
        L($"Ordner kann nicht gelesen werden: {folder} ({reason})", $"Не удаётся прочитать папку: {folder} ({reason})", $"Folder cannot be read: {folder} ({reason})");

    public static string LogMessage(JobMessageUpdate message) => message.Kind switch
    {
        JobMessageKind.CopiedInsteadOfHardLink => L(
            $"Hardlink nicht möglich ({message.Detail}). Die Datei wird in den Arbeitsordner kopiert.",
            $"Жёсткая ссылка невозможна ({message.Detail}). Файл копируется в рабочую папку.",
            $"Hard link not possible ({message.Detail}). The file is copied into the work folder."),
        JobMessageKind.CopiedReadOnlyMedia => L(
            "Die Datei ist schreibgeschützt. Sie wird in den Arbeitsordner kopiert (statt Hardlink).",
            "Файл защищён от записи. Он копируется в рабочую папку (вместо жёсткой ссылки).",
            "The file is read-only. It is copied into the work folder (instead of a hard link)."),
        JobMessageKind.Starting => L($"Starte: {message.Detail}", $"Запуск: {message.Detail}", $"Starting: {message.Detail}"),
        JobMessageKind.CleanupFailed => L(
            $"Arbeitsordner konnte nicht gelöscht werden: {message.Detail}",
            $"Не удалось удалить рабочую папку: {message.Detail}",
            $"Work folder could not be deleted: {message.Detail}"),
        _ => message.Kind.ToString(),
    };

    public static string LogResult(TranscriptionResult result) => result.Outcome switch
    {
        JobOutcome.Completed when result.CrashedAfterCompletion =>
            $"{CompletedText(result)}. {CrashedAfterCompletionNote(result)}",
        JobOutcome.Completed => CompletedText(result),
        JobOutcome.Skipped when result.SkipReason == SkipReason.DuplicateTarget => L(
            $"Übersprungen: {result.TargetPath} wurde in diesem Lauf schon aus einer anderen Datei erzeugt.",
            $"Пропущено: {result.TargetPath} уже создан в этом запуске из другого файла.",
            $"Skipped: {result.TargetPath} was already created from another file in this run."),
        JobOutcome.Skipped => L(
            $"Übersprungen, Transkript vorhanden: {result.TargetPath}",
            $"Пропущено, расшифровка уже есть: {result.TargetPath}",
            $"Skipped, transcript exists: {result.TargetPath}"),
        JobOutcome.Cancelled => L(
            $"Abgebrochen: {Path.GetFileName(result.MediaPath)}",
            $"Отменено: {Path.GetFileName(result.MediaPath)}",
            $"Cancelled: {Path.GetFileName(result.MediaPath)}"),
        _ => ErrorText(result),
    };

    private static string CompletedText(TranscriptionResult result) => L(
        $"Fertig nach {FormatDuration(result.Elapsed)}: {result.TargetPath}",
        $"Готово за {FormatDuration(result.Elapsed)}: {result.TargetPath}",
        $"Done after {FormatDuration(result.Elapsed)}: {result.TargetPath}");

    /// <summary>Error description for the log and the status column tooltip.</summary>
    public static string ErrorText(TranscriptionResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.Detail)
            ? string.Empty
            : L($" Letzte Meldung: {result.Detail}", $" Последнее сообщение: {result.Detail}", $" Last message: {result.Detail}");
        var exitCode = FormatExitCode(result.ExitCode);

        if (result.CudaProblem != CudaProblem.None)
        {
            return L(
                $"Fehler: Die Grafikkarte konnte nicht verwendet werden – {CudaProblemShort(result.CudaProblem)} (Code {exitCode}).{detail}",
                $"Ошибка: не удалось использовать видеокарту – {CudaProblemShort(result.CudaProblem)} (код {exitCode}).{detail}",
                $"Error: the graphics card could not be used – {CudaProblemShort(result.CudaProblem)} (code {exitCode}).{detail}");
        }

        return result.Error switch
        {
            JobError.MediaNotFound => L(
                $"Fehler: Datei nicht gefunden: {result.Detail}",
                $"Ошибка: файл не найден: {result.Detail}",
                $"Error: file not found: {result.Detail}"),
            JobError.StartFailed => L(
                $"Fehler: faster-whisper konnte nicht gestartet werden: {result.Detail}",
                $"Ошибка: не удалось запустить faster-whisper: {result.Detail}",
                $"Error: faster-whisper could not be started: {result.Detail}"),
            JobError.ProcessFailed => L(
                $"Fehler: faster-whisper wurde mit Code {exitCode} beendet.{detail}",
                $"Ошибка: faster-whisper завершился с кодом {exitCode}.{detail}",
                $"Error: faster-whisper exited with code {exitCode}.{detail}"),
            JobError.ResultMissing => L(
                $"Fehler: faster-whisper hat keine Ergebnisdatei geschrieben.{detail}",
                $"Ошибка: faster-whisper не создал файл с результатом.{detail}",
                $"Error: faster-whisper did not write a result file.{detail}"),
            _ => L($"Fehler: {result.Detail}", $"Ошибка: {result.Detail}", $"Error: {result.Detail}"),
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
            "известная проблема при выходе). Расшифровка полная и сохранена.",
            $"Note: faster-whisper crashed only after writing the result (code {exitCode}, " +
            "a known problem when it exits). The transcript is complete and was kept.");
    }

    // ----- Messages -----

    public static string ExeMissing => L(
        "faster-whisper-xxl.exe wurde nicht gefunden. Klicken Sie auf „Wortlaut einrichten“, um es automatisch herunterzuladen, " +
        "oder wählen Sie eine vorhandene Installation über „Durchsuchen…“.",
        "faster-whisper-xxl.exe не найден. Нажмите «Настроить Wortlaut», чтобы загрузить его автоматически, " +
        "или выберите уже установленную программу через «Обзор…».",
        "faster-whisper-xxl.exe was not found. Click “Set up Wortlaut” to download it automatically, " +
        "or select an existing installation with “Browse…”.");

    public static string ModelMissing => L("Bitte ein Modell angeben.", "Укажите модель.", "Please enter a model.");
    public static string DeviceMissing => L("Bitte ein Gerät auswählen.", "Выберите устройство.", "Please choose a device.");
    public static string MediaMissing => L("Bitte eine vorhandene Video- oder Audiodatei wählen.", "Выберите существующий видео- или аудиофайл.", "Please choose an existing video or audio file.");
    public static string FolderMissing => L("Bitte einen vorhandenen Ordner wählen.", "Выберите существующую папку.", "Please choose an existing folder.");
    public static string NothingSelected => L("Es ist keine Datei markiert.", "Не отмечено ни одного файла.", "No file is ticked.");

    public static string NothingToDo => L(
        "Alle markierten Dateien haben bereits ein Transkript und werden übersprungen.",
        "У всех отмеченных файлов уже есть расшифровка – они будут пропущены.",
        "All ticked files already have a transcript and will be skipped.");

    public static string ConfirmClose => L(
        "Eine Transkription läuft noch. Abbrechen und Wortlaut beenden?",
        "Расшифровка ещё идёт. Отменить её и закрыть Wortlaut?",
        "A transcription is still running. Cancel it and close Wortlaut?");

    public static string MediaNotSupported(string extension)
    {
        var supported = string.Join(' ', MediaFiles.SupportedExtensions);
        return L(
            $"Dateien vom Typ „{extension}“ werden nicht unterstützt. Unterstützt: {supported}",
            $"Файлы типа «{extension}» не поддерживаются. Поддерживаются: {supported}",
            $"Files of type “{extension}” are not supported. Supported: {supported}");
    }

    public static string UnexpectedError(string message) =>
        L($"Unerwarteter Fehler: {message}", $"Непредвиденная ошибка: {message}", $"Unexpected error: {message}");

    // ----- Models -----

    public static string ModelsButton => L("Modelle…", "Модели…", "Models…");
    public static string ModelsTitle => L("Modelle", "Модели", "Models");

    public static string ModelsIntro => L(
        "Das Modell ist das „Gehirn“ der Spracherkennung. Größere Modelle erkennen genauer, sind aber langsamer " +
        "und brauchen mehr Platz. Jedes Modell wird nur einmal heruntergeladen.",
        "Модель – это «мозг» распознавания речи. Большие модели распознают точнее, но работают медленнее " +
        "и занимают больше места. Каждая модель загружается только один раз.",
        "The model is the “brain” of the speech recognition. Larger models recognize more accurately but are slower " +
        "and need more space. Each model is downloaded only once.");

    public static string ModelColumnName => L("Modell", "Модель", "Model");
    public static string ModelColumnSize => L("Größe", "Размер", "Size");
    public static string ModelColumnStatus => L("Status", "Статус", "Status");
    public static string ModelColumnHint => L("Hinweis", "Примечание", "Note");
    public static string ModelInstalled => L("vorhanden", "загружена", "downloaded");
    public static string ModelNotInstalled => L("nicht heruntergeladen", "не загружена", "not downloaded");
    public static string ModelPartial => L("teilweise heruntergeladen", "загружена частично", "partly downloaded");
    public static string ModelDownload => L("Herunterladen", "Загрузить", "Download");
    public static string ModelDelete => L("Löschen", "Удалить", "Delete");
    public static string Close => L("Schließen", "Закрыть", "Close");

    public static string ModelHint(string name) => name switch
    {
        "large-v2" => L("sehr genau – empfohlen für Russisch", "очень точная – рекомендуется для русского", "very accurate – recommended for Russian"),
        "large-v3" => L("sehr genau, erfindet in Pausen manchmal Text", "точная, но в паузах иногда придумывает текст", "very accurate, sometimes invents text in pauses"),
        "large-v3-turbo" => L("fast so genau, deutlich schneller", "почти так же точна, но намного быстрее", "almost as accurate, much faster"),
        "medium" => L("schneller, etwas ungenauer", "быстрее, но немного менее точна", "faster, a little less accurate"),
        "small" => L("sehr schnell, für einfache Aufnahmen", "очень быстрая, для простых записей", "very fast, for simple recordings"),
        _ => string.Empty,
    };

    public static string ModelsFreeSpace(long free) =>
        L($"Frei auf dem Laufwerk: {FormatSize(free)}", $"Свободно на диске: {FormatSize(free)}", $"Free on the drive: {FormatSize(free)}");

    public static string ModelDeleteConfirm(string name, long size) => L(
        $"Modell „{name}“ löschen? Dadurch werden {FormatSize(size)} frei. Es kann später erneut heruntergeladen werden.",
        $"Удалить модель «{name}»? Освободится {FormatSize(size)}. Позже её можно загрузить снова.",
        $"Delete the model “{name}”? This frees {FormatSize(size)}. It can be downloaded again later.");

    public static string ModelReady(string name) => L($"Modell „{name}“ ist bereit.", $"Модель «{name}» готова.", $"Model “{name}” is ready.");

    public static string ModelMissingAsk(string name, long size) => L(
        $"Das Modell „{name}“ ist noch nicht heruntergeladen (ca. {FormatSize(size)}). " +
        "Es wird einmalig von Hugging Face geladen und danach immer wieder verwendet. Jetzt herunterladen?",
        $"Модель «{name}» ещё не загружена (около {FormatSize(size)}). " +
        "Она один раз загрузится с Hugging Face и затем будет использоваться всегда. Загрузить сейчас?",
        $"The model “{name}” is not downloaded yet (about {FormatSize(size)}). " +
        "It is downloaded once from Hugging Face and then used again and again. Download it now?");

    public static string LogUnknownModel(string name) => L(
        $"Hinweis: „{name}“ ist kein bekanntes Modell. Fehlt es, lädt faster-whisper es beim ersten Lauf selbst herunter – " +
        "das kann eine Weile dauern, ohne dass ein Fortschritt angezeigt wird.",
        $"Примечание: «{name}» – неизвестная модель. Если её нет, faster-whisper сам загрузит её при первом запуске – " +
        "это может занять время без отображения прогресса.",
        $"Note: “{name}” is not a known model. If it is missing, faster-whisper downloads it itself on the first run – " +
        "this can take a while without any progress being shown.");

    // ----- Graphics card -----

    public static string GpuCheckButton => L("Prüfen…", "Проверить…", "Check…");

    public static string GpuCheckTooltip => L(
        "Prüft, ob eine passende NVIDIA-Grafikkarte vorhanden ist, und schlägt Gerät und Modell vor.",
        "Проверяет, есть ли подходящая видеокарта NVIDIA, и предлагает устройство и модель.",
        "Checks whether a suitable NVIDIA graphics card is present and suggests device and model.");

    public static string GpuTitle => L("Grafikkarte", "Видеокарта", "Graphics card");

    public static string GpuIntro => L(
        "Mit einer NVIDIA-Grafikkarte erkennt Wortlaut Sprache um ein Vielfaches schneller als mit dem Prozessor. " +
        "Wortlaut prüft, ob eine passende Karte vorhanden ist, und schlägt die richtigen Einstellungen vor.",
        "С видеокартой NVIDIA Wortlaut распознаёт речь во много раз быстрее, чем с процессором. " +
        "Wortlaut проверит, есть ли подходящая видеокарта, и предложит нужные настройки.",
        "With an NVIDIA graphics card Wortlaut recognizes speech many times faster than with the processor. " +
        "Wortlaut checks whether a suitable card is present and suggests the right settings.");

    public static string GpuChecking => L("Prüfe die Grafikkarte …", "Проверка видеокарты…", "Checking the graphics card …");
    public static string GpuApply => L("Vorschlag übernehmen", "Применить", "Apply suggestion");
    public static string GpuRecheck => L("Erneut prüfen", "Проверить снова", "Check again");
    public static string GpuApplied => L("Übernommen. Sie können die Einstellungen jederzeit selbst ändern.", "Применено. Настройки можно в любой момент изменить вручную.", "Applied. You can change the settings yourself at any time.");
    public static string GpuAlreadyApplied => L("Ihre Einstellungen passen bereits.", "Ваши настройки уже подходят.", "Your settings already fit.");

    public static string GpuHeadline(GpuVerdict verdict) => verdict switch
    {
        GpuVerdict.Cuda => L("NVIDIA-Grafikkarte gefunden – schnelle Erkennung", "Найдена видеокарта NVIDIA – быстрое распознавание", "NVIDIA graphics card found – fast recognition"),
        GpuVerdict.CudaLowMemory => L("NVIDIA-Grafikkarte gefunden – wenig Grafikspeicher", "Найдена видеокарта NVIDIA – мало видеопамяти", "NVIDIA graphics card found – little graphics memory"),
        GpuVerdict.Cpu => L(
            "Keine passende Grafikkarte – Erkennung über den Prozessor (langsamer)",
            "Подходящей видеокарты нет – распознавание на процессоре (медленнее)",
            "No suitable graphics card – recognition on the processor (slower)"),
        _ => L("Die Grafikkarte konnte nicht geprüft werden", "Не удалось проверить видеокарту", "The graphics card could not be checked"),
    };

    public static string GpuBadge(GpuVerdict verdict) => verdict switch
    {
        GpuVerdict.Cuda => L("schnell", "быстро", "fast"),
        GpuVerdict.CudaLowMemory => L("wenig Speicher", "мало памяти", "little memory"),
        GpuVerdict.Cpu => L("langsamer", "медленнее", "slower"),
        _ => L("unbekannt", "неизвестно", "unknown"),
    };

    /// <summary>"NVIDIA GeForce GTX 1650 · 4 GB Grafikspeicher".</summary>
    public static string GpuDescription(GpuInfo gpu)
    {
        if (gpu.MemoryMiB is not { } mib)
            return gpu.Name;

        // Graphics memory is sold in whole gigabytes: "4 GB", not "4,00 GB".
        var gigabytes = (mib / 1024d).ToString("0.#", NumberCulture);
        return L($"{gpu.Name} · {gigabytes} GB Grafikspeicher", $"{gpu.Name} · {gigabytes} ГБ видеопамяти", $"{gpu.Name} · {gigabytes} GB graphics memory");
    }

    public static string GpuExplanation(GpuRecommendation recommendation) => recommendation.Verdict switch
    {
        GpuVerdict.Cuda => L(
            $"Empfohlen: Gerät „{recommendation.Device}“ (Grafikkarte) mit dem Modell „{recommendation.Model}“ – sehr genau und schnell.",
            $"Рекомендуется: устройство «{recommendation.Device}» (видеокарта) и модель «{recommendation.Model}» – очень точно и быстро.",
            $"Recommended: device “{recommendation.Device}” (graphics card) with the model “{recommendation.Model}” – very accurate and fast."),
        GpuVerdict.CudaLowMemory => L(
            "Die Karte hat weniger als 4 GB Grafikspeicher. Die großen Modelle passen eventuell nicht hinein. " +
            $"Empfohlen: Gerät „{recommendation.Device}“ mit dem kleineren Modell „{recommendation.Model}“ – fast so genau.",
            "У видеокарты меньше 4 ГБ видеопамяти. Большие модели могут в неё не поместиться. " +
            $"Рекомендуется: устройство «{recommendation.Device}» и модель поменьше «{recommendation.Model}» – почти так же точно.",
            "The card has less than 4 GB of graphics memory. The large models may not fit into it. " +
            $"Recommended: device “{recommendation.Device}” with the smaller model “{recommendation.Model}” – almost as accurate."),
        GpuVerdict.Cpu => L(
            "Die Erkennung funktioniert trotzdem, dauert über den Prozessor aber deutlich länger. " +
            $"Empfohlen: Gerät „{recommendation.Device}“ (Prozessor) mit dem schnelleren Modell „{recommendation.Model}“.",
            "Распознавание всё равно работает, но на процессоре занимает заметно больше времени. " +
            $"Рекомендуется: устройство «{recommendation.Device}» (процессор) и более быстрая модель «{recommendation.Model}».",
            "Recognition still works, but on the processor it takes much longer. " +
            $"Recommended: device “{recommendation.Device}” (processor) with the faster model “{recommendation.Model}”."),
        _ => L(
            "faster-whisper hat keine verwertbare Antwort gegeben. Ihre Einstellungen bleiben unverändert.",
            "faster-whisper не дал понятного ответа. Ваши настройки не изменены.",
            "faster-whisper did not give a usable answer. Your settings stay unchanged."),
    };

    public static string GpuNoNvidiaSmi => L(
        "Name und Grafikspeicher der Karte sind nicht bekannt (nvidia-smi nicht gefunden).",
        "Название и объём видеопамяти неизвестны (nvidia-smi не найден).",
        "Name and graphics memory of the card are not known (nvidia-smi not found).");

    public static string GpuCurrentSettings(string device, string model) =>
        L($"Aktuell eingestellt: {device} · {model}", $"Сейчас выбрано: {device} · {model}", $"Currently set: {device} · {model}");

    public static string GpuModelNotDownloaded(string model) => L(
        $"Das Modell „{model}“ ist noch nicht heruntergeladen. Wortlaut bietet es vor der ersten Transkription zum Download an.",
        $"Модель «{model}» ещё не загружена. Wortlaut предложит загрузить её перед первой расшифровкой.",
        $"The model “{model}” is not downloaded yet. Wortlaut offers to download it before the first transcription.");

    /// <summary>Short reason for the log, e.g. "keine passende NVIDIA-Grafikkarte gefunden".</summary>
    public static string CudaProblemShort(CudaProblem problem) => problem switch
    {
        CudaProblem.NoDevice => L("keine passende NVIDIA-Grafikkarte gefunden", "подходящая видеокарта NVIDIA не найдена", "no suitable NVIDIA graphics card found"),
        CudaProblem.DriverTooOld => L("der Grafiktreiber ist zu alt", "драйвер видеокарты устарел", "the graphics driver is too old"),
        CudaProblem.OutOfMemory => L("der Grafikspeicher reicht nicht aus", "не хватает видеопамяти", "not enough graphics memory"),
        CudaProblem.LibraryMissing => L("eine CUDA-Bibliothek fehlt", "отсутствует библиотека CUDA", "a CUDA library is missing"),
        _ => L("CUDA-Fehler", "ошибка CUDA", "CUDA error"),
    };

    /// <summary>Explanation after a run failed because of the graphics card. Followed by a yes/no question.</summary>
    public static string CudaProblemMessage(CudaProblem problem) => problem switch
    {
        CudaProblem.NoDevice => L(
            "Die Grafikkarte konnte nicht verwendet werden: Es wurde keine passende NVIDIA-Grafikkarte gefunden.",
            "Не удалось использовать видеокарту: подходящая видеокарта NVIDIA не найдена.",
            "The graphics card could not be used: no suitable NVIDIA graphics card was found."),
        CudaProblem.DriverTooOld => L(
            "Die Grafikkarte konnte nicht verwendet werden: Der Grafiktreiber ist zu alt.\n\n" +
            "Bitte den NVIDIA-Grafiktreiber aktualisieren (über die NVIDIA App oder www.nvidia.com/drivers) und Windows neu starten.",
            "Не удалось использовать видеокарту: драйвер видеокарты устарел.\n\n" +
            "Обновите драйвер NVIDIA (через приложение NVIDIA или www.nvidia.com/drivers) и перезагрузите Windows.",
            "The graphics card could not be used: the graphics driver is too old.\n\n" +
            "Please update the NVIDIA graphics driver (with the NVIDIA app or www.nvidia.com/drivers) and restart Windows."),
        CudaProblem.OutOfMemory => L(
            "Die Grafikkarte konnte nicht verwendet werden: Der Grafikspeicher reicht für dieses Modell nicht aus.",
            "Не удалось использовать видеокарту: для этой модели не хватает видеопамяти.",
            "The graphics card could not be used: its memory is not enough for this model."),
        CudaProblem.LibraryMissing => L(
            "Die Grafikkarte konnte nicht verwendet werden: Eine CUDA-Bibliothek von faster-whisper fehlt oder lässt sich nicht laden.\n\n" +
            "Meist hilft es, den NVIDIA-Grafiktreiber zu aktualisieren. Sonst Faster-Whisper-XXL neu einrichten.",
            "Не удалось использовать видеокарту: библиотека CUDA для faster-whisper отсутствует или не загружается.\n\n" +
            "Обычно помогает обновление драйвера NVIDIA. Если нет – установите Faster-Whisper-XXL заново.",
            "The graphics card could not be used: a CUDA library of faster-whisper is missing or cannot be loaded.\n\n" +
            "Updating the NVIDIA graphics driver usually helps. Otherwise set up Faster-Whisper-XXL again."),
        _ => L(
            "Die Grafikkarte konnte nicht verwendet werden (CUDA-Fehler).\n\nOft hilft es, den NVIDIA-Grafiktreiber zu aktualisieren.",
            "Не удалось использовать видеокарту (ошибка CUDA).\n\nЧасто помогает обновление драйвера NVIDIA.",
            "The graphics card could not be used (CUDA error).\n\nUpdating the NVIDIA graphics driver often helps."),
    };

    public static string CudaSwitchToCpuQuestion => L(
        "Auf „cpu“ (Prozessor) umstellen? Die Erkennung funktioniert dann, dauert aber deutlich länger. " +
        "Sie können das später jederzeit wieder ändern.",
        "Переключиться на «cpu» (процессор)? Распознавание будет работать, но заметно медленнее. " +
        "Это можно в любой момент изменить обратно.",
        "Switch to “cpu” (processor)? Recognition will then work but take much longer. " +
        "You can change this back at any time.");

    public static string CudaSwitchModelQuestion(string model) => L(
        $"Auf das kleinere Modell „{model}“ umstellen? Es braucht deutlich weniger Grafikspeicher und ist fast so genau.",
        $"Переключиться на модель поменьше «{model}»? Ей нужно намного меньше видеопамяти, а точность почти та же.",
        $"Switch to the smaller model “{model}”? It needs much less graphics memory and is almost as accurate.");

    public static string CudaRemainingNotProcessed => L(
        "Die übrigen Dateien des Ordners wurden nicht bearbeitet.",
        "Остальные файлы папки не обработаны.",
        "The remaining files of the folder were not processed.");

    // ----- Setup (download Faster-Whisper-XXL) -----

    public static string SetUp => L("Wortlaut einrichten", "Настроить Wortlaut", "Set up Wortlaut");
    public static string SetupTitle => L("Wortlaut einrichten", "Настройка Wortlaut", "Set up Wortlaut");

    public static string SetupIntro => L(
        "Für die Spracherkennung braucht Wortlaut das kostenlose Programm Faster-Whisper-XXL. " +
        "Es wird jetzt einmalig von GitHub heruntergeladen und auf diesem Computer eingerichtet. " +
        "Ihre Aufnahmen bleiben dabei immer auf Ihrem Computer.",
        "Для распознавания речи Wortlaut нужна бесплатная программа Faster-Whisper-XXL. " +
        "Сейчас она один раз загрузится с GitHub и будет установлена на этот компьютер. " +
        "Ваши записи при этом всегда остаются на вашем компьютере.",
        "For speech recognition Wortlaut needs the free program Faster-Whisper-XXL. " +
        "It is now downloaded once from GitHub and set up on this computer. " +
        "Your recordings always stay on your computer.");

    public static string SetupVersionLabel => L("Version", "Версия", "Version");
    public static string SetupDownloadLabel => L("Download", "Загрузка", "Download");
    public static string SetupSpaceLabel => L("Speicherplatz", "Место на диске", "Disk space");
    public static string SetupTargetLabel => L("Ziel", "Папка", "Target");
    public static string SetupLicenseLabel => L("Lizenz", "Лицензия", "License");
    public static string SetupLicenseLink => L("MIT (frei nutzbar) – Projektseite öffnen", "MIT (свободное использование) – открыть страницу проекта", "MIT (free to use) – open the project page");
    public static string SetupRetry => L("Erneut versuchen", "Повторить", "Try again");
    public static string SetupLookingUp => L("Suche die neueste Version …", "Поиск последней версии…", "Looking for the newest version …");
    public static string SetupVerifying => L("Prüfe, ob das Programm startet …", "Проверка запуска программы…", "Checking that the program starts …");
    public static string SetupCancelling => L("Wird abgebrochen …", "Отмена…", "Cancelling …");
    public static string SetupDurationLabel => L("Dauer", "Время", "Duration");

    /// <summary>"etwa 3–14 Minuten (je nach Internetverbindung)".</summary>
    public static string SetupDuration(TimeSpan fast, TimeSpan slow)
    {
        var from = (int)fast.TotalMinutes;
        var to = (int)slow.TotalMinutes;
        return from == to
            ? L($"etwa {from} Min.", $"примерно {from} мин", $"about {from} min")
            : L($"etwa {from}–{to} Minuten (je nach Internetverbindung)", $"примерно {from}–{to} мин (зависит от скорости интернета)", $"about {from}–{to} minutes (depending on the internet connection)");
    }

    // ----- Welcome wizard -----

    public static string WizardStepOf(int step, int count) => L($"Schritt {step} von {count}", $"Шаг {step} из {count}", $"Step {step} of {count}");
    public static string WizardBack => L("Zurück", "Назад", "Back");
    public static string WizardNext => L("Weiter", "Далее", "Next");
    public static string WizardHaveExe => L("Ich habe Faster-Whisper-XXL schon…", "У меня уже есть Faster-Whisper-XXL…", "I already have Faster-Whisper-XXL…");

    public static string WizardConfirmCancel => L(
        "Download abbrechen? Bereits Heruntergeladenes bleibt erhalten. Beim nächsten Start von Wortlaut geht es hier weiter.",
        "Прервать загрузку? Уже загруженное сохранится. При следующем запуске Wortlaut продолжит с этого места.",
        "Cancel the download? What has been downloaded is kept. The next time Wortlaut starts, it continues here.");

    public static string WelcomeTitle => L("Willkommen bei Wortlaut", "Добро пожаловать в Wortlaut", "Welcome to Wortlaut");

    public static string WelcomeText => L(
        "Wortlaut schreibt auf, was in Ihren Video- und Audioaufnahmen gesprochen wird – zum Beispiel " +
        "bei Vorträgen, Interviews oder Gesprächen.",
        "Wortlaut записывает текстом то, что говорится в ваших видео- и аудиозаписях, – например, " +
        "в лекциях, интервью или беседах.",
        "Wortlaut writes down what is said in your video and audio recordings – for example " +
        "in talks, interviews or conversations.");

    public static string WelcomePrivacy => L(
        "Ihre Aufnahmen bleiben auf Ihrem Computer. Die Spracherkennung läuft vollständig bei Ihnen – " +
        "nichts wird ins Internet hochgeladen.",
        "Ваши записи остаются на вашем компьютере. Распознавание речи полностью работает у вас – " +
        "ничего не загружается в интернет.",
        "Your recordings stay on your computer. The speech recognition runs entirely on your side – " +
        "nothing is uploaded to the internet.");

    public static string WelcomeSteps => L(
        "In wenigen Schritten richtet Wortlaut alles ein:\n" +
        "1.  das Spracherkennungsprogramm herunterladen\n" +
        "2.  die Grafikkarte prüfen\n" +
        "3.  ein Sprachmodell herunterladen\n" +
        "Das Internet wird nur für diese Downloads gebraucht.",
        "За несколько шагов Wortlaut всё настроит:\n" +
        "1.  загрузит программу распознавания речи\n" +
        "2.  проверит видеокарту\n" +
        "3.  загрузит языковую модель\n" +
        "Интернет нужен только для этих загрузок.",
        "In a few steps Wortlaut sets everything up:\n" +
        "1.  download the speech recognition program\n" +
        "2.  check the graphics card\n" +
        "3.  download a language model\n" +
        "The internet is only needed for these downloads.");

    public static string WelcomeLanguageLabel => L("Sprache der Oberfläche:", "Язык интерфейса:", "Language of the user interface:");

    public static string InstallTitle => L("Spracherkennung herunterladen", "Загрузка программы распознавания", "Download the speech recognition");

    public static string GraphicsTitle => L("Grafikkarte prüfen", "Проверка видеокарты", "Check the graphics card");
    public static string GpuApplyAndNext => L("Übernehmen und weiter", "Применить и далее", "Apply and continue");

    public static string ModelPageTitle => L("Sprachmodell herunterladen", "Загрузка языковой модели", "Download a language model");

    public static string ModelPageIntro(string name) => L(
        $"Zum Schluss braucht Wortlaut ein Sprachmodell – das „Gehirn“ der Spracherkennung. Eingestellt ist „{name}“. " +
        "Es wird einmalig heruntergeladen und danach immer wieder verwendet.",
        $"Напоследок Wortlaut нужна языковая модель – «мозг» распознавания речи. Выбрана модель «{name}». " +
        "Она загружается один раз и затем используется всегда.",
        $"Finally, Wortlaut needs a language model – the “brain” of the speech recognition. “{name}” is selected. " +
        "It is downloaded once and then used again and again.");

    public static string ModelPageNameLabel => L("Modell", "Модель", "Model");
    public static string ModelLater => L("Später", "Позже", "Later");

    public static string ModelPageLaterHint => L(
        "Mit „Später“ fragt Wortlaut vor der ersten Transkription nach. Weitere Modelle gibt es jederzeit unter „Modelle…“.",
        "Если нажать «Позже», Wortlaut спросит перед первой расшифровкой. Другие модели доступны в любое время через «Модели…».",
        "With “Later”, Wortlaut asks again before the first transcription. More models are available at any time under “Models…”.");

    public static string ModelPageInstalled(string name) =>
        L($"Das Modell „{name}“ ist bereits vorhanden.", $"Модель «{name}» уже загружена.", $"The model “{name}” is already downloaded.");

    public static string DoneTitle => L("Fertig", "Готово", "Done");
    public static string DoneHeadline => L("Wortlaut ist bereit.", "Wortlaut готов к работе.", "Wortlaut is ready.");

    public static string DoneText => L(
        "So geht es los: Ziehen Sie eine Video- oder Audiodatei in das Wortlaut-Fenster und klicken Sie auf " +
        "„Transkribieren“. Der Text wird neben der Datei gespeichert.\n\n" +
        "Alle Einstellungen können Sie später im Hauptfenster ändern. Diesen Assistenten finden Sie unten rechts unter „Extras“.",
        "Как начать: перетащите видео- или аудиофайл в окно Wortlaut и нажмите «Расшифровать». " +
        "Текст сохранится рядом с файлом.\n\n" +
        "Все настройки можно позже изменить в главном окне. Этот мастер находится внизу справа в меню «Сервис».",
        "How to start: drag a video or audio file into the Wortlaut window and click “Transcribe”. " +
        "The text is saved next to the file.\n\n" +
        "You can change all settings later in the main window. You find this wizard at the bottom right under “Tools”.");

    public static string DoneModelMissing(string name) => L(
        $"Das Modell „{name}“ wird vor der ersten Transkription heruntergeladen.",
        $"Модель «{name}» будет загружена перед первой расшифровкой.",
        $"The model “{name}” is downloaded before the first transcription.");

    public static string DoneCopyOption => L(
        "Wortlaut in meinen Benutzerordner kopieren und Verknüpfungen auf dem Desktop und im Startmenü anlegen",
        "Скопировать Wortlaut в мою папку пользователя и создать ярлыки на рабочем столе и в меню «Пуск»",
        "Copy Wortlaut into my user folder and create shortcuts on the desktop and in the start menu");

    public static string DoneCopyHint(string path) => L(
        $"Dann bleibt Wortlaut nicht im Download-Ordner liegen. Ziel: {path}",
        $"Тогда Wortlaut не останется в папке загрузок. Папка: {path}",
        $"Then Wortlaut does not stay in the downloads folder. Target: {path}");

    public static string DoneStart => L("Erste Datei transkribieren", "Расшифровать первый файл", "Transcribe the first file");

    public static string DoneCopyFailed(string reason) => L(
        $"Wortlaut konnte nicht kopiert werden: {reason}",
        $"Не удалось скопировать Wortlaut: {reason}",
        $"Wortlaut could not be copied: {reason}");

    public static string ExtrasMenu => L("Extras", "Сервис", "Tools");
    public static string ExtrasWizard => L("Einrichtungs-Assistent…", "Мастер настройки…", "Setup wizard…");
    public static string ExtrasGpu => L("Grafikkarte prüfen…", "Проверить видеокарту…", "Check graphics card…");

    public static string SetupVersion(string version, bool isFallback) => isFallback
        ? L($"{version} (GitHub gerade nicht erreichbar – bekannte Version)", $"{version} (GitHub сейчас недоступен – известная версия)", $"{version} (GitHub not reachable right now – known version)")
        : version;

    public static string SetupSpace(long required, long? free) => free is { } available
        ? L($"ca. {FormatSize(required)} (frei: {FormatSize(available)})", $"около {FormatSize(required)} (свободно: {FormatSize(available)})", $"about {FormatSize(required)} (free: {FormatSize(available)})")
        : L($"ca. {FormatSize(required)}", $"около {FormatSize(required)}", $"about {FormatSize(required)}");

    public static string SetupDownloading(long done, long? total, double bytesPerSecond, TimeSpan? remaining)
    {
        var amount = total is { } t ? $"{FormatSize(done)} / {FormatSize(t)}" : FormatSize(done);
        var speed = bytesPerSecond > 0 ? $" · {FormatSize((long)bytesPerSecond)}{L("/s", "/с", "/s")}" : string.Empty;
        var rest = remaining is { } r ? $" · {FormatRemaining(r)}" : string.Empty;
        return L($"Herunterladen: {amount}{speed}{rest}", $"Загрузка: {amount}{speed}{rest}", $"Downloading: {amount}{speed}{rest}");
    }

    public static string SetupExtracting(double? fraction) => fraction is { } f
        ? L($"Entpacken: {(int)Math.Floor(f * 100)} %", $"Распаковка: {(int)Math.Floor(f * 100)} %", $"Extracting: {(int)Math.Floor(f * 100)} %")
        : L("Entpacken …", "Распаковка…", "Extracting …");

    public static string SetupErrorText(Core.Setup.SetupError error, string installFolder, long required, long? free) => error switch
    {
        Core.Setup.SetupError.NotEnoughSpace => L(
            $"Auf dem Laufwerk ist nicht genug Platz frei: Benötigt werden ca. {FormatSize(required)}, frei sind {FormatSize(free ?? 0)}. " +
            "Bitte Platz schaffen (z. B. Papierkorb leeren) und erneut versuchen.",
            $"На диске недостаточно места: нужно около {FormatSize(required)}, свободно {FormatSize(free ?? 0)}. " +
            "Освободите место (например, очистите корзину) и повторите попытку.",
            $"There is not enough free space on the drive: about {FormatSize(required)} are needed, {FormatSize(free ?? 0)} are free. " +
            "Please make room (e.g. empty the recycle bin) and try again."),
        Core.Setup.SetupError.DownloadFailed => L(
            "Der Download ist fehlgeschlagen. Bitte die Internetverbindung prüfen und „Erneut versuchen“ klicken – " +
            "der Download wird dort fortgesetzt, wo er aufgehört hat.",
            "Загрузка не удалась. Проверьте подключение к интернету и нажмите «Повторить» – " +
            "загрузка продолжится с того места, где остановилась.",
            "The download failed. Please check the internet connection and click “Try again” – " +
            "the download continues where it stopped."),
        Core.Setup.SetupError.WrongFile => L(
            "Die heruntergeladene Datei ist unvollständig oder beschädigt. Bitte „Erneut versuchen“ klicken.",
            "Загруженный файл неполный или повреждён. Нажмите «Повторить».",
            "The downloaded file is incomplete or damaged. Please click “Try again”."),
        Core.Setup.SetupError.ExtractFailed => L(
            "Das Entpacken ist fehlgeschlagen. Bitte freien Speicherplatz prüfen und erneut versuchen.",
            "Распаковка не удалась. Проверьте свободное место на диске и повторите попытку.",
            "Extracting failed. Please check the free disk space and try again."),
        Core.Setup.SetupError.ExeMissing => L(
            "Nach dem Entpacken fehlt faster-whisper-xxl.exe. Häufig entfernt ein Virenschutzprogramm die Datei fälschlicherweise. " +
            $"Bitte im Virenschutz die Quarantäne prüfen oder eine Ausnahme für diesen Ordner anlegen und erneut versuchen: {installFolder}",
            "После распаковки отсутствует faster-whisper-xxl.exe. Часто антивирус ошибочно удаляет этот файл. " +
            $"Проверьте карантин антивируса или добавьте исключение для этой папки и повторите попытку: {installFolder}",
            "After extracting, faster-whisper-xxl.exe is missing. Often an antivirus program removes the file by mistake. " +
            $"Please check the antivirus quarantine or add an exception for this folder and try again: {installFolder}"),
        Core.Setup.SetupError.ExeDoesNotStart => L(
            "faster-whisper-xxl.exe startet nicht. Möglicherweise blockiert ein Virenschutzprogramm das Programm. " +
            $"Bitte eine Ausnahme für diesen Ordner anlegen und erneut versuchen: {installFolder}",
            "faster-whisper-xxl.exe не запускается. Возможно, его блокирует антивирус. " +
            $"Добавьте исключение для этой папки и повторите попытку: {installFolder}",
            "faster-whisper-xxl.exe does not start. Possibly an antivirus program blocks it. " +
            $"Please add an exception for this folder and try again: {installFolder}"),
        _ => UnexpectedError(error.ToString()),
    };

    public static string SetupDetail(string detail) => L($"Details: {detail}", $"Подробности: {detail}", $"Details: {detail}");

    /// <summary>"1,36 GB" / "1,36 ГБ" / "1.36 GB", "512 MB" / "512 МБ".</summary>
    public static string FormatSize(long bytes)
    {
        const double gigabyte = 1024d * 1024 * 1024;
        const double megabyte = 1024d * 1024;
        return bytes >= gigabyte
            ? (bytes / gigabyte).ToString("0.00", NumberCulture) + L(" GB", " ГБ", " GB")
            : (bytes / megabyte).ToString("0", NumberCulture) + L(" MB", " МБ", " MB");
    }

    /// <summary>"noch ca. 3 Min." / "осталось около 3 мин." / "about 3 min left"</summary>
    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining < TimeSpan.FromMinutes(1))
            return L("noch unter 1 Min.", "осталось меньше минуты", "less than 1 min left");

        var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return L($"noch ca. {minutes} Min.", $"осталось около {minutes} мин.", $"about {minutes} min left");
    }

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

    private static string EnglishFiles(int count) => count == 1 ? "file" : "files";

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
