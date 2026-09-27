namespace Wortlaut.UI;

/// <summary>
/// Read-only, monospaced log that scrolls to the newest line.
/// </summary>
internal sealed class LogBox : TextBox
{
    // Keeps the control responsive during long folder runs.
    private const int MaxChars = 1_000_000;

    public LogBox()
    {
        Multiline = true;
        ReadOnly = true;
        WordWrap = true;
        ScrollBars = ScrollBars.Vertical;
        MaxLength = int.MaxValue; // the default of 32767 would also cap AppendText
        Font = UiStyle.CreateMonospaceFont();
        BackColor = Color.FromArgb(243, 244, 246);
        Dock = DockStyle.Fill;
    }

    /// <summary>Appends a raw line (e.g. faster-whisper output).</summary>
    public void AppendLine(string line)
    {
        if (TextLength > MaxChars)
        {
            var text = Text;
            var cut = text.IndexOf('\n', text.Length - MaxChars / 2);
            Text = cut < 0 ? string.Empty : text[(cut + 1)..];
        }

        // AppendText moves the caret to the end and scrolls it into view.
        AppendText(line + Environment.NewLine);
    }

    /// <summary>Appends a Wortlaut message with a time stamp.</summary>
    public void AppendMessage(string message) => AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
}
