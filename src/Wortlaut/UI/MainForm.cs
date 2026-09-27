using System.Text.RegularExpressions;
using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>
/// Main window: shared faster-whisper settings on top, one tab per working mode, status bar at the bottom.
/// </summary>
internal sealed partial class MainForm : Form, IViewHost
{
    private readonly SettingsStore _settingsStore = new();
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 500 };

    private readonly TextBox _exePathBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _browseExeButton = UiStyle.CreateButton(UiText.Browse);
    private readonly StatusBadge _exeStatus = new() { Anchor = AnchorStyles.Left, Margin = new Padding(6, 3, 3, 3) };
    private readonly ComboBox _modelBox = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 150 };
    private readonly ComboBox _deviceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 200 };
    private readonly ComboBox _formatBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly GroupBox _settingsGroup = new()
    {
        Text = UiText.SettingsGroup,
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(10, 6, 10, 8),
    };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Padding = new Point(14, 5) };
    private readonly ToolStripStatusLabel _summaryLabel = new();
    private readonly ToolStripStatusLabel _savedLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleRight };

    private readonly SingleFileView _singleFileView;
    private readonly FolderView _folderView;
    private readonly TabPage _singleFilePage = new(UiText.SingleFileTab);
    private readonly TabPage _folderPage = new(UiText.FolderTab);
    private readonly List<IRunView> _views = [];

    private CancellationTokenSource? _runCancellation;
    private Task? _runTask;
    private bool _closeAfterRun;
    private bool _initializing = true;

    public MainForm()
    {
        Settings = _settingsStore.Load();
        DurationProbe = new MediaDurationProbe(() => MediaDurationProbe.FindFfmpegNextTo(Settings.ExePath));
        Job = new TranscriptionJob(new WhisperRunner());

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 720);
        MinimumSize = new Size(760, 600);

        _singleFileView = new SingleFileView(this);
        _folderView = new FolderView(this);
        _views.Add(_singleFileView);
        _views.Add(_folderView);
        BuildLayout();
        ResumeLayout(false);
        PerformLayout();

        InitializeSettingsControls();
        EnableFileDrop(this);
        ActiveControl = _singleFileView;

        _saveTimer.Tick += (_, _) => SaveSettings();
        _initializing = false;
        UpdateStatusBar();
    }

    public AppSettings Settings { get; }

    public TranscriptionJob Job { get; }

    public IMediaDurationProbe DurationProbe { get; }

    public bool IsRunning => _runTask is not null;

    // ----- Layout -----

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10, 8, 10, 4) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _settingsGroup.Controls.Add(BuildSettingsGrid());
        root.Controls.Add(_settingsGroup, 0, 0);

        _singleFilePage.Controls.Add(_singleFileView);
        _singleFilePage.UseVisualStyleBackColor = true;
        _folderPage.Controls.Add(_folderView);
        _folderPage.UseVisualStyleBackColor = true;
        _tabs.TabPages.Add(_singleFilePage);
        _tabs.TabPages.Add(_folderPage);
        _tabs.Margin = new Padding(3, 10, 3, 3);
        root.Controls.Add(_tabs, 0, 1);

        var statusStrip = new StatusStrip { SizingGrip = true, ShowItemToolTips = true };
        statusStrip.Items.Add(_summaryLabel);
        statusStrip.Items.Add(_savedLabel);

        Controls.Add(root);
        Controls.Add(statusStrip);
    }

    private TableLayoutPanel BuildSettingsGrid()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 3 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < 3; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var exeLabel = UiStyle.CreateCaption(UiText.ExePathLabel);
        grid.Controls.Add(exeLabel, 0, 0);
        grid.SetColumnSpan(exeLabel, 3);
        grid.Controls.Add(_exePathBox, 0, 1);
        grid.Controls.Add(_browseExeButton, 1, 1);
        grid.Controls.Add(_exeStatus, 2, 1);

        var options = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, RowCount = 2, Margin = new Padding(0, 4, 0, 0) };
        for (var i = 0; i < 4; i++)
            options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        options.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        (string Caption, ComboBox Box)[] fields =
        [
            (UiText.ModelLabel, _modelBox),
            (UiText.DeviceLabel, _deviceBox),
            (UiText.LanguageLabel, _languageBox),
            (UiText.FormatLabel, _formatBox),
        ];
        for (var column = 0; column < fields.Length; column++)
        {
            options.Controls.Add(UiStyle.CreateCaption(fields[column].Caption), column, 0);
            fields[column].Box.Margin = new Padding(3, 3, 12, 3);
            options.Controls.Add(fields[column].Box, column, 1);
        }

        grid.Controls.Add(options, 0, 2);
        grid.SetColumnSpan(options, 3);
        return grid;
    }

    // ----- Settings -----

    private void InitializeSettingsControls()
    {
        // First start: look for faster-whisper-xxl.exe next to Wortlaut.exe and in the current directory.
        if (string.IsNullOrWhiteSpace(Settings.ExePath) && FasterWhisperLocator.FindDefault() is { } found)
        {
            Settings.ExePath = found;
            ScheduleSave();
        }

        _exePathBox.Text = Settings.ExePath;

        _modelBox.Items.AddRange([.. WhisperSettings.KnownModels]);
        _modelBox.Text = Settings.Model;

        _deviceBox.Items.AddRange([.. WhisperSettings.KnownDevices]);
        if (!_deviceBox.Items.Contains(Settings.Device))
            _deviceBox.Items.Add(Settings.Device); // e.g. "cuda:1" from a hand-edited settings file
        _deviceBox.SelectedItem = Settings.Device;

        _languageBox.Items.AddRange([.. WhisperSettings.KnownLanguages.Select(UiText.LanguageName)]);
        _languageBox.Text = UiText.LanguageName(Settings.Language);

        foreach (var info in OutputFormats.All)
            _formatBox.Items.Add(new FormatItem(info));
        _formatBox.SelectedIndex = OutputFormats.All.ToList().FindIndex(info => info.Format == Settings.Format);

        UpdateExeStatus();

        _exePathBox.TextChanged += (_, _) =>
        {
            Settings.ExePath = _exePathBox.Text.Trim();
            UpdateExeStatus();
            OnWhisperSettingsChanged();
        };
        _browseExeButton.Click += (_, _) => BrowseForExe();
        _modelBox.TextChanged += (_, _) =>
        {
            Settings.Model = _modelBox.Text.Trim();
            OnWhisperSettingsChanged();
        };
        _deviceBox.SelectedIndexChanged += (_, _) =>
        {
            Settings.Device = _deviceBox.SelectedItem as string ?? WhisperSettings.DefaultDevice;
            OnWhisperSettingsChanged();
        };
        _languageBox.TextChanged += (_, _) =>
        {
            Settings.Language = ParseLanguage(_languageBox.Text);
            OnWhisperSettingsChanged();
        };
        _formatBox.SelectedIndexChanged += (_, _) =>
        {
            if (_formatBox.SelectedItem is FormatItem item)
                Settings.Format = item.Info.Format;
            OnWhisperSettingsChanged();
        };
    }

    /// <summary>"Russisch (ru)" → "ru", "Automatisch erkennen" → "auto", anything else is taken as typed (e.g. "fr").</summary>
    internal static string ParseLanguage(string text)
    {
        var trimmed = text.Trim();
        foreach (var code in WhisperSettings.KnownLanguages)
        {
            if (string.Equals(trimmed, UiText.LanguageName(code), StringComparison.OrdinalIgnoreCase))
                return code;
        }

        var match = LanguageCodeInParentheses().Match(trimmed);
        if (match.Success)
            return match.Groups["code"].Value.Trim();

        return trimmed.Length == 0 ? WhisperSettings.AutoLanguage : trimmed;
    }

    [GeneratedRegex(@"\((?<code>[^()]+)\)\s*$")]
    private static partial Regex LanguageCodeInParentheses();

    private void BrowseForExe()
    {
        using var dialog = new OpenFileDialog
        {
            Title = UiText.ExeDialogTitle,
            Filter = UiText.ExeDialogFilter,
            CheckFileExists = true,
        };

        if (FasterWhisperLocator.Exists(Settings.ExePath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(Settings.ExePath.Trim());
            dialog.FileName = Path.GetFileName(Settings.ExePath.Trim());
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _exePathBox.Text = dialog.FileName;
    }

    private void UpdateExeStatus()
    {
        if (FasterWhisperLocator.Exists(Settings.ExePath))
            _exeStatus.SetState(UiText.ExeFound, UiStyle.Success);
        else
            _exeStatus.SetState(UiText.ExeNotFound, UiStyle.Danger);
    }

    private void OnWhisperSettingsChanged()
    {
        if (_initializing)
            return;

        UpdateStatusBar();
        foreach (var view in _views)
            view.OnWhisperSettingsChanged();
        ScheduleSave();
    }

    public void SettingsChanged()
    {
        if (!_initializing)
            ScheduleSave();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveSettings()
    {
        _saveTimer.Stop();
        try
        {
            _settingsStore.Save(Settings);
        }
        catch (IOException ex)
        {
            _savedLabel.Text = UiText.SettingsNotSaved(ex.Message);
            return;
        }

        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        _summaryLabel.Text = UiText.SettingsSummary(Settings.ToWhisperSettings());

        var location = _settingsStore.UsesFallback
            ? Path.Combine("%APPDATA%", "Wortlaut", SettingsStore.FileName)
            : SettingsStore.FileName;
        _savedLabel.Text = UiText.SettingsSavedIn(location);
        _savedLabel.ToolTipText = _settingsStore.CurrentPath;
    }

    public WhisperSettings? ValidateWhisperSettings()
    {
        if (!FasterWhisperLocator.Exists(Settings.ExePath))
        {
            ShowWarning(UiText.ExeMissing);
            _exePathBox.Focus();
            return null;
        }

        if (string.IsNullOrWhiteSpace(Settings.Model))
        {
            ShowWarning(UiText.ModelMissing);
            _modelBox.Focus();
            return null;
        }

        if (string.IsNullOrWhiteSpace(Settings.Device))
        {
            ShowWarning(UiText.DeviceMissing);
            _deviceBox.Focus();
            return null;
        }

        return Settings.ToWhisperSettings();
    }

    public void ShowWarning(string message) =>
        MessageBox.Show(this, message, UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // ----- Running -----

    public async Task RunExclusiveAsync(IRunView view, Func<CancellationToken, Task> work)
    {
        if (_runTask is not null)
            return;

        using var cancellation = new CancellationTokenSource();
        _runCancellation = cancellation;
        ApplyRunState(view);

        try
        {
            _runTask = work(cancellation.Token);
            await _runTask;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Cancelled by the user; the view has already reported it.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.UnexpectedError(ex.Message), UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _runTask = null;
            _runCancellation = null;
            ApplyRunState(null);
        }

        if (_closeAfterRun)
            BeginInvoke(Close);
    }

    public void CancelRun() => _runCancellation?.Cancel();

    private void ApplyRunState(IRunView? runningView)
    {
        _settingsGroup.Enabled = runningView is null;
        foreach (var view in _views)
        {
            view.SetRunState(runningView is null
                ? RunState.Idle
                : ReferenceEquals(view, runningView) ? RunState.RunningHere : RunState.RunningElsewhere);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_runTask is not null)
        {
            // Never leave faster-whisper running in the background: cancel, wait for the cleanup, then close.
            e.Cancel = true;
            if (_closeAfterRun)
                return;

            if (e.CloseReason == CloseReason.UserClosing
                && MessageBox.Show(this, UiText.ConfirmClose, UiText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            _closeAfterRun = true;
            CancelRun();
            return;
        }

        if (_saveTimer.Enabled)
            SaveSettings();

        base.OnFormClosing(e);
    }

    // ----- Drag & drop -----

    /// <summary>Accepts dropped files on every control of the window, not only on empty areas.</summary>
    private void EnableFileDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += OnDragEnter;
        control.DragDrop += OnDragDrop;
        foreach (Control child in control.Controls)
            EnableFileDrop(child);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = !IsRunning && GetDroppedPath(e) is { } path && IsAcceptedDrop(path)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (IsRunning || GetDroppedPath(e) is not { } path || !IsAcceptedDrop(path))
            return;

        HandleDroppedPath(path);
    }

    private static string? GetDroppedPath(DragEventArgs e) =>
        e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } paths ? paths[0] : null;

    /// <summary>A media file (single-file tab) or a folder (folder tab).</summary>
    private static bool IsAcceptedDrop(string path) =>
        (File.Exists(path) && MediaFiles.IsSupported(path)) || Directory.Exists(path);

    private async void HandleDroppedPath(string path)
    {
        if (Directory.Exists(path))
        {
            _tabs.SelectedTab = _folderPage;
            await _folderView.SetFolderAsync(path);
        }
        else
        {
            _tabs.SelectedTab = _singleFilePage;
            _singleFileView.SetMediaFile(path);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _saveTimer.Dispose();
            _runCancellation?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Format entry of the format combo box.</summary>
    private sealed record FormatItem(OutputFormatInfo Info)
    {
        public override string ToString() => UiText.FormatName(Info);
    }
}
