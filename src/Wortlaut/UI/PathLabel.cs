namespace Wortlaut.UI;

/// <summary>
/// Single-line label for file paths. Shortens the middle of a long path ("C:\Videos\…\Лекция 12.txt"),
/// so the file name stays visible, and keeps the text vertically centered.
/// </summary>
internal sealed class PathLabel : Label
{
    public PathLabel()
    {
        AutoSize = false;
        UseMnemonic = false;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public override Size GetPreferredSize(Size proposedSize) =>
        new(proposedSize.Width, TextRenderer.MeasureText("Wg", Font).Height);

    protected override void OnPaint(PaintEventArgs e)
    {
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            Enabled ? ForeColor : SystemColors.GrayText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            | TextFormatFlags.PathEllipsis | TextFormatFlags.NoPrefix);
    }
}
