using Wortlaut.Core.Gpu;

namespace Wortlaut.UI;

/// <summary>
/// "Grafikkarte": checks the graphics card and applies the suggested device and model on request.
/// </summary>
internal sealed class GpuDialog : Form
{
    private const int TextWidth = 560;

    private readonly GpuPanel _panel;
    private readonly Action<string, string> _applySettings;
    private readonly Button _applyButton = UiStyle.CreateButton(UiText.GpuApply);
    private readonly Button _recheckButton = UiStyle.CreateButton(UiText.GpuRecheck);
    private readonly Button _closeButton = UiStyle.CreateButton(UiText.Close);

    /// <param name="exePath">faster-whisper-xxl.exe, which performs the actual check.</param>
    /// <param name="currentSettings">Reads the device and model currently set in the main window.</param>
    /// <param name="applySettings">Sets device and model in the main window.</param>
    /// <param name="probe">Replaceable for tests; defaults to running the real programs.</param>
    public GpuDialog(
        string exePath,
        Func<(string Device, string Model)> currentSettings,
        Action<string, string> applySettings,
        IGpuProbe? probe = null)
    {
        _applySettings = applySettings;
        _panel = new GpuPanel(exePath, currentSettings, TextWidth, UiText.GpuIntro, probe: probe);

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.GpuTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BuildLayout();
        ResumeLayout(false);
        PerformLayout();

        UiStyle.MakePrimary(_applyButton);
        _applyButton.Click += (_, _) => Apply();
        _recheckButton.Click += async (_, _) => await CheckAsync();
        _closeButton.Click += (_, _) => Close();
        _panel.CheckFinished += (_, _) => UpdateApplyState();
        AcceptButton = _applyButton;
        CancelButton = _closeButton;
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_panel, 0, 0);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_recheckButton);
        buttons.Controls.Add(_applyButton);
        layout.Controls.Add(buttons, 0, 1);

        Controls.Add(layout);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await CheckAsync();
    }

    private async Task CheckAsync()
    {
        _applyButton.Enabled = _recheckButton.Enabled = false;
        await _panel.CheckAsync();
        if (!IsDisposed)
            _recheckButton.Enabled = true;
    }

    private void UpdateApplyState()
    {
        var applied = _panel.SuggestionIsApplied;
        _applyButton.Enabled = _panel.HasSuggestion && !applied;
        if (_panel.HasSuggestion && applied && !_panel.IsMessageVisible)
            _panel.ShowMessage(UiText.GpuAlreadyApplied, UiStyle.Success.Fore);
    }

    private void Apply()
    {
        if (_panel.Recommendation is not { Device: { } device, Model: { } model })
            return;

        _applySettings(device, model);
        _panel.RefreshCurrentSettings();
        _panel.ShowMessage(UiText.GpuApplied, UiStyle.Success.Fore);
        _applyButton.Enabled = false;
        AcceptButton = _closeButton;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _panel.CancelCheck();
        base.OnFormClosing(e);
    }
}
