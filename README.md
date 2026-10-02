# Wortlaut

Wortlaut is a small Windows desktop app that transcribes video and audio files with
[Faster-Whisper-XXL](https://github.com/Purfview/whisper-standalone-win). It is the successor of
the in-house tool "AnandaVidyaHelper".

Pick a file (or a whole folder), choose model, device, language and output format, and Wortlaut
writes the transcript next to the media file under the same name:

```
D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.mp4
D:\Videos\Satsang 2026-09\Лекция 12 — Медитация и дыхание.txt   <- new
```

The user interface is available in German and Russian.

## Features

- **Single file** – choose a file from any folder via dialog or drag & drop and transcribe it.
- **Whole folder** – list all supported files of a folder (optionally with subfolders), mark the ones you
  want and transcribe them one after another. A failed file does not stop the queue.
- Output formats: plain text (`.txt`), JSON (`.json`), subtitles (`.srt`) and WebVTT (`.vtt`).
- Live log of the faster-whisper output, progress in percent, cancel at any time.
- Existing transcripts are skipped unless you ask to overwrite them.
- **Your media files are never renamed, moved or deleted.** Wortlaut works on a hard link (or a copy)
  in a temporary folder next to the file.
- Unicode file names (e.g. Cyrillic) and paths with spaces are fully supported.
- Settings are remembered between sessions.
- User interface in German and Russian. On the first start Wortlaut follows the Windows display language
  (Russian → Russian, otherwise German); the language can be switched at the bottom right of the window.
- Ships as one self-contained `Wortlaut.exe` – no .NET installation needed.

## Requirements

- Windows 10 or 11, 64-bit.
- [Faster-Whisper-XXL](https://github.com/Purfview/whisper-standalone-win/releases) (the standalone
  `faster-whisper-xxl.exe`, it brings its own `ffmpeg.exe`). It can live in any folder.
- For the device `cuda`: an NVIDIA GPU with a current driver. Without one, choose the device `cpu`
  (much slower).
- The Whisper models are downloaded by faster-whisper on first use (several GB for the `large` models).

## Getting started

1. Get `Wortlaut.exe` (see [Build](#build)) and put it into any folder.
2. Start it. On the first start Wortlaut looks for `faster-whisper-xxl.exe` next to `Wortlaut.exe`
   and in the current directory (also in a `Faster-Whisper-XXL` subfolder). If it is not found, the
   badge next to the path shows **nicht gefunden** – use **Durchsuchen…** to select it.
3. Choose the settings in the **Faster-Whisper** box:

   | Setting  | Values                                                                        |
   |----------|-------------------------------------------------------------------------------|
   | Modell   | `large-v2` (default), `large-v3`, `large-v3-turbo`, `medium`, `small` or any other name faster-whisper knows |
   | Gerät    | `cuda` (default), `cpu`                                                        |
   | Sprache  | Russisch (ru, default), Deutsch (de), Englisch (en), Automatisch erkennen, or type any other language code |
   | Format   | Text (.txt, default), JSON (.json), Untertitel (.srt), WebVTT (.vtt)           |
   | Ganze Sätze | On (default): every segment starts with a new sentence and sentences are not cut (`--sentence`). Applies to Text, SRT and VTT; JSON keeps Whisper's original segments. |

   "Text" is plain text without timestamps (with "Ganze Sätze": one sentence per line). The status bar
   at the bottom shows the active settings, e.g. `large-v2 · cuda · ru · .txt`.

### Tab "Einzelne Datei" (single file)

1. Choose the file with **Datei wählen…** or drop it anywhere onto the window.
2. **Ergebnis** shows where the transcript will be written. If it already exists, it is skipped – tick
   **Vorhandenes Transkript überschreiben** to replace it.
3. Click **Transkribieren**. The log shows the faster-whisper output, the progress bar the progress.
   **Abbrechen** stops faster-whisper immediately and removes all temporary files.

### Tab "Ganzer Ordner" (whole folder)

1. Choose the folder with **Ordner wählen…** or drop a folder onto the window. **Aktualisieren**
   reloads the list.
2. Options:
   - **Dateien mit vorhandenem Transkript überspringen** (default on) – otherwise existing transcripts
     are replaced.
   - **Unterordner einbeziehen** (default off) – also lists files in subfolders.
3. The list shows every supported file with its duration and status. Untick files you do not want;
   the checkbox in the header marks or unmarks all.
4. Click **Alle transkribieren**. The files are processed one after another (one GPU). The status
   column shows *wartet*, *läuft (xx %)*, *fertig*, *übersprungen*, *Fehler* (hover for details) or
   *abgebrochen*; the counter shows "n von m". At the end the log contains a summary.
5. **Abbrechen** stops the running file and the rest of the queue.

Supported file types: `.mp4 .mp3 .ogg .m4a .mov .avi .wmv .webm .mpeg .m2p .mpg`.

While a transcription runs, the settings and the start buttons are locked. Closing the window during a
run asks for confirmation and then cancels cleanly.

### How a transcription works

1. The transcript path is `{media folder}\{media name}{format extension}`.
2. Wortlaut creates `{media folder}\.wortlaut-tmp\{job id}\` and puts a hard link `job.{ext}` to the
   media file there. If the file system does not support hard links (e.g. FAT32/exFAT) or the file is
   read-only, the file is copied instead (noted in the log).
3. `faster-whisper-xxl.exe` is started directly (no batch file, no `cmd.exe`) with the work folder as
   output folder.
4. On success the result is moved next to the media file. On error the log shows the exit code and the
   last error line.
5. The work folder is always deleted, and `.wortlaut-tmp` too once it is empty.

If Wortlaut is killed during a run, a `.wortlaut-tmp` folder may remain. It is reported in the log the
next time the folder is loaded; delete it by hand after checking it.

### Settings file

Settings are stored in `Wortlaut.settings.json` next to `Wortlaut.exe`. If that folder is not writable
(e.g. under `C:\Program Files`), Wortlaut uses `%APPDATA%\Wortlaut\Wortlaut.settings.json` instead. The
status bar shows which file is used.

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows.

```powershell
dotnet build            # build the app and the tests
dotnet test             # run the unit tests
dotnet publish src/Wortlaut -c Release
```

`dotnet publish` produces a single self-contained executable (the .NET runtime is embedded):

```
src\Wortlaut\bin\Release\net10.0-windows\win-x64\publish\Wortlaut.exe
```

### Project layout

```
Wortlaut.sln
src/Wortlaut/
  Core/    UI-independent logic: settings, runner, transcription job, folder queue
  UI/      WinForms: MainForm, one UserControl per tab, all texts in UiText
tests/Wortlaut.Tests/   xUnit tests for the core (a fake runner replaces faster-whisper)
docs/MANUAL-TESTING.md  manual test plan
```

## License

[GNU General Public License v3.0](LICENSE)
