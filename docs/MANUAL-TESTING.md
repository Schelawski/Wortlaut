# Manual test plan

Automated tests cover the core with a fake runner. These manual checks cover the real
`faster-whisper-xxl.exe` and the user interface. Allow about 15 minutes.

Button and label names refer to the German user interface (the default). For the other languages, switch at
the bottom right of the main window and repeat the visual checks: all texts translated, nothing cut off,
the settings line fits into one line at the default window size (except Russian at 125 %).

## Preparation

1. Publish the app: `dotnet publish src/Wortlaut -c Release`.
2. Copy `Wortlaut.exe` into an empty folder, e.g. `C:\Temp\Wortlaut\` (no settings file yet).
3. Create a test folder with a space in its name, e.g. `D:\Videos\Wortlaut Test\`, and put into it:
   - a short video (30–60 s) named **`Лекция 12 — Медитация и дыхание.mp4`**,
   - two more short media files, e.g. **`Kirtan Abend.m4a`** and **`Q&A 14.09.mov`**,
   - a text file **`Kirtan Abend.txt`** with any content (simulates an existing transcript).
4. Write down size and "Date modified" of the three media files (Explorer, details view).

## 1. First start

Best done in a **fresh Windows user account** (no `%LOCALAPPDATA%\Wortlaut`, no settings file), with
`Wortlaut.exe` in that account's `Downloads` folder. Needs about 8 GB of free space and a working internet
connection.

| Step | Expected |
|------|----------|
| Start `Wortlaut.exe`. | The wizard "Wortlaut einrichten" opens at "Schritt 1 von 5 – Willkommen bei Wortlaut", in the Windows display language. The green note says that recordings stay on the computer. |
| Switch the language to **Русский** and back. | All texts of the page and the buttons change immediately. |
| **Weiter**. | Step 2 shows version, download size (about 1.3 GB), duration ("etwa 3–14 Minuten"), disk space and target `%LOCALAPPDATA%\Wortlaut\Faster-Whisper-XXL`. |
| **Wortlaut einrichten**, after a few seconds **Abbrechen** → **Ja**. | The wizard closes, then the main window opens with **nicht gefunden**. |
| Restart `Wortlaut.exe`. | The wizard opens directly at step 2. **Wortlaut einrichten** continues the download (the downloaded size does not start at 0), then extracts and checks the program. |
| Step 3 appears by itself. | Graphics card result and suggestion (see section 6). **Übernehmen und weiter**. |
| Step 4: **Herunterladen**. | The model downloads with progress and remaining time; then step 5 appears. |
| Step 5: leave "Wortlaut in meinen Benutzerordner kopieren …" ticked, **Erste Datei transkribieren**. | Wortlaut restarts from `%LOCALAPPDATA%\Wortlaut\Wortlaut.exe` with the main window: badge **gefunden**, device and model as suggested. "Wortlaut" shortcuts exist on the desktop and in the start menu. |
| Start Wortlaut again via the desktop shortcut. | The main window opens directly, no wizard. |
| **Extras → Einrichtungs-Assistent…**, then **Abbrechen** on step 1; restart. | No wizard on the restart (opening it from the menu is not remembered). |

Variant: in step 1 click **Ich habe Faster-Whisper-XXL schon…** and select an existing
`faster-whisper-xxl.exe` – the wizard continues with step 3.

## 2. Single file – Text (.txt)

| Step | Expected |
|------|----------|
| Tab **Einzelne Datei**: drag `Лекция 12 — Медитация и дыхание.mp4` from Explorer onto the window. | File box shows the full path. **Ergebnis** shows `D:\Videos\Wortlaut Test\Лекция 12 — Медитация и дыхание.txt`. |
| Click **Transkribieren**. | Settings and start button are locked, **Abbrechen** is active. Log shows the command line and the transcript lines (Cyrillic readable). Progress bar and percentage advance. |
| While it runs, look into the test folder. | A `.wortlaut-tmp` folder exists. The `.mp4` keeps its name. |
| Wait until done. | Log: "Fertig nach …". State "fertig". `Лекция 12 — Медитация и дыхание.txt` exists next to the video, contains plain text without timestamps. `.wortlaut-tmp` is gone. The `.mp4` has the same name, size and date as before. |
| Click **Transkribieren** again. | Log: "Transkript ist bereits vorhanden, übersprungen". Next to **Ergebnis**: "vorhanden – wird übersprungen". |

## 3. Single file – JSON (.json)

| Step | Expected |
|------|----------|
| Set **Format** to **JSON (.json)**. | **Ergebnis** switches to `….json`; status bar ends with `.json`. |
| Click **Transkribieren** and wait. | `Лекция 12 — Медитация и дыхание.json` exists (JSON with `segments`). The `.txt` from step 2 is unchanged. |
| Set **Format** back to **Text (.txt)**. | |

## 4. Folder with three files, one existing transcript, cancel mid-run

| Step | Expected |
|------|----------|
| Delete `Лекция 12 — Медитация и дыхание.txt` from step 2. | |
| Tab **Ganzer Ordner**: **Ordner wählen…** → `D:\Videos\Wortlaut Test`. | Three files, sorted by name, with duration. `Kirtan Abend.m4a` shows **vorhanden, wird übersprungen**, the others **wartet**. Counter: **0 von 2**. |
| Click **Alle transkribieren**. | `Kirtan Abend.m4a` → **übersprungen** (its `.txt` stays unchanged). The first real file shows **läuft (xx %)**, counter and overall progress advance. |
| When the second file has started and shows **läuft**, click **Abbrechen**. | The running file and all remaining ones show **abgebrochen**. Log ends with "Zusammenfassung: … fertig · 1 übersprungen · 0 Fehler · … abgebrochen · Gesamtdauer …". Task Manager shows no `faster-whisper-xxl.exe` any more. |
| Check the test folder. | Transcripts exist only for files marked **fertig**. No `.wortlaut-tmp` folder. All media files keep name, size and date. |
| Click **Alle transkribieren** again and let it finish. | Files with transcripts are skipped, the rest become **fertig**. |

## 5. Closing during a run and leftovers

| Step | Expected |
|------|----------|
| Start a transcription and close the window while it runs. | Question "Eine Transkription läuft noch. Abbrechen und Wortlaut beenden?". **Nein** keeps it running; **Ja** cancels, cleans up and closes. |
| Create an empty folder `.wortlaut-tmp\x` in the test folder and click **Aktualisieren**. | Log shows a note about a leftover work folder. Wortlaut does not delete it. |

## 6. Graphics card

| Step | Expected |
|------|----------|
| Click **Prüfen…** next to the device box. | Window "Grafikkarte". After a few seconds: on a PC with an NVIDIA card **NVIDIA-Grafikkarte gefunden – schnelle Erkennung** with name and memory, suggestion `cuda` + `large-v2`; without one **Keine passende Grafikkarte – Erkennung über den Prozessor (langsamer)**, suggestion `cpu` + `large-v3-turbo`. |
| Click **Vorschlag übernehmen**. | Device and model in the main window change; the status bar shows the new settings. |
| Set the device to `cuda` on a PC without an NVIDIA card (or simulate: start Wortlaut from a command prompt after `set CUDA_VISIBLE_DEVICES=-1`) and transcribe a file. | The log shows "Die Grafikkarte konnte nicht verwendet werden – keine passende NVIDIA-Grafikkarte gefunden" with the CUDA error line. A question offers to switch to `cpu`; **Ja** changes the device. |
| Same with a folder of three files. | The run stops after the first file; the log says the remaining files were not processed. The question appears once. |

## 7. Help

| Step | Expected |
|------|----------|
| Rest the mouse on the small **?** next to "Modell", "Gerät", "Sprache", "Format" and "Ganze Sätze". | A tooltip explains the setting in one or two sentences. |
| Click the **?** next to "Gerät". | The help window opens at "Gerät: Grafikkarte oder Prozessor". |
| Click into the language box and press **F1**. | The same help window switches to "Sprache" (no second window). |
| Switch to the tab "Ganzer Ordner", click into the list, press **F1**. | Topic "Ganzer Ordner". |
| Click **? Hilfe** in the status bar and go through all topics. | Ten topics; text, lists, bold words and paths are formatted and readable; nothing is cut off. |
| Switch the UI language to Русский and open the help again. | All topics in Russian. |
| In the welcome wizard (Extras → Einrichtungs-Assistent…), on step 2 press **F1**. | Topic "Probleme und Lösungen". |

**Acceptance:** one person from the target group per language reads the help and answers:
"Is it understandable without technical knowledge?"

## 8. Settings are remembered

| Step | Expected |
|------|----------|
| Change model, language and format, close and restart Wortlaut. | All settings, the last file and the last folder are restored. |

## 9. Version and update check

| Step | Expected |
|------|----------|
| Start a release build (from the GitHub release). | The window title shows the version, e.g. "Wortlaut 1.0.0"; the file properties (Details) show the same version and "© 2026 A. Schelawski". |
| Start an older release while a newer one exists. | After a few seconds the status bar shows "Neue Version x.y.z – herunterladen"; a click opens the release page on GitHub. |
| **Extras → Beim Start nach neuen Versionen suchen** off, restart. | No hint, no request to GitHub. |
| Start without internet. | No hint and no error message. |
