using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>
/// One page of the welcome wizard. The wizard owns the header and the buttons; the page describes what they do.
/// </summary>
internal abstract class WizardPage : UserControl
{
    /// <summary>Width at which page texts wrap (logical pixels).</summary>
    protected const int TextWidth = 600;

    protected WizardPage(SetupWizard wizard)
    {
        Wizard = wizard;
        AutoScaleMode = AutoScaleMode.Inherit;
        Dock = DockStyle.Fill;
    }

    protected SetupWizard Wizard { get; }

    public abstract WizardStep Step { get; }

    public abstract string Title { get; }

    /// <summary>Text of the main (blue) button.</summary>
    public virtual string PrimaryText => UiText.WizardNext;

    public virtual bool PrimaryEnabled => !IsBusy;

    /// <summary>Text of the optional second button, <c>null</c> to hide it.</summary>
    public virtual string? SecondaryText => null;

    public virtual bool SecondaryEnabled => !IsBusy;

    /// <summary>A download or check is running: going back is not possible, closing asks first.</summary>
    public virtual bool IsBusy => false;

    /// <summary>True when closing during <see cref="IsBusy"/> should ask for confirmation (downloads).</summary>
    public virtual bool ConfirmCancelWhileBusy => true;

    /// <summary>Called when the page becomes visible.</summary>
    public virtual Task OnShownAsync() => Task.CompletedTask;

    public abstract Task OnPrimaryAsync();

    public virtual Task OnSecondaryAsync() => Task.CompletedTask;

    /// <summary>Stops a running operation; the page calls <see cref="NotifyChanged"/> when it has stopped.</summary>
    public virtual void RequestStop()
    {
    }

    /// <summary>Lets the wizard update its buttons (and close, if it waits for a stop).</summary>
    protected void NotifyChanged() => Wizard.OnPageChanged(this);

    /// <summary>A label that wraps at the page width.</summary>
    protected static Label Paragraph(string text, Padding? margin = null) => new()
    {
        Text = text,
        AutoSize = true,
        UseMnemonic = false,
        MaximumSize = new Size(TextWidth, 0),
        Margin = margin ?? new Padding(3, 0, 3, 12),
    };

    /// <summary>A vertical stack of auto-sized rows, filling the page.</summary>
    protected static TableLayoutPanel Stack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = new Padding(0),
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }

    /// <summary>Appends a row to a <see cref="Stack"/>.</summary>
    protected static void Add(TableLayoutPanel stack, Control control)
    {
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(control, 0, stack.RowCount++);
    }

    /// <summary>"Caption: value" rows, e.g. the download details.</summary>
    protected static TableLayoutPanel DetailsTable() => new()
    {
        AutoSize = true,
        ColumnCount = 2,
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 4),
        ColumnStyles = { new ColumnStyle(SizeType.AutoSize), new ColumnStyle(SizeType.Percent, 100) },
    };

    protected static void AddDetail(TableLayoutPanel details, string caption, Control value)
    {
        var row = details.RowCount++;
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.Controls.Add(new Label { Text = caption + ":", AutoSize = true, ForeColor = UiStyle.MutedText, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 12, 4) }, 0, row);
        details.Controls.Add(value, 1, row);
    }

    protected static Label ValueLabel(string text = "") =>
        new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, UseMnemonic = false, Margin = new Padding(3, 4, 3, 4), MaximumSize = new Size(TextWidth - 140, 0) };

    /// <summary>Progress bar, status line and message as used by the download pages.</summary>
    protected sealed class ProgressArea
    {
        public ProgressBar Bar { get; } = new() { Dock = DockStyle.Top, Maximum = 1000, Height = 12, MarqueeAnimationSpeed = 30, Margin = new Padding(3, 12, 3, 0) };

        public Label Status { get; } = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 6, 3, 3), MaximumSize = new Size(TextWidth, 0) };

        public Label Message { get; } = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3), MaximumSize = new Size(TextWidth, 0), Visible = false };

        public void AddTo(TableLayoutPanel stack)
        {
            Add(stack, Bar);
            Add(stack, Status);
            Add(stack, Message);
        }

        /// <summary>Shows a fraction, or a running marquee if it is unknown.</summary>
        public void SetFraction(double? fraction)
        {
            if (fraction is { } value)
            {
                Bar.Style = ProgressBarStyle.Continuous;
                UiStyle.SetProgressImmediately(Bar, (int)Math.Round(value * Bar.Maximum));
            }
            else
            {
                Bar.Style = ProgressBarStyle.Marquee;
            }
        }

        public void Reset(string status)
        {
            Bar.Style = ProgressBarStyle.Continuous;
            UiStyle.SetProgressImmediately(Bar, 0);
            Status.Text = status;
        }

        public void ShowMessage(string text, Color color)
        {
            Message.Text = text;
            Message.ForeColor = color;
            Message.Visible = true;
        }
    }
}
