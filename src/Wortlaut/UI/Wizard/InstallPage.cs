using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>
/// What will be downloaded (version, size, duration, space, target, license), then download, extract and
/// test-start Faster-Whisper-XXL. An interrupted download continues where it stopped.
/// </summary>
internal sealed class InstallPage : WizardPage
{
    private const string ProjectUrl = "https://github.com/Purfview/whisper-standalone-win";

    private readonly FasterWhisperReleaseFinder _finder = new(AppHttp.Client);
    private readonly FasterWhisperInstaller _installer = new(
        new ResumableDownloader(AppHttp.Client), new SevenZipExtractor(), new FasterWhisperProbe());

    private readonly Label _versionValue = ValueLabel();
    private readonly Label _downloadValue = ValueLabel();
    private readonly Label _durationValue = ValueLabel();
    private readonly Label _spaceValue = ValueLabel();
    private readonly ProgressArea _progress = new();

    private FasterWhisperPackage? _package;
    private CancellationTokenSource? _cancellation;
    private bool _failed;

    public InstallPage(SetupWizard wizard)
        : base(wizard)
    {
        var stack = Stack();
        Add(stack, Paragraph(UiText.SetupIntro));

        var details = DetailsTable();
        AddDetail(details, UiText.SetupVersionLabel, _versionValue);
        AddDetail(details, UiText.SetupDownloadLabel, _downloadValue);
        AddDetail(details, UiText.SetupDurationLabel, _durationValue);
        AddDetail(details, UiText.SetupSpaceLabel, _spaceValue);
        AddDetail(details, UiText.SetupTargetLabel, ValueLabel(FasterWhisperInstaller.InstallDirectory(wizard.Root)));
        var license = new LinkLabel { Text = UiText.SetupLicenseLink, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 3, 4) };
        license.LinkClicked += (_, _) => SetupWizard.OpenUrl(ProjectUrl);
        AddDetail(details, UiText.SetupLicenseLabel, license);
        Add(stack, details);

        _progress.AddTo(stack);
        Controls.Add(stack);
    }

    public override WizardStep Step => WizardStep.Install;

    public override string Title => UiText.InstallTitle;

    public override string PrimaryText => _failed ? UiText.SetupRetry : UiText.SetUp;

    public override bool PrimaryEnabled => _package is not null && !IsBusy;

    public override string? SecondaryText => UiText.WizardHaveExe;

    public override bool IsBusy => _cancellation is not null;

    public override async Task OnShownAsync()
    {
        _progress.Status.Text = UiText.SetupLookingUp;
        _progress.Bar.Style = ProgressBarStyle.Marquee;

        // Falls back to a known version when GitHub cannot be reached.
        var (package, isFallback) = await _finder.FindLatestAsync(CancellationToken.None);
        if (IsDisposed)
            return;

        _package = package;
        _versionValue.Text = UiText.SetupVersion(package.Version, isFallback);
        _downloadValue.Text = UiText.FormatSize(package.Size);
        var (fast, slow) = SetupWizardFlow.EstimateDuration(package.Size, includesExtracting: true);
        _durationValue.Text = UiText.SetupDuration(fast, slow);
        _spaceValue.Text = UiText.SetupSpace(
            FasterWhisperInstaller.RequiredFreeSpace(Wizard.Root, package),
            FasterWhisperInstaller.AvailableFreeSpace(Wizard.Root));
        _progress.Reset(string.Empty);
        NotifyChanged();
    }

    public override async Task OnPrimaryAsync()
    {
        if (_package is not { } package || IsBusy)
            return;

        _progress.Message.Visible = false;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        NotifyChanged();

        string? exePath = null;
        try
        {
            exePath = await _installer.InstallAsync(package, Wizard.Root, new Progress<SetupProgress>(OnProgress), cancellation.Token);
            _failed = false;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _progress.Reset(UiText.StateCancelled);
        }
        catch (SetupException ex)
        {
            _failed = true;
            var required = FasterWhisperInstaller.RequiredFreeSpace(Wizard.Root, package);
            var text = UiText.SetupErrorText(ex.Error, FasterWhisperInstaller.InstallDirectory(Wizard.Root), required, FasterWhisperInstaller.AvailableFreeSpace(Wizard.Root));
            if (!string.IsNullOrWhiteSpace(ex.Detail) && ex.Error is SetupError.DownloadFailed or SetupError.ExtractFailed)
                text += Environment.NewLine + UiText.SetupDetail(ex.Detail);
            _progress.Reset(string.Empty);
            _progress.ShowMessage(text, UiStyle.Danger.Fore);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _failed = true;
            _progress.Reset(string.Empty);
            _progress.ShowMessage(UiText.UnexpectedError(ex.Message), UiStyle.Danger.Fore);
        }
        finally
        {
            _cancellation = null;
        }

        if (exePath is not null)
        {
            Wizard.Settings.ExePath = exePath;
            Wizard.SaveSettings();
        }

        NotifyChanged(); // closes the wizard if it waited for the stop
        if (exePath is not null && !IsDisposed)
            Wizard.GoNext();
    }

    public override Task OnSecondaryAsync()
    {
        Wizard.ChooseExistingExe();
        return Task.CompletedTask;
    }

    public override void RequestStop()
    {
        _progress.Status.Text = UiText.SetupCancelling;
        _cancellation?.Cancel();
    }

    private void OnProgress(SetupProgress progress)
    {
        if (IsDisposed)
            return;

        _progress.Status.Text = progress.Stage switch
        {
            SetupStage.Downloading => UiText.SetupDownloading(progress.Done, progress.Total, progress.BytesPerSecond, progress.Remaining),
            SetupStage.Extracting => UiText.SetupExtracting(progress.Fraction),
            _ => UiText.SetupVerifying,
        };
        _progress.SetFraction(progress.Fraction);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancellation?.Cancel();
        base.Dispose(disposing);
    }
}
