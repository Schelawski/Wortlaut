Help texts of Wortlaut (English). Format: see src/Wortlaut/Core/Help/HelpDocument.cs.
Same topics in the same order as help.de.md.

# about | What does Wortlaut do?

Wortlaut writes down what is said in video and audio recordings – for example in talks, seminars, interviews or conversations. A recording becomes a text file that you can read, search, print or edit. **Wortlaut is free of charge** (see “Licenses and thanks”).

## Your recordings stay with you

The speech recognition runs entirely on your computer. **No recording and no text is uploaded to the internet.** You need no account and no subscription.

The internet is only needed once: to download the speech recognition program and the language models. After that, Wortlaut also works without internet. In addition, Wortlaut asks GitHub on start whether there is a new version – only the version number is requested, nothing about you or your recordings. You can switch this off under “Tools”.

## Your files stay unchanged

Wortlaut only reads your recordings. They are never renamed, moved or deleted. The text is saved as a new file next to the recording.

## Where does the speech recognition come from?

The actual recognition is done by the free program Faster-Whisper-XXL. It is based on “Whisper”, a freely available speech recognition. Wortlaut is the simple interface for it.

# start | First steps

1. **Choose a file:** drag a video or audio file with the mouse into the Wortlaut window. Or click “Choose file…”.
2. **Check the settings:** at the top are model, device, language and format. The suggested values usually fit. For Russian: language “Russian (ru)”.
3. **Start:** click “Transcribe”. The bar shows how far the recognition has come. With “Cancel” you can stop at any time.
4. **Open the result:** the text is in the same folder as the recording and has the same name – only with a different ending. For example, `Talk.mp4` becomes `Talk.txt`. Under “Result:” you see the exact path beforehand.

## How long does it take?

That depends mainly on whether your computer has a suitable graphics card (see “Device”). The first run takes a little longer because the model is loaded.

## Is there a text already?

If there already is a text next to the recording, Wortlaut skips the file. To replace the old text, tick “Overwrite existing transcript”.

## Supported files

Video and audio in these formats: `.mp4 .mp3 .ogg .m4a .mov .avi .wmv .webm .mpeg .m2p .mpg`.

# models | Models

The model is the “brain” of the speech recognition. Larger models recognize more accurately but need more time and more disk space. Each model is downloaded only once and then used again and again. With “Models…” (next to the model box) you can download and delete models.

- **large-v2** (2.9 GB) – very accurate. **Our recommendation for Russian talks.**
- **large-v3** (2.9 GB) – also very accurate, but during pauses, silence or music it sometimes invents text that was not spoken.
- **large-v3-turbo** (1.5 GB) – almost as accurate as the large models, but much faster. Good without a suitable graphics card or with little graphics memory.
- **medium** (1.4 GB) – faster, but a little less accurate.
- **small** (0.5 GB) – very fast, for simple and clear recordings.

## Which one should I take?

Let Wortlaut decide: click “Check…” next to “Device” and then “Apply suggestion”. If the result is not accurate enough for you, try a larger model.

# device | Device: graphics card or processor

Speech recognition needs a lot of computing power. Wortlaut can do it in two ways:

- **cuda – with the graphics card.** This is many times faster. It only works with an NVIDIA graphics card. “CUDA” is the name of NVIDIA's technology for this.
- **cpu – with the processor.** This works on every computer but takes much longer.

## What does “slower” mean in practice?

As a rough guide for one hour of recording: with a mid-range graphics card, recognition takes about 10 to 20 minutes. With the processor rather one to several hours – depending on the computer and the model. With the processor we recommend the faster model “large-v3-turbo”.

## How do I know what my computer has?

Click “Check…” next to “Device”. Wortlaut checks the graphics card and suggests device and model. “Apply suggestion” sets both.

# language | Language

Here you set the language that is spoken in the recording.

- **A fixed language** (e.g. “Russian (ru)”) is the best choice if you know the language. Recognition is then more reliable.
- **“Detect automatically”** lets Wortlaut determine the language from the first seconds itself. This is handy for mixed folders but can be wrong – for example if the recording starts with music or another language.

You can type other languages by their code, for example `fr` for French or `uk` for Ukrainian.

Note: Wortlaut writes down what is said. It does not translate.

# formats | Formats and “Whole sentences”

The format decides what the result file looks like:

- **Text (.txt)** – plain text without times. The right choice for reading, printing and editing.
- **Subtitles (.srt)** – text with times that match the video. Video players such as VLC show it as subtitles if it has the same name as the video.
- **WebVTT (.vtt)** – subtitles for websites and online video players.
- **JSON (.json)** – for programs and further processing, with all details. Not meant for reading.

