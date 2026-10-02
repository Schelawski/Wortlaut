using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>Greeting, choice of the UI language and the privacy promise.</summary>
internal sealed class WelcomePage : WizardPage
{
    public WelcomePage(SetupWizard wizard)
        : base(wizard)
    {
        var stack = Stack();
        Add(stack, Paragraph(UiText.WelcomeText));
        Add(stack, BuildPrivacyNote());
        Add(stack, Paragraph(UiText.WelcomeSteps, new Padding(3, 4, 3, 16)));
        Add(stack, BuildLanguageChoice());
        Controls.Add(stack);
    }

    public override WizardStep Step => WizardStep.Welcome;

    public override string Title => UiText.WelcomeTitle;

    /// <summary>Skips the download for users who already have Faster-Whisper-XXL.</summary>
    public override string? SecondaryText => Wizard.ExeFound ? null : UiText.WizardHaveExe;

    public override Task OnPrimaryAsync()
    {
        Wizard.GoNext();
        return Task.CompletedTask;
    }

    public override Task OnSecondaryAsync()
    {
        Wizard.ChooseExistingExe();
        return Task.CompletedTask;
    }

    /// <summary>The most important promise, set apart in green.</summary>
    private static Control BuildPrivacyNote()
    {
        var label = new Label
        {
            Text = UiText.WelcomePrivacy,
            AutoSize = true,
            MaximumSize = new Size(TextWidth - 24, 0),
            ForeColor = UiStyle.Success.Fore,
            BackColor = UiStyle.Success.Back,
            Margin = new Padding(0),
        };
        label.Font = new Font(label.Font, FontStyle.Bold);

        var box = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = UiStyle.Success.Back,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(3, 0, 3, 14),
        };
        box.Controls.Add(label);
        return box;
    }

    private Control BuildLanguageChoice()
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        row.Controls.Add(new Label { Text = UiText.WelcomeLanguageLabel, AutoSize = true, Margin = new Padding(3, 6, 12, 3) });

        foreach (var language in UiLanguages.All)
        {
            var option = new RadioButton
            {
                Text = UiLanguages.NativeName(language),
                AutoSize = true,
                Checked = language == UiText.Language,
                Margin = new Padding(3, 3, 12, 3),
            };
            option.CheckedChanged += (_, _) =>
            {
                if (option.Checked)
                    BeginInvoke(() => Wizard.SwitchLanguage(language)); // the page is replaced, so not inside the event
            };
            row.Controls.Add(option);
        }

        return row;
    }
}
