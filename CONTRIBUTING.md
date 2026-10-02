# Contributing to Wortlaut

Thank you for your interest in Wortlaut! Bug reports, ideas, translations and code are welcome.
Wortlaut is a small project maintained in spare time, so please read these few rules first.

## Reporting a problem or an idea

Open an [issue](https://github.com/Schelawski/Wortlaut/issues). Helpful for problems:

- what you did, what you expected and what happened instead,
- the Wortlaut version (window title) and your Windows version,
- the lines from the log in the Wortlaut window, if there are any,
- for graphics card problems: the result of **Tools → Check graphics card…**.

Please never attach recordings or transcripts that contain private conversations.

## Before you write code

**Please open an issue first and wait for a short answer** before starting larger changes (new features,
new dependencies, changes to the user interface or the folder layout). This avoids work that cannot be
merged. Small fixes (typos, obvious bugs) can go straight to a pull request.

## Pull requests

1. Fork the repository and create a branch from `develop` (not `master`).
2. Keep a pull request focused on one topic.
3. Build and test locally (see [Build](README.md#build)):
   ```powershell
   dotnet build
   dotnet test
   ```
   The automatic build on GitHub runs the same tests; it starts after a maintainer has approved it for
   first-time contributors.
4. Describe what you changed and why, and link the issue (`Closes #123`).

The maintainer reviews every pull request and decides whether and when it is merged. Nothing is merged
automatically.

## Guidelines

- **Code, comments and documentation in English.** Match the style of the surrounding code.
- **The user interface speaks German, English and Russian.** Every user-visible text lives in
  `src/Wortlaut/UI/UiText.cs` and is written as `L("German", "Russian", "English")`. A text in only one
  language does not compile. If you cannot translate into one of the languages, say so in the pull
  request – it will be completed before merging.
- **Plain language.** Wortlaut is made for people without technical knowledge. Prefer “graphics card”
  over “CUDA device”, and explain what the user can do.
- **Help texts** are in `src/Wortlaut/Help/help.{de,en,ru}.md`. All three files keep the same topics in
  the same order, and button names must match the user interface (the tests check this).
- **Privacy first.** Recordings and transcripts never leave the computer. Network access is limited to
  downloading Faster-Whisper-XXL and the models, and to the optional update check.
- **Never touch the user's media files.** They are only read – never renamed, moved or deleted.
- Add or update unit tests in `tests/Wortlaut.Tests` for changes in `src/Wortlaut/Core`.

## License of contributions

Wortlaut is licensed under the [GNU General Public License v3.0](LICENSE). By submitting a pull request
you agree that your contribution is published under the same license. You keep the copyright of your
contribution.
