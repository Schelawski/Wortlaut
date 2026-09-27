namespace Wortlaut.UI;

/// <summary>
/// Small rounded label, e.g. "gefunden" / "nicht gefunden" next to the executable path.
/// </summary>
internal sealed class StatusBadge : Control
{
    private UiStyle.Badge _badge = UiStyle.Neutral;

    public StatusBadge()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.Transparent;
        TabStop = false;
        AutoSize = true;
    }

    public void SetState(string text, UiStyle.Badge badge)
    {
        _badge = badge;
        Text = text;
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font);
        return new Size(text.Width + LogicalToDeviceUnits(14), text.Height + LogicalToDeviceUnits(6));
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (AutoSize)
            Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (AutoSize)
            Size = GetPreferredSize(Size.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        UiStyle.DrawBadge(e.Graphics, bounds, Text, Font, _badge);
    }
}
