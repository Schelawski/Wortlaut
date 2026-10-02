using System.Diagnostics;
using Wortlaut.Core;
using Wortlaut.Core.Models;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>How the wizard ended.</summary>
internal enum WizardOutcome
{
    /// <summary>Closed before the end; it continues at the same page next time.</summary>
    Cancelled,

    /// <summary>"Erste Datei transkribieren": the main window opens.</summary>
    Finished,

    /// <summary>Wortlaut was copied to the user folder and the copy was started; this instance should exit.</summary>
    StartedCopy,
}

/// <summary>
/// Welcome wizard for the first start: language → download Faster-Whisper-XXL → graphics card → model → done.
/// It can be closed at any time and continues at the same page on the next start.
/// </summary>
internal sealed class SetupWizard : Form
{
    private readonly SettingsStore _store;
    private readonly bool _rememberProgress;

    private readonly Label _stepLabel = new() { AutoSize = true, ForeColor = UiStyle.MutedText, Margin = new Padding(3, 0, 3, 2) };
    private readonly Label _titleLabel = new() { AutoSize = true, Margin = new Padding(0, 0, 3, 0) };
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 6) };
    private readonly Button _backButton = UiStyle.CreateButton(UiText.WizardBack);
    private readonly Button _primaryButton = UiStyle.CreateButton(UiText.WizardNext);
    private readonly Button _secondaryButton = UiStyle.CreateButton(string.Empty);
    private readonly Button _cancelButton = UiStyle.CreateButton(UiText.Cancel);

    private WizardPage? _page;
    private bool _closeWhenStopped;
    private bool _actionRunning;

    /// <param name="store">Where the settings are saved after every step.</param>
    /// <param name="settings">The settings the wizard fills in (shared with the main window).</param>
    /// <param name="root">Install folder, normally <c>%LOCALAPPDATA%\Wortlaut</c>.</param>
    /// <param name="openedFromMainWindow">
    /// Opened by the user via "Extras" while Faster-Whisper-XXL is set up: closing it early is not remembered.
    /// </param>
    public SetupWizard(SettingsStore store, AppSettings settings, string root, bool openedFromMainWindow)
    {
        _store = store;
        Settings = settings;
        Root = root;

        // First start: use an installation that is already there (e.g. next to Wortlaut.exe).
        if (!FasterWhisperLocator.Exists(Settings.ExePath) && FasterWhisperLocator.FindDefault(root) is { } found)
            Settings.ExePath = found;

        _rememberProgress = !(openedFromMainWindow && ExeFound);

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = UiText.SetupTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = !openedFromMainWindow;
        StartPosition = openedFromMainWindow ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        ClientSize = new Size(680, 540);
        BuildLayout();
        ResumeLayout(false);
        PerformLayout();

        UiStyle.MakePrimary(_primaryButton);
        _primaryButton.Click += async (_, _) => await RunPageActionAsync(page => page.OnPrimaryAsync());
        _secondaryButton.Click += async (_, _) => await RunPageActionAsync(page => page.OnSecondaryAsync());
        _backButton.Click += (_, _) => GoBack();
        _cancelButton.Click += (_, _) => Close();
        AcceptButton = _primaryButton;

        ShowStep(openedFromMainWindow ? WizardStep.Welcome : SetupWizardFlow.StartStep(Settings.WizardResumeStep, ExeFound));
    }

    public AppSettings Settings { get; }

    /// <summary>Install folder of Faster-Whisper-XXL (and of the optional copy of Wortlaut).</summary>
    public string Root { get; }

    public WizardOutcome Outcome { get; private set; } = WizardOutcome.Cancelled;

    /// <summary>The UI language was changed; an open main window has to be rebuilt.</summary>
    public bool LanguageChanged { get; private set; }

    public bool ExeFound => FasterWhisperLocator.Exists(Settings.ExePath);

    /// <summary>True when the selected model is downloaded, or is no known model (then there is nothing to download).</summary>
    public bool ModelInstalled =>
        !ExeFound
        || WhisperModels.Find(Settings.Model) is not { } model
        || WhisperModels.IsInstalled(WhisperModels.ModelsDirectory(Settings.ExePath), model.Name);

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Header: "Schritt 2 von 5" above a large title, on a white band.
        _titleLabel.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = SystemColors.Window,
            Padding = new Padding(20, 14, 20, 12),
            Margin = new Padding(0),
        };
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(_stepLabel, 0, 0);
        header.Controls.Add(_titleLabel, 0, 1);
        root.Controls.Add(header, 0, 0);

        root.Controls.Add(_pageHost, 0, 1);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, Padding = new Padding(14, 8, 14, 12), Margin = new Padding(0) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.Controls.Add(_backButton, 0, 0);
        buttons.Controls.Add(_secondaryButton, 2, 0);
        buttons.Controls.Add(_primaryButton, 3, 0);
        buttons.Controls.Add(_cancelButton, 4, 0);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
    }

    // ----- Navigation -----

    public void GoNext()
    {
        if (_page is not null)
            ShowStep(SetupWizardFlow.Next(_page.Step, ExeFound, ModelInstalled));
    }

    private void GoBack()
    {
        if (_page is { IsBusy: false })
            ShowStep(SetupWizardFlow.Previous(_page.Step, ExeFound, ModelInstalled));
    }

    private void ShowStep(WizardStep step)
    {
        var page = CreatePage(step);
        // The window was scaled to the screen's DPI when it was created; pages created later are scaled here
        // (sizes, margins and text widths are written for 96 DPI).
        if (DeviceDpi != 96)
            page.Scale(new SizeF(DeviceDpi / 96f, DeviceDpi / 96f));

        var old = _page;
        _page = page;

        _pageHost.SuspendLayout();
        _pageHost.Controls.Clear();
        _pageHost.Controls.Add(page);
        _pageHost.ResumeLayout();
        old?.Dispose();

        if (_rememberProgress && step != WizardStep.Done)
        {
            Settings.WizardResumeStep = step;
            SaveSettings();
        }

        UpdateChrome();
        _ = ShowPageAsync(page);
    }

    private async Task ShowPageAsync(WizardPage page)
    {
        // Let the page appear before it starts working (e.g. looking up the newest version).
        await Task.Yield();
        if (ReferenceEquals(page, _page) && !page.IsDisposed)
            await page.OnShownAsync();
    }

    private WizardPage CreatePage(WizardStep step) => step switch
    {
        WizardStep.Welcome => new WelcomePage(this),
        WizardStep.Install => new InstallPage(this),
        WizardStep.Graphics => new GraphicsPage(this),
        WizardStep.Model => new ModelPage(this),
        _ => new DonePage(this),
    };

    /// <summary>Called by the page when its state changed.</summary>
    public void OnPageChanged(WizardPage page)
    {
        if (!ReferenceEquals(page, _page))
            return;

        if (_closeWhenStopped && !page.IsBusy)
        {
            Close();
            return;
        }

        UpdateChrome();
    }

    private void UpdateChrome()
    {
        if (_page is not { } page)
            return;

        Text = UiText.SetupTitle;
        _stepLabel.Text = UiText.WizardStepOf((int)page.Step + 1, SetupWizardFlow.StepCount);
        _titleLabel.Text = page.Title;

        _backButton.Text = UiText.WizardBack;
        _backButton.Visible = page.Step != WizardStep.Welcome;
        _backButton.Enabled = !page.IsBusy && !_actionRunning;

        _primaryButton.Text = page.PrimaryText;
        _primaryButton.Enabled = page.PrimaryEnabled && !_actionRunning;

        _secondaryButton.Visible = page.SecondaryText is not null;
        _secondaryButton.Text = page.SecondaryText ?? string.Empty;
        _secondaryButton.Enabled = page.SecondaryEnabled && !_actionRunning;

        _cancelButton.Text = UiText.Cancel;
        _cancelButton.Enabled = !_closeWhenStopped;
    }

    private async Task RunPageActionAsync(Func<WizardPage, Task> action)
    {
        if (_page is not { } page || _actionRunning)
            return;

        _actionRunning = true;
        UpdateChrome();
        try
        {
            await action(page);
        }
        finally
        {
            _actionRunning = false;
            if (!IsDisposed)
                UpdateChrome();
        }
    }

    // ----- Services for the pages -----

    public void SaveSettings()
    {
        try
        {
            _store.Save(Settings);
        }
        catch (IOException)
        {
            // Not fatal here: the main window reports it when it saves.
        }
    }

    /// <summary>Switches all texts; the current page is rebuilt in the new language.</summary>
    public void SwitchLanguage(UiLanguage language)
    {
        if (language == UiText.Language || _page is not { IsBusy: false } page)
            return;

        UiLanguages.Apply(language);
        Settings.UiLanguage = UiLanguages.Code(language);
        SaveSettings();
        LanguageChanged = true;
        ShowStep(page.Step);
    }

    /// <summary>"Ich habe Faster-Whisper-XXL schon…": choose an existing installation and skip the download.</summary>
    public void ChooseExistingExe()
    {
        using var dialog = new OpenFileDialog
        {
            Title = UiText.ExeDialogTitle,
            Filter = UiText.ExeDialogFilter,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        Settings.ExePath = dialog.FileName;
        SaveSettings();
        ShowStep(WizardStep.Graphics);
    }

    /// <summary>Ends the wizard successfully.</summary>
    public void Finish(WizardOutcome outcome)
    {
        Outcome = outcome;
        Settings.WizardResumeStep = null;
        SaveSettings();
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Path of the settings file, so the optional copy of Wortlaut starts with the same settings.</summary>
    public string SettingsFile => _store.CurrentPath;

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // No browser configured; the address is visible in the README.
        }
    }

    // ----- Closing -----

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // A running download is stopped cleanly first; what was downloaded stays for next time.
        if (_page is { IsBusy: true } page && Outcome == WizardOutcome.Cancelled)
        {
            e.Cancel = true;
            if (_closeWhenStopped)
                return;

            if (page.ConfirmCancelWhileBusy
                && e.CloseReason == CloseReason.UserClosing
                && MessageBox.Show(this, UiText.WizardConfirmCancel, UiText.SetupTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            _closeWhenStopped = true;
            UpdateChrome();
            page.RequestStop();
            return;
        }

        base.OnFormClosing(e);
    }
}
