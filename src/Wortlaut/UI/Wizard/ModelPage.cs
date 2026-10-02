using Wortlaut.Core.Models;
using Wortlaut.Core.Setup;

namespace Wortlaut.UI.Wizard;

/// <summary>Downloads the selected model with progress; can be postponed with "Später".</summary>
internal sealed class ModelPage : WizardPage
{
    private readonly ModelDownloader _downloader = new(AppHttp.Client, new ResumableDownloader(AppHttp.Client));
    private readonly WhisperModelInfo? _model;
    private readonly string _modelsDirectory;
    private readonly ProgressArea _progress = new();

    private CancellationTokenSource? _cancellation;
    private bool _failed;

    public ModelPage(SetupWizard wizard)
        : base(wizard)
    {
        _model = WhisperModels.Find(wizard.Settings.Model);
        _modelsDirectory = WhisperModels.ModelsDirectory(wizard.Settings.ExePath);

        var stack = Stack();
        var name = _model?.Name ?? wizard.Settings.Model;
        Add(stack, Paragraph(UiText.ModelPageIntro(name)));

        if (_model is { } model)
        {
            var details = DetailsTable();
            AddDetail(details, UiText.ModelPageNameLabel, ValueLabel($"{model.Name} ({UiText.ModelHint(model.Name)})"));
            AddDetail(details, UiText.SetupDownloadLabel, ValueLabel(UiText.FormatSize(model.ApproximateSize)));
            var (fast, slow) = SetupWizardFlow.EstimateDuration(model.ApproximateSize, includesExtracting: false);
            AddDetail(details, UiText.SetupDurationLabel, ValueLabel(UiText.SetupDuration(fast, slow)));
            AddDetail(details, UiText.SetupSpaceLabel, ValueLabel(UiText.SetupSpace(model.ApproximateSize, FasterWhisperInstaller.AvailableFreeSpace(_modelsDirectory))));
            AddDetail(details, UiText.SetupTargetLabel, ValueLabel(_modelsDirectory));
            Add(stack, details);
        }

        Add(stack, Paragraph(UiText.ModelPageLaterHint, new Padding(3, 8, 3, 0)));
        _progress.AddTo(stack);
        Controls.Add(stack);

        if (IsInstalled)
        {
            _progress.Bar.Visible = false;
            _progress.ShowMessage(UiText.ModelPageInstalled(name), UiStyle.Success.Fore);
        }
    }

    public override WizardStep Step => WizardStep.Model;

    public override string Title => UiText.ModelPageTitle;

    private bool IsInstalled => _model is null || WhisperModels.IsInstalled(_modelsDirectory, _model.Name);

    public override string PrimaryText =>
        IsInstalled ? UiText.WizardNext : _failed ? UiText.SetupRetry : UiText.ModelDownload;

    public override string? SecondaryText => IsInstalled ? null : UiText.ModelLater;

    public override bool IsBusy => _cancellation is not null;

    public override async Task OnPrimaryAsync()
    {
        if (IsInstalled)
        {
            Wizard.GoNext();
            return;
        }

        if (_model is not { } model || IsBusy)
            return;

        _progress.Message.Visible = false;
        _progress.Status.Text = UiText.SetupLookingUp;
        _progress.Bar.Style = ProgressBarStyle.Marquee;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        NotifyChanged();

        var done = false;
        try
        {
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (IsDisposed)
                    return;
                _progress.Status.Text = UiText.SetupDownloading(p.Received, p.Total, p.BytesPerSecond, p.Remaining);
                _progress.SetFraction(p.Fraction);
            });
            await _downloader.DownloadAsync(model, _modelsDirectory, progress, cancellation.Token);
            done = true;
            _failed = false;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _progress.Reset(UiText.StateCancelled);
        }
        catch (SetupException ex)
        {
            _failed = true;
            var text = UiText.SetupErrorText(ex.Error, _modelsDirectory, model.ApproximateSize, FasterWhisperInstaller.AvailableFreeSpace(_modelsDirectory));
            if (!string.IsNullOrWhiteSpace(ex.Detail) && ex.Error == SetupError.DownloadFailed)
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

        NotifyChanged(); // closes the wizard if it waited for the stop
        if (done && !IsDisposed)
            Wizard.GoNext();
    }

    /// <summary>"Später": the model is offered again before the first transcription.</summary>
    public override Task OnSecondaryAsync()
    {
        Wizard.GoNext();
        return Task.CompletedTask;
    }

    public override void RequestStop()
    {
        _progress.Status.Text = UiText.SetupCancelling;
        _cancellation?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancellation?.Cancel();
        base.Dispose(disposing);
    }
}
