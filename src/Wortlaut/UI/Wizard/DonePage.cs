using System.Diagnostics;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>
/// "Wortlaut ist bereit": how to start, and optionally copy Wortlaut out of the downloads folder with shortcuts.
/// </summary>
internal sealed class DonePage : WizardPage
{
    private readonly CheckBox? _copyBox;
    private readonly Label _messageLabel = new() { AutoSize = true, MaximumSize = new Size(TextWidth, 0), ForeColor = UiStyle.Danger.Fore, Margin = new Padding(3, 10, 3, 3), Visible = false };

    public DonePage(SetupWizard wizard)
        : base(wizard)
    {
        var stack = Stack();

        var headline = Paragraph(UiText.DoneHeadline);
        headline.Font = new Font(headline.Font.FontFamily, 11f, FontStyle.Bold);
        headline.ForeColor = UiStyle.Success.Fore;
        Add(stack, headline);
        Add(stack, Paragraph(UiText.DoneText));
        if (!wizard.ModelInstalled)
            Add(stack, Paragraph(UiText.DoneModelMissing(wizard.Settings.Model.Trim())));

        var processPath = Environment.ProcessPath;
        if (LocalInstall.CanOffer(processPath, wizard.Root))
        {
            _copyBox = new CheckBox
            {
                Text = UiText.DoneCopyOption,
                AutoSize = true,
                MaximumSize = new Size(TextWidth, 0),
                Checked = LocalInstall.IsTemporaryLocation(processPath!, LocalInstall.DefaultTemporaryFolders()),
                Margin = new Padding(3, 8, 3, 0),
            };
            Add(stack, _copyBox);
            var hint = Paragraph(UiText.DoneCopyHint(LocalInstall.TargetPath(wizard.Root)), new Padding(22, 2, 3, 3));
            hint.ForeColor = UiStyle.MutedText;
            Add(stack, hint);
        }

        Add(stack, _messageLabel);
        Controls.Add(stack);
    }

    public override WizardStep Step => WizardStep.Done;

    public override string Title => UiText.DoneTitle;

    public override string PrimaryText => UiText.DoneStart;

    public override Task OnPrimaryAsync()
    {
        if (_copyBox is not { Checked: true } || Environment.ProcessPath is not { } processPath)
        {
            Wizard.Finish(WizardOutcome.Finished);
            return Task.CompletedTask;
        }

        try
        {
            // Saved first, so the copy starts with everything the wizard set up.
            Wizard.Settings.WizardResumeStep = null;
            Wizard.SaveSettings();
            var copy = LocalInstall.Copy(processPath, Wizard.Root, Wizard.SettingsFile);
            LocalInstall.CreateShortcuts(copy);
            Process.Start(new ProcessStartInfo(copy) { UseShellExecute = true, WorkingDirectory = Wizard.Root });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            _messageLabel.Text = UiText.DoneCopyFailed(ex.Message);
            _messageLabel.Visible = true;
            _copyBox.Checked = false; // a second click opens Wortlaut from here
            return Task.CompletedTask;
        }

        Wizard.Finish(WizardOutcome.StartedCopy);
        return Task.CompletedTask;
    }
}
