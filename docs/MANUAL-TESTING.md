# Manual test plan

Automated tests cover the core with a fake runner. These manual checks cover the real
`faster-whisper-xxl.exe` and the user interface. Allow about 15 minutes.

## Preparation

1. Publish the app: `dotnet publish src/Wortlaut -c Release`.
2. Copy `Wortlaut.exe` into an empty folder, e.g. `C:\Temp\Wortlaut\` (no settings file yet).
3. Create a test folder with a space in its name, e.g. `D:\Videos\Wortlaut Test\`, and put into it:
   - a short video (30–60 s) named **`Лекция 12 — Медитация и дыхание.mp4`**,
   - two more short media files, e.g. **`Kirtan Abend.m4a`** and **`Q&A 14.09.mov`**,
   - a text file **`Kirtan Abend.txt`** with any content (simulates an existing transcript).
4. Write down size and "Date modified" of the three media files (Explorer, details view).

## 1. First start

| Step | Expected |
|------|----------|
| Start `Wortlaut.exe`. | Window title "Wortlaut". If `faster-whisper-xxl.exe` is not next to `Wortlaut.exe`, the badge shows **nicht gefunden**. |
| Click **Durchsuchen…** and select `faster-whisper-xxl.exe`. | Badge switches to **gefunden**. Status bar: `large-v2 · cuda · ru · .txt` and "Einstellungen gespeichert in Wortlaut.settings.json". |
| Check the folder of `Wortlaut.exe`. | `Wortlaut.settings.json` exists and contains the path. |

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

## 6. Settings are remembered

| Step | Expected |
|------|----------|
| Change model, language and format, close and restart Wortlaut. | All settings, the last file and the last folder are restored. |
