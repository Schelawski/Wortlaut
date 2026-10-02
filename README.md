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

The user interface is available in German, English and Russian.

> **Wortlaut is free of charge.** The only official source is this repository,
> [github.com/Schelawski/Wortlaut](https://github.com/Schelawski/Wortlaut). If you paid for Wortlaut, you paid
> for something you can get here for free – and copies from other sources may have been changed.

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
- User interface in German, English and Russian. German is the default (Russian on a Russian Windows); the
  language can be switched at the bottom right of the window and on the first page of the welcome wizard.
- Built-in help in plain language (see [Help](#help)).
- Ships as one self-contained `Wortlaut.exe` – no .NET installation needed.

## Requirements

- Windows 10 or 11, 64-bit.
- [Faster-Whisper-XXL](https://github.com/Purfview/whisper-standalone-win/releases) (the standalone
  `faster-whisper-xxl.exe`, it brings its own `ffmpeg.exe`). It can live in any folder.
- For the device `cuda`: an NVIDIA GPU with a current driver. Without one, choose the device `cpu`
  (much slower). **Check…** next to the device box tells you which one fits (see [Graphics card](#graphics-card)).
- The Whisper models are downloaded once (several GB for the `large` models, see [Models](#models)).

## Getting started

1. Get `Wortlaut.exe` (see [Build](#build)) and put it into any folder.
2. Start it. Wortlaut looks for `faster-whisper-xxl.exe` next to `Wortlaut.exe`, in the current directory
   and in `%LOCALAPPDATA%\Wortlaut` (also in a `Faster-Whisper-XXL` subfolder). If it is not found, the
   [welcome wizard](#welcome-wizard) sets everything up. With an existing installation the main window opens
   directly.
3. Choose the settings in the **Faster-Whisper** box (names as in the English UI):

   | Setting  | Values                                                                        |
   |----------|-------------------------------------------------------------------------------|
   | Model    | `large-v2` (default), `large-v3`, `large-v3-turbo`, `medium`, `small` or any other name faster-whisper knows |
   | Device   | `cuda` (default), `cpu`                                                        |
   | Language | Russian (ru, default), German (de), English (en), Detect automatically, or type any other language code |
   | Format   | Text (.txt, default), JSON (.json), Subtitles (.srt), WebVTT (.vtt)            |
   | Whole sentences | On (default): every segment starts with a new sentence and sentences are not cut (`--sentence`). Applies to Text, SRT and VTT; JSON keeps Whisper's original segments. |

   "Text" is plain text without timestamps (with "Whole sentences": one sentence per line). The status bar
   at the bottom shows the active settings, e.g. `large-v2 · cuda · ru · .txt`.

### Welcome wizard

On the first start (no `faster-whisper-xxl.exe` found) a wizard guides through the setup in five steps:

1. **Welcome** – choose the UI language (Deutsch / English / Русский). Your recordings stay on your computer.
2. **Download the speech recognition** – version, download size, rough duration, required disk space and
   target, then **Set up Wortlaut** downloads the newest Faster-Whisper-XXL from its
   [official GitHub release](https://github.com/Purfview/whisper-standalone-win/releases) (about 1.4 GB,
   about 4.5 GB unpacked) into `%LOCALAPPDATA%\Wortlaut\Faster-Whisper-XXL` – no administrator rights needed.
   The free disk space is checked first, and the program is test-started before it is used.
3. **Check the graphics card** – see [Graphics card](#graphics-card); the suggestion is applied with one click.
4. **Download a language model** – the selected model with progress (or **Later**: Wortlaut then asks
   before the first transcription).
5. **Done** – **Transcribe the first file** opens the main window. If Wortlaut runs from the downloads
   folder, it offers to copy itself to `%LOCALAPPDATA%\Wortlaut` and to create shortcuts on the desktop and
   in the start menu.

**I already have Faster-Whisper-XXL…** selects an existing installation and skips the download. The wizard
can be closed at any time: downloads keep what they have and continue, and on the next start the wizard
opens at the same step. Later it is available under **Tools → Setup wizard…** at the bottom
right of the main window (also via **Set up Wortlaut** while no installation is found).

### Models

**Models…** next to the model box opens an overview of the models with size, status and a short hint.
Models are downloaded from Hugging Face into the `_models` folder next to `faster-whisper-xxl.exe` (where
Faster-Whisper-XXL looks for them) – with progress, cancel and resume – and can be deleted to free space.
If the selected model is missing when a transcription starts, Wortlaut offers to download it first, so the
first run does not silently download several gigabytes.

| Model | Download | Source |
|-------|----------|--------|
| large-v2 | 2.9 GB | `Systran/faster-whisper-large-v2` |
| large-v3 | 2.9 GB | `Systran/faster-whisper-large-v3` |
| large-v3-turbo | 1.5 GB | `Purfview/faster-whisper-large-v3-turbo` |
| medium | 1.4 GB | `Systran/faster-whisper-medium` |
| small | 0.5 GB | `Systran/faster-whisper-small` |

Model names typed by hand are passed to faster-whisper unchanged; it downloads them itself if needed.

### Graphics card

**Check…** next to the device box checks which graphics card faster-whisper can use and suggests matching
settings (also under **Tools → Check graphics card…**). The welcome wizard runs this check as step 3.

| Result | Suggestion |
|--------|------------|
| NVIDIA card found | `cuda` + `large-v2` (most accurate for Russian) |
| NVIDIA card with less than 4 GB graphics memory | `cuda` + `large-v3-turbo` (the large models may not fit) |
| No usable card | `cpu` + `large-v3-turbo` (almost as accurate, and on the processor much faster than the large models) |

**Apply suggestion** applies the suggestion; device and model can still be changed by hand at any time.
Whether a card is usable is decided by `faster-whisper-xxl.exe --checkcuda`. The name and memory of the card
come from `nvidia-smi` (installed with the NVIDIA driver) and are only shown if it is available.

If a transcription fails because the graphics card cannot be used (no CUDA device, driver too old, out of
graphics memory, missing CUDA library), Wortlaut explains the cause in plain words and offers a setting that
works: a smaller model when a large one does not fit into the graphics memory, the processor (`cpu`)
otherwise. A folder run stops at the first such error, because every following file would fail the same way.

### Tab "Single file"

1. Choose the file with **Choose file…** or drop it anywhere onto the window.
2. **Result** shows where the transcript will be written. If it already exists, it is skipped – tick
   **Overwrite existing transcript** to replace it.
3. Click **Transcribe**. The log shows the faster-whisper output, the progress bar the progress.
   **Cancel** stops faster-whisper immediately and removes all temporary files.

### Tab "Whole folder"

1. Choose the folder with **Choose folder…** or drop a folder onto the window. **Refresh**
   reloads the list.
2. Options:
   - **Skip files that already have a transcript** (default on) – otherwise existing transcripts
     are replaced.
   - **Include subfolders** (default off) – also lists files in subfolders.
3. The list shows every supported file with its duration and status. Untick files you do not want;
   the checkbox in the header marks or unmarks all.
4. Click **Transcribe all**. The files are processed one after another (one GPU). The status
   column shows *waiting*, *running (xx %)*, *done*, *skipped*, *error* (hover for details) or
   *cancelled*; the counter shows "n of m". At the end the log contains a summary.
5. **Cancel** stops the running file and the rest of the queue.

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

### Help

**? Help** in the status bar or **F1** opens the help window: a list of topics on the left (what Wortlaut
does and privacy, first steps, models, device, language, formats, whole folder, problems and solutions,
uninstalling, licenses), the text on the right. F1 opens the topic of the focused setting or tab. The small
**?** next to Model, Device, Language, Format and Whole sentences explains the setting in one or two sentences
when the mouse rests on it, and opens the matching topic when clicked.

The help texts are plain Markdown files embedded into the exe:
[`help.de.md`](src/Wortlaut/Help/help.de.md), [`help.en.md`](src/Wortlaut/Help/help.en.md) and
[`help.ru.md`](src/Wortlaut/Help/help.ru.md) in `src/Wortlaut/Help`. Each topic starts with `# id | Title`;
`##` subheadings, `-` and `1.` lists, `**bold**` and `` `code` `` are supported. All files must contain the
same topics in the same order, and the button names they mention must match the user interface – the unit
tests check both.

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
  UI/      WinForms: MainForm, one UserControl per tab, all texts in UiText; UI/Wizard: welcome wizard
  Help/    help texts (help.de.md, help.en.md, help.ru.md), embedded into the exe
tests/Wortlaut.Tests/   xUnit tests for the core (a fake runner replaces faster-whisper)
docs/MANUAL-TESTING.md  manual test plan
```

## License

© 2026 A. Schelawski – [GNU General Public License v3.0](LICENSE)

Third-party components:
- [Faster-Whisper-XXL](https://github.com/Purfview/whisper-standalone-win) (MIT) – downloaded from its official
  release on request, not bundled.
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT) – extracts the Faster-Whisper-XXL archive.
