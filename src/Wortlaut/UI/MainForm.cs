using System.Text.RegularExpressions;
using Wortlaut.Core;
using Wortlaut.Core.Gpu;
using Wortlaut.Core.Models;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI;

/// <summary>
/// Main window: shared faster-whisper settings on top, one tab per working mode, status bar at the bottom.
/// </summary>
internal sealed partial class MainForm : Form, IViewHost
{
    private readonly SettingsStore _settingsStore;
    private readonly WindowLayout? _initialLayout;
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 500 };

    private readonly TextBox _exePathBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _browseExeButton = UiStyle.CreateButton(UiText.Browse);
    private readonly Button _setupButton = UiStyle.CreateButton(UiText.SetUp);
    private readonly Button _modelsButton = UiStyle.CreateButton(UiText.ModelsButton);
    private readonly Button _gpuButton = UiStyle.CreateButton(UiText.GpuCheckButton);
    private readonly StatusBadge _exeStatus = new() { Anchor = AnchorStyles.Left, Margin = new Padding(6, 3, 3, 3) };
    private readonly ComboBox _modelBox = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 135 };
    // Narrow boxes keep the settings in one line; the opened lists are as wide as their longest entry (see FitDropDownWidth).
    private readonly ComboBox _deviceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 72 };
    private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 145 };
    private readonly ComboBox _formatBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly CheckBox _wholeSentencesBox = new() { Text = UiText.WholeSentences, AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 15000 };
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
    private readonly ToolStripDropDownButton _uiLanguageButton = new();
    private readonly ToolStripDropDownButton _extrasButton = new();
    private readonly ToolStripMenuItem _extrasGpuItem = new(UiText.ExtrasGpu);
    private readonly ToolStripMenuItem _extrasModelsItem = new(UiText.ModelsButton);

    private readonly SingleFileView _singleFileView;
    private readonly FolderView _folderView;
    private readonly TabPage _singleFilePage = new(UiText.SingleFileTab);
    private readonly TabPage _folderPage = new(UiText.FolderTab);
    private readonly List<IRunView> _views = [];

    private CancellationTokenSource? _runCancellation;
    private (TranscriptionResult Result, bool RemainingNotProcessed)? _pendingCudaProblem;
    private bool _isRunning;
    private bool _closeAfterRun;
    private bool _initializing = true;

    /// <param name="settingsStore">Where the settings are saved.</param>
    /// <param name="settings">
    /// The loaded settings. <see cref="UiText.Language"/> must already match them, because the controls read
    /// their texts while they are created.
    /// </param>
    /// <param name="initialLayout">Window position to restore, e.g. after switching the UI language.</param>
    public MainForm(SettingsStore settingsStore, AppSettings settings, WindowLayout? initialLayout = null)
    {
        _settingsStore = settingsStore;
        _initialLayout = initialLayout;
        Settings = settings;
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

    public bool IsRunning => _isRunning;

    /// <summary>True when the window was closed to reopen it in another UI language.</summary>
    public bool LanguageChangeRequested { get; private set; }

    /// <summary>Position, size and tab when the window was closed.</summary>
    public WindowLayout? LastLayout { get; private set; }

    /// <summary>What is kept when the window is rebuilt in another UI language.</summary>
    internal sealed record WindowLayout(Rectangle Bounds, FormWindowState State, int SelectedTab);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Applied after the DPI scaling of OnLoad, so the size is not scaled a second time.
        if (_initialLayout is not null)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = _initialLayout.Bounds;
            WindowState = _initialLayout.State;
            if (_initialLayout.SelectedTab >= 0 && _initialLayout.SelectedTab < _tabs.TabCount)
                _tabs.SelectedIndex = _initialLayout.SelectedTab;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        LastLayout = new WindowLayout(
            WindowState == FormWindowState.Normal ? Bounds : RestoreBounds,
            WindowState == FormWindowState.Minimized ? FormWindowState.Normal : WindowState,
            _tabs.SelectedIndex);
        base.OnFormClosed(e);
    }

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
        statusStrip.Items.Add(BuildExtrasButton());
        statusStrip.Items.Add(BuildUiLanguageButton());

        Controls.Add(root);
        Controls.Add(statusStrip);
    }

    /// <summary>"Extras ▾" in the status bar: the welcome wizard, the graphics card check and the models.</summary>
    private ToolStripDropDownButton BuildExtrasButton()
    {
        _extrasButton.Text = UiText.ExtrasMenu;
        _extrasButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _extrasButton.DropDownItems.Add(UiText.ExtrasWizard, null, (_, _) => ShowWizard());
        _extrasButton.DropDownItems.Add(_extrasGpuItem);
        _extrasButton.DropDownItems.Add(_extrasModelsItem);
        _extrasGpuItem.Click += (_, _) => ShowGpuCheck();
        _extrasModelsItem.Click += (_, _) => ShowModels(autoDownload: null);
        return _extrasButton;
    }

    /// <summary>"Deutsch ▾" / "Русский ▾" at the right end of the status bar.</summary>
    private ToolStripDropDownButton BuildUiLanguageButton()
    {
        _uiLanguageButton.Text = UiLanguages.NativeName(UiText.Language);
        _uiLanguageButton.ToolTipText = UiText.UiLanguageTooltip;
        _uiLanguageButton.DisplayStyle = ToolStripItemDisplayStyle.Text;

        foreach (var language in UiLanguages.All)
        {
            var item = new ToolStripMenuItem(UiLanguages.NativeName(language)) { Checked = language == UiText.Language };
            item.Click += (_, _) => SwitchUiLanguage(language);
            _uiLanguageButton.DropDownItems.Add(item);
        }

        return _uiLanguageButton;
    }

    /// <summary>Saves the choice and closes the window; Program reopens it in the new language.</summary>
    private void SwitchUiLanguage(UiLanguage language)
    {
        if (language == UiText.Language || _isRunning)
            return;

        Settings.UiLanguage = UiLanguages.Code(language);
        SaveSettings();
        LanguageChangeRequested = true;
        Close();
    }

    private TableLayoutPanel BuildSettingsGrid()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 3 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < 3; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var exeLabel = UiStyle.CreateCaption(UiText.ExePathLabel);
        grid.Controls.Add(exeLabel, 0, 0);
        grid.SetColumnSpan(exeLabel, 4);
        grid.Controls.Add(_exePathBox, 0, 1);
        grid.Controls.Add(_browseExeButton, 1, 1);
        grid.Controls.Add(_exeStatus, 2, 1);
        // Only shown while no faster-whisper-xxl.exe is found (see UpdateExeStatus).
        grid.Controls.Add(_setupButton, 3, 1);

        // Fields wrap into a second line when the window (or a longer translation) leaves too little room,
        // instead of squeezing the program path above.
        var options = new FlowLayoutPanel { WrapContents = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 4, 0, 0) };
        // AutoSize would measure a single line; the height for the actual width is set here instead.
        void FitHeight()
        {
            var height = options.GetPreferredSize(new Size(options.Width, 0)).Height;
            if (options.Height != height)
                options.Height = height;
        }
        options.Resize += (_, _) => FitHeight();
        options.ControlAdded += (_, _) => FitHeight();

        options.Controls.Add(Field(UiText.ModelLabel, WithButton(_modelBox, _modelsButton)));
        options.Controls.Add(Field(UiText.DeviceLabel, WithButton(_deviceBox, _gpuButton)));
        options.Controls.Add(Field(UiText.LanguageLabel, _languageBox));
        options.Controls.Add(Field(UiText.FormatLabel, _formatBox));
        // Next to the format, because it only affects some formats (see tooltip).
        _wholeSentencesBox.Margin = new Padding(3, 6, 3, 3);
        options.Controls.Add(Field(string.Empty, _wholeSentencesBox));

        grid.Controls.Add(options, 0, 2);
        grid.SetColumnSpan(options, 4);
        return grid;
    }

    /// <summary>A combo box with a small button right next to it, e.g. "Modelle…" next to the model box.</summary>
    private static FlowLayoutPanel WithButton(ComboBox box, Button button)
    {
        var cell = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        box.Margin = new Padding(3, 3, 3, 3);
        button.Margin = new Padding(0, 2, 3, 2);
        button.MinimumSize = new Size(0, 0);
        button.Padding = new Padding(6, 0, 6, 0);
        cell.Controls.Add(box);
        cell.Controls.Add(button);
        return cell;
    }

    /// <summary>A caption above a control, as one block of the settings line.</summary>
    private static TableLayoutPanel Field(string caption, Control control)
    {
        var field = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 9, 0) };
        field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = UiStyle.CreateCaption(caption.Length == 0 ? " " : caption);
        field.Controls.Add(label, 0, 0);
        if (control is ComboBox)
            control.Margin = new Padding(3);
        field.Controls.Add(control, 0, 1);
        return field;
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

        foreach (var box in new[] { _modelBox, _deviceBox, _languageBox, _formatBox })
            box.DropDown += (_, _) => FitDropDownWidth(box);

        _wholeSentencesBox.Checked = Settings.WholeSentences;
        _toolTip.SetToolTip(_wholeSentencesBox, UiText.WholeSentencesTooltip);

        UpdateExeStatus();

        _exePathBox.TextChanged += (_, _) =>
        {
            Settings.ExePath = PathInput.Clean(_exePathBox.Text);
            UpdateExeStatus();
            OnWhisperSettingsChanged();
        };
        _browseExeButton.Click += (_, _) => BrowseForExe();
        UiStyle.MakePrimary(_setupButton);
        _setupButton.Margin = new Padding(9, 3, 3, 3);
        _setupButton.Click += (_, _) => ShowWizard();
        _modelsButton.Click += (_, _) => ShowModels(autoDownload: null);
        _gpuButton.Click += (_, _) => ShowGpuCheck();
        _toolTip.SetToolTip(_gpuButton, UiText.GpuCheckTooltip);
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
        _wholeSentencesBox.CheckedChanged += (_, _) =>
        {
            Settings.WholeSentences = _wholeSentencesBox.Checked;
            OnWhisperSettingsChanged();
        };
    }

    /// <summary>Makes the opened list wide enough for the longest entry, e.g. "Определить автоматически".</summary>
    private static void FitDropDownWidth(ComboBox box)
    {
        var widest = box.Items.Cast<object>()
            .Select(item => TextRenderer.MeasureText(box.GetItemText(item), box.Font).Width)
            .DefaultIfEmpty(0)
            .Max();
        box.DropDownWidth = Math.Max(box.Width, widest + SystemInformation.VerticalScrollBarWidth + box.LogicalToDeviceUnits(8));
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
        var found = FasterWhisperLocator.Exists(Settings.ExePath);
        if (found)
            _exeStatus.SetState(UiText.ExeFound, UiStyle.Success);
        else
            _exeStatus.SetState(UiText.ExeNotFound, UiStyle.Danger);
        _setupButton.Visible = !found;
        _modelsButton.Enabled = _extrasModelsItem.Enabled = found; // the models live next to faster-whisper-xxl.exe
        _gpuButton.Enabled = _extrasGpuItem.Enabled = found;       // faster-whisper-xxl.exe performs the check
    }

    /// <summary>Checks the graphics card and offers matching device and model settings.</summary>
    private void ShowGpuCheck()
    {
        if (!FasterWhisperLocator.Exists(Settings.ExePath))
            return;

        using var dialog = new GpuDialog(
            Settings.ExePath.Trim(),
            () => (Settings.Device, Settings.Model),
            ApplyDeviceAndModel);
        dialog.ShowDialog(this);
    }

    /// <summary>Sets device and model through the controls, so the settings are updated and saved as usual.</summary>
    private void ApplyDeviceAndModel(string device, string model)
    {
        if (!_deviceBox.Items.Contains(device))
            _deviceBox.Items.Add(device);
        _deviceBox.SelectedItem = device;
        _modelBox.Text = model;
    }

    /// <summary>Opens the model overview; with <paramref name="autoDownload"/> it downloads that model right away.</summary>
    /// <returns>True if the dialog was closed after the model became ready.</returns>
    private bool ShowModels(string? autoDownload)
    {
        if (!FasterWhisperLocator.Exists(Settings.ExePath))
            return false;

        using var dialog = new ModelsDialog(Settings.ExePath.Trim(), autoDownload);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    /// <summary>
    /// Makes sure a known model is downloaded before a transcription starts, so faster-whisper does not
    /// silently download gigabytes while the UI shows no progress.
    /// </summary>
    private bool EnsureModelDownloaded(WhisperSettings settings)
    {
        if (WhisperModels.Find(settings.Model) is not { } model)
            return true; // a name typed by the user: faster-whisper handles it (the view logs a hint)

        if (WhisperModels.IsInstalled(WhisperModels.ModelsDirectory(settings.ExePath), model.Name))
            return true;

        if (MessageBox.Show(this, UiText.ModelMissingAsk(model.Name, model.ApproximateSize), UiText.AppTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return false;
        }

        return ShowModels(model.Name) && WhisperModels.IsInstalled(WhisperModels.ModelsDirectory(settings.ExePath), model.Name);
    }

    /// <summary>
    /// The welcome wizard: downloads Faster-Whisper-XXL into %LOCALAPPDATA%\Wortlaut, checks the graphics card and
    /// downloads the model. It changes <see cref="Settings"/> directly; the controls are updated afterwards.
    /// </summary>
    private void ShowWizard()
    {
        if (_isRunning)
            return;

        SaveSettings();
        using var wizard = new Wizard.SetupWizard(_settingsStore, Settings, FasterWhisperInstaller.DefaultRoot, openedFromMainWindow: true);
        wizard.ShowDialog(this);

        if (wizard.Outcome == Wizard.WizardOutcome.StartedCopy)
        {
            Close(); // the copy in the user folder is running now
            return;
        }

        if (wizard.LanguageChanged)
        {
            // The texts of this window are fixed at creation: rebuild it in the new language.
            LanguageChangeRequested = true;
            Close();
            return;
        }

        ReloadWhisperSettings();
    }

    /// <summary>Shows settings that were changed outside this window (by the wizard).</summary>
    private void ReloadWhisperSettings()
    {
        _exePathBox.Text = Settings.ExePath;
        UpdateExeStatus();
        ApplyDeviceAndModel(Settings.Device, Settings.Model);
        OnWhisperSettingsChanged();
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

        var settings = Settings.ToWhisperSettings();
        return EnsureModelDownloaded(settings) ? settings : null;
    }

    public void ShowWarning(string message) =>
        MessageBox.Show(this, message, UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // ----- Running -----

    public async Task RunExclusiveAsync(IRunView view, Func<CancellationToken, Task> work)
    {
        if (_isRunning)
            return;

        using var cancellation = new CancellationTokenSource();
        _isRunning = true;
        _runCancellation = cancellation;
        _pendingCudaProblem = null;
        ApplyRunState(view);

        try
        {
            await work(cancellation.Token);
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
            _isRunning = false;
            _runCancellation = null;
            ApplyRunState(null);
        }

        if (_closeAfterRun)
        {
            BeginInvoke(Close);
            return;
        }

        // Shown after the run, when the settings can be changed again.
        if (_pendingCudaProblem is { } cudaProblem)
        {
            _pendingCudaProblem = null;
            OfferCudaFallback(cudaProblem.Result, cudaProblem.RemainingNotProcessed);
        }
    }

    public void ReportCudaProblem(TranscriptionResult result, bool remainingNotProcessed)
    {
        // Only relevant while the graphics card is selected; the first report of a run wins.
        if (result.CudaProblem != CudaProblem.None && CudaErrors.UsesCuda(Settings.Device))
            _pendingCudaProblem ??= (result, remainingNotProcessed);
    }

    /// <summary>
    /// Explains why the graphics card could not be used and offers a working alternative:
    /// a smaller model if the memory was too small for a large one, the processor otherwise.
    /// </summary>
    private void OfferCudaFallback(TranscriptionResult result, bool remainingNotProcessed)
    {
        var smallerModel = result.CudaProblem == CudaProblem.OutOfMemory
            && Settings.Model.Trim() is "large-v2" or "large-v3"
            ? GpuAdvisor.SmallerModel
            : null;

        var text = UiText.CudaProblemMessage(result.CudaProblem);
        if (remainingNotProcessed)
            text += " " + UiText.CudaRemainingNotProcessed;
        if (!string.IsNullOrWhiteSpace(result.Detail))
            text += Environment.NewLine + Environment.NewLine + UiText.SetupDetail(result.Detail);
        text += Environment.NewLine + Environment.NewLine
            + (smallerModel is null ? UiText.CudaSwitchToCpuQuestion : UiText.CudaSwitchModelQuestion(smallerModel));

        if (MessageBox.Show(this, text, UiText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        if (smallerModel is not null)
            _modelBox.Text = smallerModel;
        else
            ApplyDeviceAndModel("cpu", Settings.Model);
    }

    public void CancelRun() => _runCancellation?.Cancel();

    private void ApplyRunState(IRunView? runningView)
    {
        _settingsGroup.Enabled = runningView is null;
        _uiLanguageButton.Enabled = runningView is null;
        _extrasButton.Enabled = runningView is null;
        foreach (var view in _views)
        {
            view.SetRunState(runningView is null
                ? RunState.Idle
                : ReferenceEquals(view, runningView) ? RunState.RunningHere : RunState.RunningElsewhere);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isRunning)
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
            _toolTip.Dispose();
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
