using System.Diagnostics;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI;

/// <summary>
/// Downloads and installs Faster-Whisper-XXL with one click. Shows what will happen (version, size, target,
/// license), the progress, and plain-language errors with "try again" (the download resumes).
/// </summary>
internal sealed class SetupDialog : Form
{
    private const string ProjectUrl = "https://github.com/Purfview/whisper-standalone-win";

    private readonly string _root;
    private readonly FasterWhisperReleaseFinder _finder = new(AppHttp.Client);
    private readonly FasterWhisperInstaller _installer = new(
        new ResumableDownloader(AppHttp.Client), new SevenZipExtractor(), new FasterWhisperProbe());

    private readonly Label _versionValue = CreateValueLabel();
    private readonly Label _downloadValue = CreateValueLabel();
    private readonly Label _spaceValue = CreateValueLabel();
    private readonly Label _targetValue = CreateValueLabel();
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill, Maximum = 1000, Height = 12, MarqueeAnimationSpeed = 30 };
    private readonly Label _statusLabel = new() { AutoSize = true, Dock = DockStyle.Fill, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 6, 3, 3) };
    private readonly Label _messageLabel = new() { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(3, 8, 3, 3), Visible = false };
    private readonly Button _startButton = UiStyle.CreateButton(UiText.SetupStart);
    private readonly Button _cancelButton = UiStyle.CreateButton(UiText.Cancel);

    private FasterWhisperPackage? _package;
    private CancellationTokenSource? _cancellation;
    private Task? _installTask;
    private bool _closeWhenCancelled;

    public SetupDialog(string root)
    {
        _root = root;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.SetupTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        // The height follows the content, e.g. when a longer error message appears.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BuildLayout();
        ResumeLayout(false);
        PerformLayout();

        UiStyle.MakePrimary(_startButton);
        _startButton.Enabled = false;
        _startButton.Click += async (_, _) => await OnStartClickAsync();
        _cancelButton.Click += (_, _) => OnCancelClick();
        _targetValue.Text = FasterWhisperInstaller.InstallDirectory(root);
        AcceptButton = _startButton;
    }

    /// <summary>Path of the installed faster-whisper-xxl.exe after a successful setup.</summary>
    public string? InstalledExePath { get; private set; }

    private static Label CreateValueLabel() =>
        new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 3, 4), MaximumSize = new Size(480, 0) };

    private void BuildLayout()
    {
        // Fixed width (texts wrap inside it), height from the content.
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(14),
            MinimumSize = new Size(640, 0),
            MaximumSize = new Size(640, 0),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var intro = new Label { Text = UiText.SetupIntro, AutoSize = true, MaximumSize = new Size(590, 0), Margin = new Padding(3, 0, 3, 10) };
        layout.Controls.Add(intro, 0, 0);

        var details = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddDetail(details, UiText.SetupVersionLabel, _versionValue);
        AddDetail(details, UiText.SetupDownloadLabel, _downloadValue);
        AddDetail(details, UiText.SetupSpaceLabel, _spaceValue);
        AddDetail(details, UiText.SetupTargetLabel, _targetValue);
        var license = new LinkLabel { Text = UiText.SetupLicenseLink, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 3, 4) };
        license.LinkClicked += (_, _) => OpenProjectPage();
        AddDetail(details, UiText.SetupLicenseLabel, license);
        layout.Controls.Add(details, 0, 1);

        layout.Controls.Add(_progressBar, 0, 2);
        _progressBar.Margin = new Padding(3, 12, 3, 0);
        layout.Controls.Add(_statusLabel, 0, 3);
        layout.Controls.Add(_messageLabel, 0, 4);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_startButton);
        layout.Controls.Add(buttons, 0, 5);

        Controls.Add(layout);
    }

    private static void AddDetail(TableLayoutPanel details, string caption, Control value)
    {
        var row = details.RowCount++;
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.Controls.Add(new Label { Text = caption + ":", AutoSize = true, ForeColor = UiStyle.MutedText, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 12, 4) }, 0, row);
        details.Controls.Add(value, 1, row);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LookUpPackageAsync();
    }

    /// <summary>Asks GitHub for the newest version and checks the free space.</summary>
    private async Task LookUpPackageAsync()
    {
        _statusLabel.Text = UiText.SetupLookingUp;
        _progressBar.Style = ProgressBarStyle.Marquee;

        var (package, isFallback) = await _finder.FindLatestAsync(CancellationToken.None);
        if (IsDisposed)
            return;

        _package = package;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _versionValue.Text = UiText.SetupVersion(package.Version, isFallback);
        _downloadValue.Text = UiText.FormatSize(package.Size);
        _spaceValue.Text = UiText.SetupSpace(FasterWhisperInstaller.RequiredFreeSpace(_root, package), FasterWhisperInstaller.AvailableFreeSpace(_root));
        _statusLabel.Text = string.Empty;
        _startButton.Enabled = true;
        _startButton.Focus();
    }

    private async Task OnStartClickAsync()
    {
        if (InstalledExePath is not null)
        {
            DialogResult = DialogResult.OK; // "Fertig"
            return;
        }

        if (_package is not { } package || _installTask is not null)
            return;

        _messageLabel.Visible = false;
        _startButton.Enabled = false;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        try
        {
            _installTask = RunInstallAsync(package, cancellation.Token);
            await _installTask;
        }
        finally
        {
            _installTask = null;
            _cancellation = null;
        }

        if (_closeWhenCancelled)
            Close();
    }

    private async Task RunInstallAsync(FasterWhisperPackage package, CancellationToken cancellationToken)
    {
        var progress = new Progress<SetupProgress>(OnProgress);
        try
        {
            InstalledExePath = await _installer.InstallAsync(package, _root, progress, cancellationToken);
            UiStyle.SetProgressImmediately(_progressBar, _progressBar.Maximum);
            _progressBar.Style = ProgressBarStyle.Continuous;
            _statusLabel.Text = string.Empty;
            ShowMessage(UiText.SetupDone(package.Version), UiStyle.Success.Fore);
            _startButton.Text = UiText.SetupFinish;
            _startButton.Enabled = true;
            _cancelButton.Visible = false;
            _startButton.Focus();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ResetAfterStop(UiText.StateCancelled);
        }
        catch (SetupException ex)
        {
            var required = FasterWhisperInstaller.RequiredFreeSpace(_root, package);
            var text = UiText.SetupErrorText(ex.Error, FasterWhisperInstaller.InstallDirectory(_root), required, FasterWhisperInstaller.AvailableFreeSpace(_root));
            if (!string.IsNullOrWhiteSpace(ex.Detail) && ex.Error is SetupError.DownloadFailed or SetupError.ExtractFailed)
                text += Environment.NewLine + UiText.SetupDetail(ex.Detail);
            ResetAfterStop(string.Empty);
            ShowMessage(text, UiStyle.Danger.Fore);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ResetAfterStop(string.Empty);
            ShowMessage(UiText.UnexpectedError(ex.Message), UiStyle.Danger.Fore);
        }
    }

    private void OnProgress(SetupProgress progress)
    {
        switch (progress.Stage)
        {
            case SetupStage.Downloading:
                _statusLabel.Text = UiText.SetupDownloading(progress.Done, progress.Total, progress.BytesPerSecond, progress.Remaining);
                break;
            case SetupStage.Extracting:
                _statusLabel.Text = UiText.SetupExtracting(progress.Fraction);
                break;
            case SetupStage.Verifying:
                _statusLabel.Text = UiText.SetupVerifying;
                break;
        }

        if (progress.Fraction is { } fraction)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            UiStyle.SetProgressImmediately(_progressBar, (int)Math.Round(fraction * _progressBar.Maximum));
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
        }
    }

    private void ResetAfterStop(string status)
    {
        _progressBar.Style = ProgressBarStyle.Continuous;
        UiStyle.SetProgressImmediately(_progressBar, 0);
        _statusLabel.Text = status;
        _startButton.Text = UiText.SetupRetry;
        _startButton.Enabled = true;
        _cancelButton.Enabled = true;
    }

    private void ShowMessage(string text, Color color)
    {
        _messageLabel.Text = text;
        _messageLabel.ForeColor = color;
        _messageLabel.MaximumSize = new Size(LogicalToDeviceUnits(600), 0);
        _messageLabel.Visible = true;
    }

    private void OnCancelClick()
    {
        if (_installTask is null)
        {
            DialogResult = InstalledExePath is null ? DialogResult.Cancel : DialogResult.OK;
            return;
        }

        if (!ConfirmCancel())
            return;

        _cancelButton.Enabled = false;
        _statusLabel.Text = UiText.SetupCancelling;
        _cancellation?.Cancel();
    }

    private bool ConfirmCancel() =>
        MessageBox.Show(this, UiText.SetupConfirmCancel, UiText.SetupTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // A running setup is cancelled cleanly first (the partial download stays for next time).
        if (_installTask is not null)
        {
            e.Cancel = true;
            if (!_closeWhenCancelled && (e.CloseReason != CloseReason.UserClosing || ConfirmCancel()))
            {
                _closeWhenCancelled = true;
                _cancelButton.Enabled = false;
                _statusLabel.Text = UiText.SetupCancelling;
                _cancellation?.Cancel();
            }

            return;
        }

        if (InstalledExePath is not null)
            DialogResult = DialogResult.OK;

        base.OnFormClosing(e);
    }

    private static void OpenProjectPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ProjectUrl) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // No browser configured; the address is visible in the README.
        }
    }
}
