using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>Checks the graphics card and applies the suggested device and model.</summary>
internal sealed class GraphicsPage : WizardPage
{
    private readonly GpuPanel _panel;

    public GraphicsPage(SetupWizard wizard)
        : base(wizard)
    {
        _panel = new GpuPanel(
            wizard.Settings.ExePath.Trim(),
            () => (wizard.Settings.Device, wizard.Settings.Model),
            TextWidth,
            UiText.GpuIntro,
            mentionMissingModel: false); // the next page downloads it
        _panel.CheckFinished += (_, _) => NotifyChanged();

        var stack = Stack();
        Add(stack, _panel);
        Controls.Add(stack);
    }

    public override WizardStep Step => WizardStep.Graphics;

    public override string Title => UiText.GraphicsTitle;

    /// <summary>"Übernehmen und weiter" while there is something to apply, otherwise "Weiter".</summary>
    public override string PrimaryText =>
        _panel.HasSuggestion && !_panel.SuggestionIsApplied ? UiText.GpuApplyAndNext : UiText.WizardNext;

    public override string? SecondaryText => UiText.GpuRecheck;

    public override bool IsBusy => _panel.IsChecking;

    /// <summary>The check takes seconds and only reads; closing needs no confirmation.</summary>
    public override bool ConfirmCancelWhileBusy => false;

    public override Task OnShownAsync() => CheckAsync();

    public override Task OnPrimaryAsync()
    {
        if (_panel.Recommendation is { Device: { } device, Model: { } model })
        {
            Wizard.Settings.Device = device;
            Wizard.Settings.Model = model;
            Wizard.SaveSettings();
        }

        Wizard.GoNext();
        return Task.CompletedTask;
    }

    public override Task OnSecondaryAsync() => CheckAsync();

    public override void RequestStop() => _panel.CancelCheck();

    private async Task CheckAsync()
    {
        var check = _panel.CheckAsync();
        NotifyChanged(); // "checking": buttons disabled
        await check;
        NotifyChanged();
    }
}
