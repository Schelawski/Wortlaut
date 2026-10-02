using Wortlaut.Core.Gpu;
using Wortlaut.Core.Models;

namespace Wortlaut.UI;

/// <summary>
/// Checks the graphics card (<c>--checkcuda</c>, <c>nvidia-smi</c>), explains the result in plain words and
/// applies the suggested device and model on request.
/// </summary>
internal sealed class GpuDialog : Form
{
    private const int TextWidth = 560;

    private readonly string _exePath;
    private readonly IGpuProbe _probe;
    private readonly Func<(string Device, string Model)> _currentSettings;
    private readonly Action<string, string> _applySettings;

    private readonly StatusBadge _badge = new() { Anchor = AnchorStyles.Left, Margin = new Padding(3, 3, 8, 3) };
    private readonly Label _headlineLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 3, 3) };
    private readonly Label _gpuLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 4, 3, 3) };
    private readonly Label _explanationLabel = new() { AutoSize = true, Margin = new Padding(3, 10, 3, 3) };
    private readonly Label _currentLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 10, 3, 3) };
    private readonly Label _messageLabel = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3), Visible = false };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill, Height = 12, Style = ProgressBarStyle.Marquee, Margin = new Padding(3, 10, 3, 0) };
    private readonly Button _applyButton = UiStyle.CreateButton(UiText.GpuApply);
    private readonly Button _recheckButton = UiStyle.CreateButton(UiText.GpuRecheck);
    private readonly Button _closeButton = UiStyle.CreateButton(UiText.Close);

    private GpuRecommendation? _recommendation;
    private CancellationTokenSource? _cancellation;

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
        _exePath = exePath;
        _currentSettings = currentSettings;
        _applySettings = applySettings;
        _probe = probe ?? new GpuProbe();

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
            RowCount = 8,
            Padding = new Padding(14),
            MinimumSize = new Size(TextWidth + 50, 0),
            MaximumSize = new Size(TextWidth + 50, 0),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 8; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = UiText.GpuIntro, AutoSize = true, MaximumSize = new Size(TextWidth, 0), Margin = new Padding(3, 0, 3, 12) }, 0, 0);

        _headlineLabel.Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold);
        _headlineLabel.MaximumSize = new Size(TextWidth - 120, 0);
        var headline = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        headline.Controls.Add(_badge);
        headline.Controls.Add(_headlineLabel);
        layout.Controls.Add(headline, 0, 1);

        foreach (var label in new[] { _gpuLabel, _explanationLabel, _currentLabel, _messageLabel })
            label.MaximumSize = new Size(TextWidth, 0);
        layout.Controls.Add(_gpuLabel, 0, 2);
        layout.Controls.Add(_explanationLabel, 0, 3);
        layout.Controls.Add(_currentLabel, 0, 4);
        layout.Controls.Add(_messageLabel, 0, 5);
        layout.Controls.Add(_progressBar, 0, 6);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_recheckButton);
        buttons.Controls.Add(_applyButton);
        layout.Controls.Add(buttons, 0, 7);

        Controls.Add(layout);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await CheckAsync();
    }

    private async Task CheckAsync()
    {
        if (_cancellation is not null)
            return;

        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        _recommendation = null;
        _badge.SetState("…", UiStyle.Neutral);
        _headlineLabel.Text = UiText.GpuChecking;
        _gpuLabel.Text = _explanationLabel.Text = _currentLabel.Text = string.Empty;
        _messageLabel.Visible = false;
        _progressBar.Visible = true;
        _applyButton.Enabled = _recheckButton.Enabled = false;

        try
        {
            var result = await _probe.CheckAsync(_exePath, cancellation.Token);
            ShowResult(result);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return; // the dialog is closing
        }
        finally
        {
            _cancellation = null;
        }

        if (IsDisposed)
            return;
        _progressBar.Visible = false;
        _recheckButton.Enabled = true;
        UpdateApplyState();
    }

    private void ShowResult(GpuCheckResult result)
    {
        var recommendation = GpuAdvisor.Recommend(result);
        _recommendation = recommendation;

        _badge.SetState(UiText.GpuBadge(recommendation.Verdict), recommendation.Verdict switch
        {
            GpuVerdict.Cuda => UiStyle.Success,
            GpuVerdict.CudaLowMemory or GpuVerdict.Cpu => UiStyle.Warning,
            _ => UiStyle.Danger,
        });
        _headlineLabel.Text = UiText.GpuHeadline(recommendation.Verdict);

        // Name and memory are only interesting when a card can be used; nvidia-smi is optional.
        _gpuLabel.Text = recommendation.Verdict is GpuVerdict.Cuda or GpuVerdict.CudaLowMemory
            ? result.Gpus.Count > 0
                ? string.Join(Environment.NewLine, result.Gpus.Select(UiText.GpuDescription))
                : UiText.GpuNoNvidiaSmi
            : string.Empty;
        _gpuLabel.Visible = _gpuLabel.Text.Length > 0;

        _explanationLabel.Text = UiText.GpuExplanation(recommendation);
        if (recommendation.Model is { } model
            && WhisperModels.Find(model) is not null
            && !WhisperModels.IsInstalled(WhisperModels.ModelsDirectory(_exePath), model))
        {
            _explanationLabel.Text += Environment.NewLine + Environment.NewLine + UiText.GpuModelNotDownloaded(model);
        }

        var (device, currentModel) = _currentSettings();
        _currentLabel.Text = UiText.GpuCurrentSettings(device, currentModel);
    }

    /// <summary>True when the main window already uses the suggestion.</summary>
    private bool SuggestionIsApplied()
    {
        if (_recommendation is not { Device: { } device, Model: { } model })
            return false;

        var current = _currentSettings();
        return string.Equals(current.Device, device, StringComparison.OrdinalIgnoreCase)
            && string.Equals(current.Model, model, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateApplyState()
    {
        var hasSuggestion = _recommendation is { Device: not null, Model: not null };
        var applied = SuggestionIsApplied();
        _applyButton.Enabled = hasSuggestion && !applied;
        if (hasSuggestion && applied && !_messageLabel.Visible)
            ShowMessage(UiText.GpuAlreadyApplied, UiStyle.Success.Fore);
    }

    private void Apply()
    {
        if (_recommendation is not { Device: { } device, Model: { } model })
            return;

        _applySettings(device, model);
        var (currentDevice, currentModel) = _currentSettings();
        _currentLabel.Text = UiText.GpuCurrentSettings(currentDevice, currentModel);
        ShowMessage(UiText.GpuApplied, UiStyle.Success.Fore);
        _applyButton.Enabled = false;
        AcceptButton = _closeButton;
    }

    private void ShowMessage(string text, Color color)
    {
        _messageLabel.Text = text;
        _messageLabel.ForeColor = color;
        _messageLabel.Visible = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Stops a running check; the helper processes are killed.
        _cancellation?.Cancel();
        base.OnFormClosing(e);
    }
}
