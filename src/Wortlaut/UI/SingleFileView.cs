using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>
/// Tab "Einzelne Datei": transcribe one file from any folder.
/// </summary>
internal sealed class SingleFileView : UserControl, IRunView
{
    private readonly IViewHost _host;
    private readonly ToolTip _toolTip = new();

    private readonly TextBox _fileBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _chooseButton = UiStyle.CreateButton(UiText.ChooseFile);
    private readonly PathLabel _resultPathLabel = new()
    {
        Dock = DockStyle.Fill,
        Font = UiStyle.CreateMonospaceFont(9f, FontStyle.Bold),
    };
    private readonly Label _resultHintLabel = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        ForeColor = UiStyle.MutedText,
    };
    private readonly CheckBox _overwriteBox = new() { Text = UiText.OverwriteExisting, AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
    private readonly Button _transcribeButton = UiStyle.CreateButton(UiText.Transcribe);
    private readonly Button _cancelButton = UiStyle.CreateButton(UiText.Cancel);
    private readonly ProgressBar _progressBar = new()
    {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Height = 10,
        Maximum = 1000,
        MarqueeAnimationSpeed = 30,
    };
    private readonly Label _stateLabel = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        Font = UiStyle.CreateMonospaceFont(),
        ForeColor = UiStyle.MutedText,
        Text = UiText.StateReady,
    };
    private readonly LogBox _log = new();

    public SingleFileView(IViewHost host)
    {
        _host = host;
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        BuildLayout();

        UiStyle.MakePrimary(_transcribeButton);
        _cancelButton.Enabled = false;

        _fileBox.Text = _host.Settings.LastFile;
        _fileBox.TextChanged += (_, _) => OnFileChanged();
        _chooseButton.Click += (_, _) => ChooseFile();
        _overwriteBox.CheckedChanged += (_, _) => UpdateResultPreview();
        _transcribeButton.Click += async (_, _) => await TranscribeAsync();
        _cancelButton.Click += (_, _) => Cancel();

        _log.AppendMessage(UiText.LogReady);
        UpdateResultPreview();
    }

    /// <summary>Sets the file, e.g. after drag and drop onto the window.</summary>
    public void SetMediaFile(string path) => _fileBox.Text = path;

    public void SetRunState(RunState state)
    {
        var idle = state == RunState.Idle;
        _fileBox.ReadOnly = !idle;
        _chooseButton.Enabled = idle;
        _overwriteBox.Enabled = idle;
        _transcribeButton.Enabled = idle;
        _cancelButton.Enabled = state == RunState.RunningHere;
    }

    public void OnWhisperSettingsChanged() => UpdateResultPreview();

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(UiStyle.CreateCaption(UiText.MediaFileLabel), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);

        layout.Controls.Add(_fileBox, 0, 1);
        layout.Controls.Add(_chooseButton, 1, 1);

        var resultRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 4, 0, 0) };
        resultRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        resultRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        resultRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        resultRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // All labels of this row fill the row and center their text, so the different fonts line up.
        resultRow.Controls.Add(new Label { Text = UiText.ResultCaption, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        resultRow.Controls.Add(_resultPathLabel, 1, 0);
        resultRow.Controls.Add(_resultHintLabel, 2, 0);
        layout.Controls.Add(resultRow, 0, 2);
        layout.SetColumnSpan(resultRow, 2);

        layout.Controls.Add(_overwriteBox, 0, 3);
        layout.SetColumnSpan(_overwriteBox, 2);

        var actionRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 0, 0, 6) };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionRow.Controls.Add(_transcribeButton, 0, 0);
        actionRow.Controls.Add(_cancelButton, 1, 0);
        actionRow.Controls.Add(_progressBar, 2, 0);
        actionRow.Controls.Add(_stateLabel, 3, 0);
        _progressBar.Margin = new Padding(12, 3, 12, 3);
        layout.Controls.Add(actionRow, 0, 4);
        layout.SetColumnSpan(actionRow, 2);

        layout.Controls.Add(_log, 0, 5);
        layout.SetColumnSpan(_log, 2);

        Controls.Add(layout);
    }

    private void OnFileChanged()
    {
        _host.Settings.LastFile = PathInput.Clean(_fileBox.Text);
        _host.SettingsChanged();
        UpdateResultPreview();
    }

    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = UiText.MediaDialogTitle,
            Filter = UiText.MediaDialogFilter,
            CheckFileExists = true,
        };

        var current = PathInput.Clean(_fileBox.Text);
        if (File.Exists(current))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(current);
            dialog.FileName = Path.GetFileName(current);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _fileBox.Text = dialog.FileName;
    }

    /// <summary>Shows where the transcript will be written and whether it already exists.</summary>
    private void UpdateResultPreview()
    {
        var target = TryGetTargetPath(PathInput.Clean(_fileBox.Text));
        _resultPathLabel.Text = target ?? string.Empty;
        _toolTip.SetToolTip(_resultPathLabel, target);

        _resultHintLabel.Text = target is not null && File.Exists(target)
            ? (_overwriteBox.Checked ? UiText.TargetExistsWillOverwrite : UiText.TargetExistsWillSkip)
            : string.Empty;
    }

    private string? TryGetTargetPath(string mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
            return null;

        try
        {
            return TranscriptionPaths.GetTargetPath(mediaPath, _host.Settings.Format);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null; // incomplete input while typing
        }
    }

    private async Task TranscribeAsync()
    {
        var settings = _host.ValidateWhisperSettings();
        if (settings is null)
            return;

        var mediaPath = PathInput.Clean(_fileBox.Text);
        if (!File.Exists(mediaPath))
        {
            _host.ShowWarning(UiText.MediaMissing);
            return;
        }

        if (!MediaFiles.IsSupported(mediaPath))
        {
            _host.ShowWarning(UiText.MediaNotSupported(Path.GetExtension(mediaPath)));
            return;
        }

        var overwrite = _overwriteBox.Checked;
        await _host.RunExclusiveAsync(this, cancellationToken => RunJobAsync(mediaPath, settings, overwrite, cancellationToken));
    }

    private async Task RunJobAsync(string mediaPath, WhisperSettings settings, bool overwrite, CancellationToken cancellationToken)
    {
        _log.AppendLine(string.Empty);
        _log.AppendMessage(UiText.LogStartFile(mediaPath));
        if (Core.Models.WhisperModels.Find(settings.Model) is null)
            _log.AppendMessage(UiText.LogUnknownModel(settings.Model));
        SetState(UiText.StateStarting);
        _progressBar.Style = ProgressBarStyle.Marquee;

        TimeSpan? duration = null;
        try
        {
            duration = await _host.DurationProbe.GetDurationAsync(mediaPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The job below reports the cancellation.
        }

        _log.AppendMessage(duration is { } known ? UiText.LogDuration(known) : UiText.LogDurationUnknown);
        if (duration is not null)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 0;
        }

        var progress = new Progress<JobUpdate>(OnJobUpdate);
        var result = await _host.Job.RunAsync(new TranscriptionRequest(mediaPath, settings, overwrite, duration), progress, cancellationToken);

        _log.AppendMessage(UiText.LogResult(result));
        if (result.CudaProblem != Core.Gpu.CudaProblem.None)
            _host.ReportCudaProblem(result, remainingNotProcessed: false);
        _progressBar.Style = ProgressBarStyle.Continuous;
        UiStyle.SetProgressImmediately(_progressBar, result.Outcome == JobOutcome.Completed ? _progressBar.Maximum : 0);
        SetState(result.Outcome switch
        {
            JobOutcome.Completed => UiText.StateDone,
            JobOutcome.Skipped => UiText.StateSkipped,
            JobOutcome.Cancelled => UiText.StateCancelled,
            _ => UiText.StateFailed,
        });
        UpdateResultPreview();
    }

    private void OnJobUpdate(JobUpdate update)
    {
        switch (update)
        {
            case JobOutputUpdate output when !string.IsNullOrWhiteSpace(output.Line):
                _log.AppendLine(output.Line);
                break;

            case JobMessageUpdate message:
                _log.AppendMessage(UiText.LogMessage(message));
                break;

            case JobProgressUpdate progress when progress.Fraction is { } fraction:
                _progressBar.Style = ProgressBarStyle.Continuous;
                UiStyle.SetProgressImmediately(_progressBar, (int)Math.Round(fraction * _progressBar.Maximum));
                SetState(UiText.StatePercent(fraction));
                break;

            case JobProgressUpdate progress:
                _progressBar.Style = ProgressBarStyle.Marquee;
                SetState(UiText.StatePosition(progress.Position));
                break;
        }
    }

    private void Cancel()
    {
        _cancelButton.Enabled = false;
        SetState(UiText.StateCancelling);
        _log.AppendMessage(UiText.LogCancelling);
        _host.CancelRun();
    }

    private void SetState(string text) => _stateLabel.Text = text;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
