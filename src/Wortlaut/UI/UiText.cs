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
        "faster-whisper-xxl.exe wurde nicht gefunden. Klicken Sie auf „Wortlaut einrichten“, um es automatisch herunterzuladen, " +
        "oder wählen Sie eine vorhandene Installation über „Durchsuchen…“.",
        "faster-whisper-xxl.exe не найден. Нажмите «Настроить Wortlaut», чтобы загрузить его автоматически, " +
        "или выберите уже установленную программу через «Обзор…».");

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

    // ----- Models -----

    public static string ModelsButton => L("Modelle…", "Модели…");
    public static string ModelsTitle => L("Modelle", "Модели");

    public static string ModelsIntro => L(
        "Das Modell ist das „Gehirn“ der Spracherkennung. Größere Modelle erkennen genauer, sind aber langsamer " +
        "und brauchen mehr Platz. Jedes Modell wird nur einmal heruntergeladen.",
        "Модель – это «мозг» распознавания речи. Большие модели распознают точнее, но работают медленнее " +
        "и занимают больше места. Каждая модель загружается только один раз.");

    public static string ModelColumnName => L("Modell", "Модель");
    public static string ModelColumnSize => L("Größe", "Размер");
    public static string ModelColumnStatus => L("Status", "Статус");
    public static string ModelColumnHint => L("Hinweis", "Примечание");
    public static string ModelInstalled => L("vorhanden", "загружена");
    public static string ModelNotInstalled => L("nicht heruntergeladen", "не загружена");
    public static string ModelPartial => L("teilweise heruntergeladen", "загружена частично");
    public static string ModelDownload => L("Herunterladen", "Загрузить");
    public static string ModelDelete => L("Löschen", "Удалить");
    public static string Close => L("Schließen", "Закрыть");

    public static string ModelHint(string name) => name switch
    {
        "large-v2" => L("sehr genau – empfohlen für Russisch", "очень точная – рекомендуется для русского"),
        "large-v3" => L("sehr genau, erfindet in Pausen manchmal Text", "точная, но в паузах иногда придумывает текст"),
        "large-v3-turbo" => L("fast so genau, deutlich schneller", "почти так же точна, но намного быстрее"),
        "medium" => L("schneller, etwas ungenauer", "быстрее, но немного менее точна"),
        "small" => L("sehr schnell, für einfache Aufnahmen", "очень быстрая, для простых записей"),
        _ => string.Empty,
    };

    public static string ModelsFreeSpace(long free) => L($"Frei auf dem Laufwerk: {FormatSize(free)}", $"Свободно на диске: {FormatSize(free)}");

    public static string ModelDeleteConfirm(string name, long size) => L(
        $"Modell „{name}“ löschen? Dadurch werden {FormatSize(size)} frei. Es kann später erneut heruntergeladen werden.",
        $"Удалить модель «{name}»? Освободится {FormatSize(size)}. Позже её можно загрузить снова.");

    public static string ModelReady(string name) => L($"Modell „{name}“ ist bereit.", $"Модель «{name}» готова.");

    public static string ModelMissingAsk(string name, long size) => L(
        $"Das Modell „{name}“ ist noch nicht heruntergeladen (ca. {FormatSize(size)}). " +
        "Es wird einmalig von Hugging Face geladen und danach immer wieder verwendet. Jetzt herunterladen?",
        $"Модель «{name}» ещё не загружена (около {FormatSize(size)}). " +
        "Она один раз загрузится с Hugging Face и затем будет использоваться всегда. Загрузить сейчас?");

    public static string LogUnknownModel(string name) => L(
        $"Hinweis: „{name}“ ist kein bekanntes Modell. Fehlt es, lädt faster-whisper es beim ersten Lauf selbst herunter – " +
        "das kann eine Weile dauern, ohne dass ein Fortschritt angezeigt wird.",
        $"Примечание: «{name}» – неизвестная модель. Если её нет, faster-whisper сам загрузит её при первом запуске – " +
        "это может занять время без отображения прогресса.");

    // ----- Setup (download Faster-Whisper-XXL) -----

    public static string SetUp => L("Wortlaut einrichten", "Настроить Wortlaut");
    public static string SetupTitle => L("Wortlaut einrichten", "Настройка Wortlaut");

    public static string SetupIntro => L(
        "Für die Spracherkennung braucht Wortlaut das kostenlose Programm Faster-Whisper-XXL. " +
        "Es wird jetzt einmalig von GitHub heruntergeladen und auf diesem Computer eingerichtet. " +
        "Ihre Aufnahmen bleiben dabei immer auf Ihrem Computer.",
        "Для распознавания речи Wortlaut нужна бесплатная программа Faster-Whisper-XXL. " +
        "Сейчас она один раз загрузится с GitHub и будет установлена на этот компьютер. " +
        "Ваши записи при этом всегда остаются на вашем компьютере.");

    public static string SetupVersionLabel => L("Version", "Версия");
    public static string SetupDownloadLabel => L("Download", "Загрузка");
    public static string SetupSpaceLabel => L("Speicherplatz", "Место на диске");
    public static string SetupTargetLabel => L("Ziel", "Папка");
    public static string SetupLicenseLabel => L("Lizenz", "Лицензия");
    public static string SetupLicenseLink => L("MIT (frei nutzbar) – Projektseite öffnen", "MIT (свободное использование) – открыть страницу проекта");
    public static string SetupStart => L("Herunterladen und einrichten", "Загрузить и установить");
    public static string SetupRetry => L("Erneut versuchen", "Повторить");
    public static string SetupFinish => L("Fertig", "Готово");
    public static string SetupLookingUp => L("Suche die neueste Version …", "Поиск последней версии…");
    public static string SetupVerifying => L("Prüfe, ob das Programm startet …", "Проверка запуска программы…");
    public static string SetupCancelling => L("Wird abgebrochen …", "Отмена…");

    public static string SetupDone(string version) => L(
        $"Fertig! Faster-Whisper-XXL {version} ist eingerichtet. Sie können jetzt Dateien transkribieren.",
        $"Готово! Faster-Whisper-XXL {version} установлен. Теперь можно расшифровывать файлы.");

    public static string SetupVersion(string version, bool isFallback) => isFallback
        ? L($"{version} (GitHub gerade nicht erreichbar – bekannte Version)", $"{version} (GitHub сейчас недоступен – известная версия)")
        : version;

    public static string SetupSpace(long required, long? free) => free is { } available
        ? L($"ca. {FormatSize(required)} (frei: {FormatSize(available)})", $"около {FormatSize(required)} (свободно: {FormatSize(available)})")
        : L($"ca. {FormatSize(required)}", $"около {FormatSize(required)}");

    public static string SetupDownloading(long done, long? total, double bytesPerSecond, TimeSpan? remaining)
    {
        var amount = total is { } t ? $"{FormatSize(done)} / {FormatSize(t)}" : FormatSize(done);
        var speed = bytesPerSecond > 0 ? $" · {FormatSize((long)bytesPerSecond)}{L("/s", "/с")}" : string.Empty;
        var rest = remaining is { } r ? $" · {FormatRemaining(r)}" : string.Empty;
        return L($"Herunterladen: {amount}{speed}{rest}", $"Загрузка: {amount}{speed}{rest}");
    }

    public static string SetupExtracting(double? fraction) => fraction is { } f
        ? L($"Entpacken: {(int)Math.Floor(f * 100)} %", $"Распаковка: {(int)Math.Floor(f * 100)} %")
        : L("Entpacken …", "Распаковка…");

    public static string SetupConfirmCancel => L(
        "Einrichtung abbrechen? Der bisherige Download bleibt erhalten und wird beim nächsten Mal fortgesetzt.",
        "Прервать установку? Уже загруженная часть сохранится, и загрузка продолжится в следующий раз.");

    public static string SetupErrorText(Core.Setup.SetupError error, string installFolder, long required, long? free) => error switch
    {
        Core.Setup.SetupError.NotEnoughSpace => L(
            $"Auf dem Laufwerk ist nicht genug Platz frei: Benötigt werden ca. {FormatSize(required)}, frei sind {FormatSize(free ?? 0)}. " +
            "Bitte Platz schaffen (z. B. Papierkorb leeren) und erneut versuchen.",
            $"На диске недостаточно места: нужно около {FormatSize(required)}, свободно {FormatSize(free ?? 0)}. " +
            "Освободите место (например, очистите корзину) и повторите попытку."),
        Core.Setup.SetupError.DownloadFailed => L(
            "Der Download ist fehlgeschlagen. Bitte die Internetverbindung prüfen und „Erneut versuchen“ klicken – " +
            "der Download wird dort fortgesetzt, wo er aufgehört hat.",
            "Загрузка не удалась. Проверьте подключение к интернету и нажмите «Повторить» – " +
            "загрузка продолжится с того места, где остановилась."),
        Core.Setup.SetupError.WrongFile => L(
            "Die heruntergeladene Datei ist unvollständig oder beschädigt. Bitte „Erneut versuchen“ klicken.",
            "Загруженный файл неполный или повреждён. Нажмите «Повторить»."),
        Core.Setup.SetupError.ExtractFailed => L(
            "Das Entpacken ist fehlgeschlagen. Bitte freien Speicherplatz prüfen und erneut versuchen.",
            "Распаковка не удалась. Проверьте свободное место на диске и повторите попытку."),
        Core.Setup.SetupError.ExeMissing => L(
            "Nach dem Entpacken fehlt faster-whisper-xxl.exe. Häufig entfernt ein Virenschutzprogramm die Datei fälschlicherweise. " +
            $"Bitte im Virenschutz die Quarantäne prüfen oder eine Ausnahme für diesen Ordner anlegen und erneut versuchen: {installFolder}",
            "После распаковки отсутствует faster-whisper-xxl.exe. Часто антивирус ошибочно удаляет этот файл. " +
            $"Проверьте карантин антивируса или добавьте исключение для этой папки и повторите попытку: {installFolder}"),
        Core.Setup.SetupError.ExeDoesNotStart => L(
            "faster-whisper-xxl.exe startet nicht. Möglicherweise blockiert ein Virenschutzprogramm das Programm. " +
            $"Bitte eine Ausnahme für diesen Ordner anlegen und erneut versuchen: {installFolder}",
            "faster-whisper-xxl.exe не запускается. Возможно, его блокирует антивирус. " +
            $"Добавьте исключение для этой папки и повторите попытку: {installFolder}"),
        _ => UnexpectedError(error.ToString()),
    };

    public static string SetupDetail(string detail) => L($"Details: {detail}", $"Подробности: {detail}");

    /// <summary>"1,36 GB" / "1,36 ГБ", "512 MB" / "512 МБ".</summary>
    public static string FormatSize(long bytes)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(Language == UiLanguage.Russian ? "ru-RU" : "de-DE");
        const double gigabyte = 1024d * 1024 * 1024;
        const double megabyte = 1024d * 1024;
        return bytes >= gigabyte
            ? (bytes / gigabyte).ToString("0.00", culture) + L(" GB", " ГБ")
            : (bytes / megabyte).ToString("0", culture) + L(" MB", " МБ");
    }

    /// <summary>"noch ca. 3 Min." / "осталось около 3 мин."</summary>
    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining < TimeSpan.FromMinutes(1))
            return L("noch unter 1 Min.", "осталось меньше минуты");

        var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return L($"noch ca. {minutes} Min.", $"осталось около {minutes} мин.");
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
