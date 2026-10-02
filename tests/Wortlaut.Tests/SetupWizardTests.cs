using Wortlaut.Core;
using Wortlaut.Core.Setup;

namespace Wortlaut.Tests;

public class SetupWizardTests
{
    // ----- When the wizard appears -----

    [Theory]
    [InlineData(null, false, true)]                 // first start, nothing installed
    [InlineData(null, true, false)]                 // Faster-Whisper-XXL already there: straight to the main window
    [InlineData(WizardStep.Graphics, true, true)]   // closed early last time: continue
    [InlineData(WizardStep.Install, false, true)]
    [InlineData(WizardStep.Model, true, true)]
    [InlineData(WizardStep.Welcome, true, false)]   // closed early, then the program was chosen in the main window
    [InlineData(WizardStep.Install, true, false)]
    public void WizardAppearsOnFirstStartAndToContinue(WizardStep? resume, bool exeFound, bool expected)
    {
        Assert.Equal(expected, SetupWizardFlow.ShouldShowAtStartup(resume, exeFound));
    }

    [Theory]
    [InlineData(null, false, WizardStep.Welcome)]
    [InlineData(WizardStep.Welcome, false, WizardStep.Welcome)]
    [InlineData(WizardStep.Install, false, WizardStep.Install)]   // the download continues
    [InlineData(WizardStep.Graphics, true, WizardStep.Graphics)]
    [InlineData(WizardStep.Model, true, WizardStep.Model)]
    [InlineData(WizardStep.Model, false, WizardStep.Install)]    // installation was deleted in the meantime
    [InlineData(WizardStep.Graphics, false, WizardStep.Install)]
    [InlineData(WizardStep.Done, true, WizardStep.Done)]
    public void WizardContinuesWhereItStopped(WizardStep? resume, bool exeFound, WizardStep expected)
    {
        Assert.Equal(expected, SetupWizardFlow.StartStep(resume, exeFound));
    }

    // ----- Navigation -----

    [Fact]
    public void FullRunVisitsEveryPage()
    {
        Assert.Equal(WizardStep.Install, SetupWizardFlow.Next(WizardStep.Welcome, exeFound: false, modelInstalled: false));
        Assert.Equal(WizardStep.Graphics, SetupWizardFlow.Next(WizardStep.Install, exeFound: true, modelInstalled: false));
        Assert.Equal(WizardStep.Model, SetupWizardFlow.Next(WizardStep.Graphics, exeFound: true, modelInstalled: false));
        Assert.Equal(WizardStep.Done, SetupWizardFlow.Next(WizardStep.Model, exeFound: true, modelInstalled: true));
    }

    [Fact]
    public void ExistingInstallationSkipsTheDownload()
    {
        Assert.Equal(WizardStep.Graphics, SetupWizardFlow.Next(WizardStep.Welcome, exeFound: true, modelInstalled: false));
        Assert.Equal(WizardStep.Welcome, SetupWizardFlow.Previous(WizardStep.Graphics, exeFound: true, modelInstalled: false));
    }

    [Fact]
    public void ExistingModelSkipsTheModelPage()
    {
        Assert.Equal(WizardStep.Done, SetupWizardFlow.Next(WizardStep.Graphics, exeFound: true, modelInstalled: true));
        Assert.Equal(WizardStep.Graphics, SetupWizardFlow.Previous(WizardStep.Done, exeFound: true, modelInstalled: true));
    }

    [Fact]
    public void BackGoesToThePreviousPage()
    {
        Assert.Equal(WizardStep.Welcome, SetupWizardFlow.Previous(WizardStep.Install, exeFound: false, modelInstalled: false));
        Assert.Equal(WizardStep.Install, SetupWizardFlow.Previous(WizardStep.Graphics, exeFound: false, modelInstalled: false));
        Assert.Equal(WizardStep.Graphics, SetupWizardFlow.Previous(WizardStep.Model, exeFound: true, modelInstalled: false));
        Assert.Equal(WizardStep.Model, SetupWizardFlow.Previous(WizardStep.Done, exeFound: true, modelInstalled: false));
    }

    [Fact]
    public void StepCountMatchesThePages()
    {
        Assert.Equal(5, SetupWizardFlow.StepCount);
    }

    // ----- Duration estimate -----

    [Fact]
    public void FasterWhisperDownloadTakesAFewToFifteenMinutes()
    {
        // r245.4: 1.36 GB
        var (fast, slow) = SetupWizardFlow.EstimateDuration(1_360_000_000, includesExtracting: true);

        Assert.Equal(TimeSpan.FromMinutes(3), fast);  // 109 s download + 1 min extracting
        Assert.Equal(TimeSpan.FromMinutes(14), slow); // 680 s download + 2 min extracting
    }

