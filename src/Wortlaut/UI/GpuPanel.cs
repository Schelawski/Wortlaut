using Wortlaut.Core.Gpu;
using Wortlaut.Core.Models;

namespace Wortlaut.UI;

/// <summary>
/// Checks the graphics card (<c>--checkcuda</c>, <c>nvidia-smi</c>) and explains the result and the suggested
/// device and model in plain words. Used by the "Grafikkarte" dialog and by the welcome wizard.
/// </summary>
internal sealed class GpuPanel : UserControl
{
    private readonly string _exePath;
    private readonly IGpuProbe _probe;
    private readonly Func<(string Device, string Model)> _currentSettings;
    private readonly int _textWidth;
    private readonly bool _mentionMissingModel;

    private readonly StatusBadge _badge = new() { Anchor = AnchorStyles.Left, Margin = new Padding(3, 3, 8, 3) };
    private readonly Label _headlineLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 3, 3) };
    private readonly Label _gpuLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 4, 3, 3) };
    private readonly Label _explanationLabel = new() { AutoSize = true, Margin = new Padding(3, 10, 3, 3) };
    private readonly Label _currentLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 10, 3, 3) };
    private readonly Label _messageLabel = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3), Visible = false };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill, Height = 12, Style = ProgressBarStyle.Marquee, Margin = new Padding(3, 10, 3, 0) };

    private CancellationTokenSource? _cancellation;

    /// <param name="exePath">faster-whisper-xxl.exe, which performs the actual check.</param>
    /// <param name="currentSettings">Reads the device and model currently set.</param>
    /// <param name="textWidth">Width at which the texts wrap (logical pixels).</param>
    /// <param name="introText">Text above the result, or <c>null</c> for none.</param>
    /// <param name="mentionMissingModel">
    /// Say when the suggested model is not downloaded yet (not in the wizard, whose next page downloads it).
    /// </param>
    /// <param name="probe">Replaceable for tests; defaults to running the real programs.</param>
    public GpuPanel(
        string exePath,
        Func<(string Device, string Model)> currentSettings,
        int textWidth,
        string? introText,
        bool mentionMissingModel = true,
        IGpuProbe? probe = null)
    {
        _exePath = exePath;
        _mentionMissingModel = mentionMissingModel;
        _currentSettings = currentSettings;
        _textWidth = textWidth;
        _probe = probe ?? new GpuProbe();

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BuildLayout(introText);
    }

    /// <summary>Raised when a check has finished.</summary>
    public event EventHandler? CheckFinished;

    /// <summary>The suggestion of the last check, or <c>null</c> while checking.</summary>
    public GpuRecommendation? Recommendation { get; private set; }

    public bool IsChecking => _cancellation is not null;

    /// <summary>True when a suggestion exists and is already set.</summary>
    public bool SuggestionIsApplied
    {
        get
        {
            if (Recommendation is not { Device: { } device, Model: { } model })
                return false;

            var current = _currentSettings();
            return string.Equals(current.Device, device, StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.Model, model, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool HasSuggestion => Recommendation is { Device: not null, Model: not null };

    private void BuildLayout(string? introText)
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7,
            Margin = new Padding(0),
            MinimumSize = new Size(_textWidth + 6, 0),
            MaximumSize = new Size(_textWidth + 6, 0),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (introText is not null)
            layout.Controls.Add(new Label { Text = introText, AutoSize = true, MaximumSize = new Size(_textWidth, 0), Margin = new Padding(3, 0, 3, 12) }, 0, 0);

        _headlineLabel.Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold);
        _headlineLabel.MaximumSize = new Size(_textWidth - 120, 0);
        var headline = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        headline.Controls.Add(_badge);
        headline.Controls.Add(_headlineLabel);
        layout.Controls.Add(headline, 0, 1);

        foreach (var label in new[] { _gpuLabel, _explanationLabel, _currentLabel, _messageLabel })
            label.MaximumSize = new Size(_textWidth, 0);
        layout.Controls.Add(_gpuLabel, 0, 2);
        layout.Controls.Add(_explanationLabel, 0, 3);
        layout.Controls.Add(_currentLabel, 0, 4);
        layout.Controls.Add(_messageLabel, 0, 5);
        layout.Controls.Add(_progressBar, 0, 6);

        Controls.Add(layout);
    }

    /// <summary>Runs the check and shows the result. Does nothing while a check is running.</summary>
    public async Task CheckAsync()
    {
        if (_cancellation is not null)
            return;

        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        Recommendation = null;
        _badge.SetState("…", UiStyle.Neutral);
        _headlineLabel.Text = UiText.GpuChecking;
        _gpuLabel.Text = _explanationLabel.Text = _currentLabel.Text = string.Empty;
        _messageLabel.Visible = false;
        _progressBar.Visible = true;

        try
        {
            var result = await _probe.CheckAsync(_exePath, cancellation.Token);
            ShowResult(result);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return; // the window is closing
        }
        finally
        {
            _cancellation = null;
        }

        if (IsDisposed)
            return;
        _progressBar.Visible = false;
        CheckFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Stops a running check; the helper processes are killed.</summary>
    public void CancelCheck() => _cancellation?.Cancel();

    private void ShowResult(GpuCheckResult result)
    {
        var recommendation = GpuAdvisor.Recommend(result);
        Recommendation = recommendation;

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
        if (_mentionMissingModel
            && recommendation.Model is { } model
            && WhisperModels.Find(model) is not null
            && !WhisperModels.IsInstalled(WhisperModels.ModelsDirectory(_exePath), model))
        {
            _explanationLabel.Text += Environment.NewLine + Environment.NewLine + UiText.GpuModelNotDownloaded(model);
        }

        RefreshCurrentSettings();
    }

    /// <summary>Updates "Aktuell eingestellt", e.g. after the suggestion was applied.</summary>
    public void RefreshCurrentSettings()
    {
        var (device, model) = _currentSettings();
        _currentLabel.Text = UiText.GpuCurrentSettings(device, model);
    }

    public void ShowMessage(string text, Color color)
    {
        _messageLabel.Text = text;
        _messageLabel.ForeColor = color;
        _messageLabel.Visible = true;
    }

    public void HideMessage() => _messageLabel.Visible = false;

    public bool IsMessageVisible => _messageLabel.Visible;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancellation?.Cancel();
        base.Dispose(disposing);
    }
}