## “Whole sentences”

When ticked, every segment starts with a new sentence, and sentences are not split in the middle. With the Text format there is then one sentence per line. This applies to Text, Subtitles and WebVTT – **not to JSON**, which keeps the original segments.

# folder | Whole folder

In the tab “Whole folder” you process many recordings at once.

1. Choose the folder with “Choose folder…” or drag it into the window.
2. The list shows all recordings with length and status. Untick files that should not be processed.
3. Click “Transcribe all”. The files are processed one after another.

## Skipping existing texts

With “Skip files that already have a transcript” (the default), Wortlaut only processes recordings that have no text yet. Without the tick, existing texts are replaced.

## Cancelling and continuing later

“Cancel” stops the running file and all following ones. Finished texts are kept. Simply start the folder again later: thanks to skipping, Wortlaut continues where it stopped.

If one file fails, Wortlaut continues with the next. Move the mouse over “error” to see the reason.

# problems | Problems and solutions

## Windows warns on the first start

If “Windows protected your PC” appears, click **“More info”** and then **“Run anyway”**. Windows shows this warning for programs that are not yet widely used. Only download Wortlaut from the official project page.

## The antivirus program reports faster-whisper

Some antivirus programs wrongly report Faster-Whisper-XXL because it is a large self-extracting program. Wortlaut downloads it only from the developer's official page on GitHub. Add an exception for this folder in your antivirus program: `%LOCALAPPDATA%\Wortlaut`.

## No internet during the setup

The internet is only needed for the downloads. If the connection drops, click “Try again”: the download continues where it stopped. If you already got Faster-Whisper-XXL another way (e.g. on a USB stick), select it with “I already have Faster-Whisper-XXL…”.

## Not enough disk space

The speech recognition program needs about 6 GB, each model another 0.5 to 3 GB. Make room on drive `C:` or delete models you do not need under “Models…”.

## “faster-whisper crashed only after writing the result”

This note is **harmless**. Faster-Whisper-XXL sometimes crashes when it exits, after the text has been written completely. The text is complete and was kept.

## Errors with the graphics card

If the graphics card cannot be used, Wortlaut explains why and offers to switch to the processor (“cpu”) or to a smaller model. Updating the NVIDIA graphics driver (with the NVIDIA app or `www.nvidia.com/drivers`) and restarting Windows often helps. Then check again with “Check…” next to “Device”.

## The recognition invents text or repeats itself

This happens mainly with long pauses, silence or music. Take the model “large-v2” and set the language explicitly instead of using “Detect automatically”.

# uninstall | Uninstalling

Wortlaut installs nothing into Windows and changes no system settings. To remove it, delete:

1. the folder `%LOCALAPPDATA%\Wortlaut` – it contains the speech recognition program, the models and, if present, the copy of Wortlaut. Simply type the address into the address bar of File Explorer.
2. the file `Wortlaut.exe` and next to it `Wortlaut.settings.json`, if you keep Wortlaut somewhere else,
3. the “Wortlaut” shortcuts on the desktop and in the start menu, if present,
4. the folder `%APPDATA%\Wortlaut`, if present (the settings are stored there when the folder of Wortlaut.exe was read-only).

Your recordings and the created texts are not touched.

# licenses | Licenses and thanks

© 2026 A. Schelawski. Wortlaut is free software under the **GNU General Public License, version 3**. The source code is public: `github.com/Schelawski/Wortlaut`.

## Free of charge – only from the official source

**Wortlaut is free of charge and will stay so.** The only official source is the project page `github.com/Schelawski/Wortlaut`. If you paid for Wortlaut, you paid for something you can get there for free. Only download Wortlaut from there: copies from other sources may have been changed.

Wortlaut builds on the work of others. Many thanks to:

- **Faster-Whisper-XXL** by Purfview – the speech recognition program that Wortlaut controls (MIT license).
- **faster-whisper** by SYSTRAN – the fast implementation of Whisper that Faster-Whisper-XXL is based on (MIT license).
- **Whisper** by OpenAI – the speech recognition and its models (MIT license).
- **FFmpeg** – reads video and audio files, comes with Faster-Whisper-XXL (GPL, version 3).
- **SharpCompress** – extracts Faster-Whisper-XXL during the setup (MIT license).

The models are downloaded from Hugging Face, the speech recognition program from GitHub.
