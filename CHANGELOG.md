# Changelog

All notable changes to Wortlaut. The section of a version becomes its release notes on GitHub
(see [Releasing](README.md#releasing)). Versions follow [semantic versioning](https://semver.org/).

## [Unreleased]

## [1.0.0]

The first public version of Wortlaut – the successor of the in-house tool “AnandaVidyaHelper”.

### Transcribing
- Transcribe a single video or audio file, or a whole folder (optionally with subfolders) one file after
  another. A failed file does not stop the folder run.
- Output as plain text, JSON, subtitles (SRT) or WebVTT, next to the recording under the same name.
- “Whole sentences”: every segment starts with a new sentence.
- Live log, progress in percent and remaining length, cancel at any time.
- Recordings are never renamed, moved or deleted; Wortlaut works on a hard link (or a copy).
- Existing transcripts are skipped unless overwriting is chosen.

### Setup for beginners
- Welcome wizard on the first start: downloads Faster-Whisper-XXL, checks the graphics card, downloads the
  language model – with progress, cancel and resume. Optionally copies Wortlaut to the user folder and creates
  shortcuts.
- Graphics card check with a suggestion for device and model; plain-language help when the graphics card
  cannot be used, with the offer to switch to the processor or a smaller model.
- Model overview: download, resume and delete models.

### Everyday use
- User interface in German (default), English and Russian.
- Built-in help in plain language (“? Help”, F1, “?” next to every setting).
- Optional check for new versions on start (only the version number is requested from GitHub).
- Settings are remembered; one self-contained `Wortlaut.exe`, no installation needed.