    [Fact]
    public void SmallDownloadTakesAtLeastOneMinute()
    {
        var (fast, slow) = SetupWizardFlow.EstimateDuration(10_000_000, includesExtracting: false);

        Assert.Equal(TimeSpan.FromMinutes(1), fast);
        Assert.Equal(TimeSpan.FromMinutes(1), slow);
    }

    // ----- Settings -----

    [Fact]
    public void ResumeStepIsSavedAndLoaded()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(folder.Path, Path.Combine(folder.Path, "fallback"));

        store.Save(new AppSettings { WizardResumeStep = WizardStep.Model });

        Assert.Equal(WizardStep.Model, store.Load().WizardResumeStep);
        Assert.Contains("\"WizardResumeStep\": \"Model\"", File.ReadAllText(store.CurrentPath));
    }

    [Fact]
    public void OlderSettingsFilesHaveNoResumeStep()
    {
        using var folder = new TempFolder();
        folder.CreateFile(SettingsStore.FileName, "{ \"ExePath\": \"C:\\\\fw\\\\faster-whisper-xxl.exe\", \"Model\": \"large-v2\" }");

        var settings = new SettingsStore(folder.Path, Path.Combine(folder.Path, "fallback")).Load();

        Assert.Null(settings.WizardResumeStep);
    }

    [Fact]
    public void UnknownResumeStepIsIgnored()
    {
        using var folder = new TempFolder();
        folder.CreateFile(SettingsStore.FileName, "{ \"WizardResumeStep\": 42 }");

        var settings = new SettingsStore(folder.Path, Path.Combine(folder.Path, "fallback")).Load();

        Assert.Null(settings.WizardResumeStep);
    }

    // ----- Copy to the user folder -----

    [Theory]
    [InlineData(@"C:\Users\Anna\Downloads\Wortlaut.exe", true)]
    [InlineData(@"C:\Users\Anna\Downloads\Wortlaut-1.0\Wortlaut.exe", true)]
    [InlineData(@"C:\Users\Anna\Downloads2\Wortlaut.exe", false)] // only the folder itself and its subfolders
    [InlineData(@"D:\Programme\Wortlaut\Wortlaut.exe", false)]
    public void DownloadsFolderIsATemporaryLocation(string processPath, bool expected)
    {
        Assert.Equal(expected, LocalInstall.IsTemporaryLocation(processPath, [@"C:\Users\Anna\Downloads\", @"C:\Users\Anna\Desktop"]));
    }

    [Theory]
    [InlineData(@"C:\Users\Anna\Downloads\Wortlaut.exe", true)]
    [InlineData(@"C:\Users\Anna\AppData\Local\Wortlaut\Wortlaut.exe", false)] // already there
    [InlineData(@"C:\Projekte\Wortlaut\bin\Debug\harness.exe", false)]        // not the published app
    [InlineData(null, false)]
    public void CopyIsOfferedForThePublishedAppOutsideTheUserFolder(string? processPath, bool expected)
    {
        Assert.Equal(expected, LocalInstall.CanOffer(processPath, @"C:\Users\Anna\AppData\Local\Wortlaut"));
    }

    [Fact]
    public void CopyTakesTheExeAndTheSettings()
    {
        using var folder = new TempFolder();
        var exe = folder.CreateFile(@"Загрузки\Wortlaut.exe", "MZ fake exe");
        var settings = folder.CreateFile(@"Загрузки\" + SettingsStore.FileName, "{ \"Model\": \"large-v3-turbo\" }");
        var root = Path.Combine(folder.Path, "Local", "Wortlaut");

        var copy = LocalInstall.Copy(exe, root, settings);

        Assert.Equal(Path.Combine(root, "Wortlaut.exe"), copy);
        Assert.Equal("MZ fake exe", File.ReadAllText(copy));
        Assert.Equal("{ \"Model\": \"large-v3-turbo\" }", File.ReadAllText(Path.Combine(root, SettingsStore.FileName)));
        Assert.True(File.Exists(exe)); // the original stays where it is
    }

    [Fact]
    public void ShortcutPointsToTheExe()
    {
        using var folder = new TempFolder();
        var exe = folder.CreateFile(@"Программа\Wortlaut.exe", "MZ fake exe");
        var shortcut = Path.Combine(folder.Path, "Ярлыки", "Wortlaut.lnk");

        LocalInstall.CreateShortcut(shortcut, exe);

        Assert.True(File.Exists(shortcut));
        // A .lnk file stores the target path as UTF-16 (Cyrillic included).
        var content = System.Text.Encoding.Unicode.GetString(File.ReadAllBytes(shortcut));
        Assert.Contains(exe, content);
    }
}
