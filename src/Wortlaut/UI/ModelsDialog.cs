using Wortlaut.Core.Models;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI;

/// <summary>
/// Overview of the Whisper models: size, status, a short hint, download (with progress, resumable) and delete.
/// </summary>
internal sealed class ModelsDialog : Form
{
    private readonly string _modelsDirectory;
    private readonly string? _autoDownload;
    private readonly ModelDownloader _downloader = new(AppHttp.Client, new ResumableDownloader(AppHttp.Client));

    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        Dock = DockStyle.Fill,
    };
    private readonly Label _freeSpaceLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 6, 3, 3) };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill, Maximum = 1000, Height = 12, Margin = new Padding(3, 10, 3, 0) };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 6, 3, 3) };
    private readonly Label _messageLabel = new() { AutoSize = true, Margin = new Padding(3, 6, 3, 3), Visible = false };
    private readonly Button _downloadButton = UiStyle.CreateButton(UiText.ModelDownload);
    private readonly Button _deleteButton = UiStyle.CreateButton(UiText.ModelDelete);
    private readonly Button _closeButton = UiStyle.CreateButton(UiText.Close);

    private CancellationTokenSource? _cancellation;
    private Task? _downloadTask;
    private bool _closeWhenStopped;

    /// <param name="exePath">faster-whisper-xxl.exe; the models live in its <c>_models</c> folder.</param>
    /// <param name="autoDownload">Model to download right away; the dialog closes with OK once it is ready.</param>
    public ModelsDialog(string exePath, string? autoDownload = null)
    {
        _modelsDirectory = WhisperModels.ModelsDirectory(exePath);
        _autoDownload = autoDownload;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.ModelsTitle;
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

        UiStyle.MakePrimary(_downloadButton);
        _downloadButton.Click += async (_, _) => await DownloadSelectedAsync();
        _deleteButton.Click += (_, _) => DeleteSelected();
        _closeButton.Click += (_, _) => OnCloseClick();
        _list.SelectedIndexChanged += (_, _) => UpdateButtons();

        RefreshList();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(14),
            MinimumSize = new Size(740, 0),
            MaximumSize = new Size(740, 0),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = UiText.ModelsIntro, AutoSize = true, MaximumSize = new Size(670, 0), Margin = new Padding(3, 0, 3, 10) }, 0, 0);

        _list.Columns.Add(UiText.ModelColumnName);
        _list.Columns.Add(UiText.ModelColumnSize);
        _list.Columns.Add(UiText.ModelColumnStatus);
        _list.Columns.Add(UiText.ModelColumnHint);
        _list.Height = 150;
        layout.Controls.Add(_list, 0, 1);
        layout.Controls.Add(_freeSpaceLabel, 0, 2);
        layout.Controls.Add(_progressBar, 0, 3);
        layout.Controls.Add(_statusLabel, 0, 4);
        layout.Controls.Add(_messageLabel, 0, 5);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_deleteButton);
        buttons.Controls.Add(_downloadButton);
        layout.Controls.Add(buttons, 0, 6);

        Controls.Add(layout);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Column widths from the font, so they fit at every DPI and in both languages.
        var font = _list.Font;
        int Width(params string[] texts) => texts.Max(t => TextRenderer.MeasureText(t, font).Width) + LogicalToDeviceUnits(18);
        _list.Columns[0].Width = Width([UiText.ModelColumnName, .. WhisperModels.All.Select(m => m.Name)]);
        _list.Columns[1].Width = Width(UiText.ModelColumnSize, UiText.FormatSize(3_000_000_000));
        _list.Columns[2].Width = Width(UiText.ModelColumnStatus, UiText.ModelInstalled, UiText.ModelNotInstalled, UiText.ModelPartial);
        _list.Columns[3].Width = Math.Max(LogicalToDeviceUnits(120), _list.ClientSize.Width - _list.Columns[0].Width - _list.Columns[1].Width - _list.Columns[2].Width - LogicalToDeviceUnits(4));
        // Exactly as high as header + rows (the first item's top is the header height).
        var first = _list.GetItemRect(0);
        _list.Height = first.Top + first.Height * _list.Items.Count + LogicalToDeviceUnits(6);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_autoDownload is not null)
            await DownloadSelectedAsync();
    }

    private void RefreshList()
    {
        var selected = SelectedModel?.Name ?? _autoDownload ?? WhisperModels.All[0].Name;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var model in WhisperModels.All)
        {
            var installed = WhisperModels.IsInstalled(_modelsDirectory, model.Name);
            var partial = !installed && Directory.Exists(ModelDownloader.IncompleteDirectory(_modelsDirectory, model.Name));
            var size = installed ? WhisperModels.SizeOnDisk(_modelsDirectory, model.Name) : model.ApproximateSize;
            var item = new ListViewItem([
                model.Name,
                UiText.FormatSize(size),
                installed ? UiText.ModelInstalled : partial ? UiText.ModelPartial : UiText.ModelNotInstalled,
                UiText.ModelHint(model.Name),
            ])
            {
                Tag = model,
                ForeColor = installed ? UiStyle.Success.Fore : SystemColors.WindowText,
                Selected = model.Name == selected,
            };
            _list.Items.Add(item);
        }

        _list.EndUpdate();
        if (FasterWhisperInstaller.AvailableFreeSpace(_modelsDirectory) is { } free)
            _freeSpaceLabel.Text = UiText.ModelsFreeSpace(free);
        UpdateButtons();
    }

    private WhisperModelInfo? SelectedModel =>
        _list.SelectedItems.Count == 1 ? _list.SelectedItems[0].Tag as WhisperModelInfo : null;

    private void UpdateButtons()
    {
        var busy = _downloadTask is not null;
        var installed = SelectedModel is { } model && WhisperModels.IsInstalled(_modelsDirectory, model.Name);
        _downloadButton.Enabled = !busy && SelectedModel is not null && !installed;
        _deleteButton.Enabled = !busy && installed;
        _list.Enabled = !busy;
        _closeButton.Text = busy ? UiText.Cancel : UiText.Close;
    }

    private async Task DownloadSelectedAsync()
    {
        if (SelectedModel is not { } model || _downloadTask is not null)
            return;

        _messageLabel.Visible = false;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        try
        {
            _downloadTask = RunDownloadAsync(model, cancellation.Token);
            UpdateButtons();
            await _downloadTask;
        }
        finally
        {
            _downloadTask = null;
            _cancellation = null;
            RefreshList();
        }

        if (_closeWhenStopped)
            Close();
        else if (_autoDownload is not null && WhisperModels.IsInstalled(_modelsDirectory, _autoDownload))
            DialogResult = DialogResult.OK; // the transcription waiting for this model can start
    }

    private async Task RunDownloadAsync(WhisperModelInfo model, CancellationToken cancellationToken)
    {
        _statusLabel.Text = UiText.SetupLookingUp;
        _progressBar.Style = ProgressBarStyle.Marquee;
        var progress = new Progress<DownloadProgress>(p =>
        {
            _statusLabel.Text = UiText.SetupDownloading(p.Received, p.Total, p.BytesPerSecond, p.Remaining);
            if (p.Fraction is { } fraction)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
                UiStyle.SetProgressImmediately(_progressBar, (int)Math.Round(fraction * _progressBar.Maximum));
            }
        });

        try
        {
            await _downloader.DownloadAsync(model, _modelsDirectory, progress, cancellationToken);
            Finish(string.Empty);
            ShowMessage(UiText.ModelReady(model.Name), UiStyle.Success.Fore);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Finish(UiText.StateCancelled);
        }
        catch (SetupException ex)
        {
            Finish(string.Empty);
            var free = FasterWhisperInstaller.AvailableFreeSpace(_modelsDirectory);
            var text = UiText.SetupErrorText(ex.Error, _modelsDirectory, model.ApproximateSize, free);
            if (!string.IsNullOrWhiteSpace(ex.Detail) && ex.Error == SetupError.DownloadFailed)
                text += Environment.NewLine + UiText.SetupDetail(ex.Detail);
            ShowMessage(text, UiStyle.Danger.Fore);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Finish(string.Empty);
            ShowMessage(UiText.UnexpectedError(ex.Message), UiStyle.Danger.Fore);
        }
    }

    private void Finish(string status)
    {
        _progressBar.Style = ProgressBarStyle.Continuous;
        UiStyle.SetProgressImmediately(_progressBar, 0);
        _statusLabel.Text = status;
    }

    private void ShowMessage(string text, Color color)
    {
        _messageLabel.Text = text;
        _messageLabel.ForeColor = color;
        _messageLabel.MaximumSize = new Size(LogicalToDeviceUnits(670), 0);
        _messageLabel.Visible = true;
    }

    private void DeleteSelected()
    {
        if (SelectedModel is not { } model)
            return;

        var size = WhisperModels.SizeOnDisk(_modelsDirectory, model.Name);
        if (MessageBox.Show(this, UiText.ModelDeleteConfirm(model.Name, size), UiText.ModelsTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            WhisperModels.Delete(_modelsDirectory, model.Name);
            _messageLabel.Visible = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage(UiText.UnexpectedError(ex.Message), UiStyle.Danger.Fore);
        }

        RefreshList();
    }

    private void OnCloseClick()
    {
        if (_downloadTask is null)
        {
            DialogResult = DialogResult.Cancel;
            return;
        }

        // The partial download is kept and continues next time.
        _statusLabel.Text = UiText.SetupCancelling;
        _cancellation?.Cancel();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_downloadTask is not null)
        {
            e.Cancel = true;
            _closeWhenStopped = true;
            _statusLabel.Text = UiText.SetupCancelling;
            _cancellation?.Cancel();
            return;
        }

        base.OnFormClosing(e);
    }
}
