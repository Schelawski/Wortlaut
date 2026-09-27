using System.Drawing.Drawing2D;

namespace Wortlaut.UI;

/// <summary>
/// Colors, fonts and small drawing helpers shared by the views.
/// </summary>
internal static class UiStyle
{
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentDisabled = Color.FromArgb(191, 207, 240);
    public static readonly Color MutedText = Color.FromArgb(75, 85, 99);
    public static readonly Color GridLine = Color.FromArgb(229, 231, 235);

    /// <summary>Background/text colors of a status badge.</summary>
    public readonly record struct Badge(Color Back, Color Fore);

    public static readonly Badge Neutral = new(Color.FromArgb(229, 231, 235), Color.FromArgb(55, 65, 81));
    public static readonly Badge Info = new(Color.FromArgb(219, 234, 254), Color.FromArgb(30, 64, 175));
    public static readonly Badge Success = new(Color.FromArgb(220, 252, 231), Color.FromArgb(22, 101, 52));
    public static readonly Badge Warning = new(Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64, 14));
    public static readonly Badge Danger = new(Color.FromArgb(254, 226, 226), Color.FromArgb(153, 27, 27));

    public static Font CreateMonospaceFont(float size = 9f, FontStyle style = FontStyle.Regular) =>
        new("Consolas", size, style);

    /// <summary>Filled blue button for the main action.</summary>
    public static void MakePrimary(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.ForeColor = Color.White;
        button.Font = new Font(button.Font, FontStyle.Bold);
        button.UseVisualStyleBackColor = false;

        void Apply() => button.BackColor = button.Enabled ? Accent : AccentDisabled;
        button.EnabledChanged += (_, _) => Apply();
        Apply();
    }

    /// <summary>Common settings for buttons: sized to their text with some breathing room.</summary>
    public static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 3, 10, 3),
            MinimumSize = new Size(0, 30),
            UseVisualStyleBackColor = true,
        };
    }

    public static Label CreateCaption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        UseMnemonic = false, // show "&" literally ("Drag & Drop")
        ForeColor = MutedText,
        Margin = new Padding(3, 6, 3, 0),
    };

    public static void FillRoundedRectangle(Graphics graphics, Color color, RectangleF bounds, float radius)
    {
        using var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        using var brush = new SolidBrush(color);
        var previous = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(brush, path);
        graphics.SmoothingMode = previous;
    }

    /// <summary>
    /// Sets a progress bar value without the slow fill animation of visual styles. faster-whisper reports
    /// progress in jumps (one 30-second window at a time), and the animation would lag behind the percentage.
    /// </summary>
    public static void SetProgressImmediately(ProgressBar bar, int value)
    {
        value = Math.Clamp(value, bar.Minimum, bar.Maximum);
        // The animation only runs when the value grows, so step past the target and back.
        if (value < bar.Maximum)
        {
            bar.Value = value + 1;
            bar.Value = value;
        }
        else
        {
            bar.Value = value;
            bar.Value = value - 1;
            bar.Value = value;
        }
    }

    /// <summary>Draws a rounded badge with centered text inside <paramref name="bounds"/>.</summary>
    public static void DrawBadge(Graphics graphics, Rectangle bounds, string text, Font font, Badge badge)
    {
        FillRoundedRectangle(graphics, badge.Back, bounds, bounds.Height / 2f);
        TextRenderer.DrawText(
            graphics,
            text,
            font,
            bounds,
            badge.Fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
}
