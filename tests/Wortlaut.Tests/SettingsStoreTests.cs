using Wortlaut.Core;

namespace Wortlaut.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void MissingFileYieldsDefaults()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(Path.Combine(folder.Path, "exe"), Path.Combine(folder.Path, "appdata"));

        var settings = store.Load();

        Assert.Equal("large-v2", settings.Model);
        Assert.Equal("cuda", settings.Device);
        Assert.Equal("ru", settings.Language);
        Assert.Equal(OutputFormat.Text, settings.Format);
        Assert.True(settings.SkipExisting);
        Assert.False(settings.IncludeSubfolders);
        Assert.Equal(string.Empty, settings.ExePath);
    }

    [Fact]
    public void SettingsRoundTripNextToExecutable()
    {
        using var folder = new TempFolder();
        var exeDirectory = Path.Combine(folder.Path, "exe");
        Directory.CreateDirectory(exeDirectory);
        var store = new SettingsStore(exeDirectory, Path.Combine(folder.Path, "appdata"));
        var saved = new AppSettings
        {
            ExePath = @"C:\Tools\Faster-Whisper-XXL\faster-whisper-xxl.exe",
            Model = "large-v3-turbo",
            Device = "cpu",
            Language = "auto",
            Format = OutputFormat.Srt,
            WholeSentences = false,
            LastFile = @"D:\Videos\Лекция 12 — Медитация.mp4",
            LastFolder = @"D:\Videos\Satsang 2026-09",
            IncludeSubfolders = true,
            SkipExisting = false,
            UiLanguage = "ru",
        };

        store.Save(saved);
        var loaded = new SettingsStore(exeDirectory, Path.Combine(folder.Path, "appdata")).Load();

        var file = Path.Combine(exeDirectory, "Wortlaut.settings.json");
        Assert.Equal(file, store.CurrentPath);
        Assert.False(store.UsesFallback);
        Assert.Contains("Лекция 12", File.ReadAllText(file)); // readable, not \u-escaped
        Assert.Contains("\"Srt\"", File.ReadAllText(file));
        Assert.Equivalent(saved, loaded);
    }

    [Fact]
    public void UnwritableExecutableFolderFallsBackToAppData()
    {
        using var folder = new TempFolder();
        // A file where the folder should be makes every write to the primary location fail.
        var blockedDirectory = folder.CreateFile("blocked", "not a folder");
        var appData = Path.Combine(folder.Path, "appdata", "Wortlaut");
        var store = new SettingsStore(blockedDirectory, appData);

        store.Save(new AppSettings { Model = "medium" });

        Assert.True(store.UsesFallback);
        Assert.Equal(Path.Combine(appData, "Wortlaut.settings.json"), store.CurrentPath);
        Assert.Equal("medium", new SettingsStore(blockedDirectory, appData).Load().Model);
    }

    [Fact]
    public void CorruptFileYieldsDefaults()
    {
        using var folder = new TempFolder();
        folder.CreateFile("Wortlaut.settings.json", "{ this is not json");
        var store = new SettingsStore(folder.Path, Path.Combine(folder.Path, "appdata"));

        var settings = store.Load();

        Assert.Equal("large-v2", settings.Model);
    }

    [Fact]
    public void IncompleteFileIsFilledWithDefaults()
    {
        using var folder = new TempFolder();
        folder.CreateFile("Wortlaut.settings.json", """{ "Model": "", "Device": null, "Format": "Json" }""");
        var store = new SettingsStore(folder.Path, Path.Combine(folder.Path, "appdata"));

        var settings = store.Load();

        Assert.Equal("large-v2", settings.Model);
        Assert.Equal("cuda", settings.Device);
        Assert.Equal(OutputFormat.Json, settings.Format);
    }

    [Fact]
    public void OlderFileWithoutWholeSentencesTurnsThemOn()
    {
        using var folder = new TempFolder();
        folder.CreateFile("Wortlaut.settings.json", """{ "Model": "large-v2", "Format": "Text", "SkipExisting": true }""");
        var store = new SettingsStore(folder.Path, Path.Combine(folder.Path, "appdata"));

        var settings = store.Load();
        Assert.True(settings.WholeSentences);
        Assert.Equal(string.Empty, settings.UiLanguage); // not chosen yet: Windows decides
    }

    [Fact]
    public void NewerFileWinsWhenBothLocationsHaveOne()
    {
        using var folder = new TempFolder();
        var primary = folder.CreateFile(Path.Combine("exe", "Wortlaut.settings.json"), """{ "Model": "small" }""");
        var fallback = folder.CreateFile(Path.Combine("appdata", "Wortlaut.settings.json"), """{ "Model": "medium" }""");
        File.SetLastWriteTimeUtc(primary, DateTime.UtcNow.AddDays(-1));
        File.SetLastWriteTimeUtc(fallback, DateTime.UtcNow);
        var store = new SettingsStore(Path.GetDirectoryName(primary)!, Path.GetDirectoryName(fallback)!);

        Assert.Equal("medium", store.Load().Model);
        Assert.True(store.UsesFallback);
    }

    [Fact]
    public void ToWhisperSettingsTrimsValues()
    {
        var settings = new AppSettings { ExePath = @" C:\fw\faster-whisper-xxl.exe ", Model = " large-v3 ", Language = " de ", Format = OutputFormat.Vtt, WholeSentences = false };

        var whisper = settings.ToWhisperSettings();

        Assert.Equal(@"C:\fw\faster-whisper-xxl.exe", whisper.ExePath);
        Assert.Equal("large-v3", whisper.Model);
        Assert.Equal("de", whisper.Language);
        Assert.Equal(OutputFormat.Vtt, whisper.Format);
        Assert.False(whisper.WholeSentences);
    }

    [Fact]
    public void ExecutableIsFoundInFolderOrArchiveSubfolder()
    {
        using var first = new TempFolder();
        using var second = new TempFolder();
        var expected = second.CreateFile(Path.Combine("Faster-Whisper-XXL", "faster-whisper-xxl.exe"), "exe");

        Assert.Equal(expected, FasterWhisperLocator.Find([first.Path, second.Path]));
        Assert.Null(FasterWhisperLocator.Find([first.Path]));
    }

    [Fact]
    public void ExecutableDirectlyInFolderIsPreferred()
    {
        using var folder = new TempFolder();
        var direct = folder.CreateFile("faster-whisper-xxl.exe", "exe");
        folder.CreateFile(Path.Combine("Faster-Whisper-XXL", "faster-whisper-xxl.exe"), "exe");

        Assert.Equal(direct, FasterWhisperLocator.Find([folder.Path]));
    }
}
