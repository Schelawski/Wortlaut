using Wortlaut.Core;

namespace Wortlaut.Tests;

public class MediaFilesTests
{
    [Theory]
    [InlineData("a.mp4")]
    [InlineData("a.MP4")]
    [InlineData("a.Mp3")]
    [InlineData("a.ogg")]
    [InlineData("a.m4a")]
    [InlineData("a.mov")]
    [InlineData("a.avi")]
    [InlineData("a.wmv")]
    [InlineData("a.webm")]
    [InlineData("a.mpeg")]
    [InlineData("a.m2p")]
    [InlineData("a.MPG")]
    public void SupportedExtensionsAreRecognizedCaseInsensitively(string fileName)
    {
        Assert.True(MediaFiles.IsSupported(fileName));
    }

    [Theory]
    [InlineData("a.txt")]
    [InlineData("a.json")]
    [InlineData("a.mkv")]
    [InlineData("mp4")]
    public void OtherFilesAreNotSupported(string fileName)
    {
        Assert.False(MediaFiles.IsSupported(fileName));
    }

    [Fact]
    public void FolderListingIsSortedNaturallyAndIgnoresOtherFiles()
    {
        using var folder = new TempFolder();
        folder.CreateFile("Лекция 12.mp4");
        folder.CreateFile("Лекция 2.mp4");
        folder.CreateFile("Лекция 2.txt");
        folder.CreateFile("Kirtan Abend.M4A");
        folder.CreateFile(Path.Combine("Unterordner", "Лекция 1.mp4"));

        var files = MediaFiles.FindInFolder(folder.Path, includeSubfolders: false);

        Assert.Equal(["Kirtan Abend.M4A", "Лекция 2.mp4", "Лекция 12.mp4"], files.Select(Path.GetFileName));
    }

    [Fact]
    public void SubfoldersAreIncludedOnRequestButTempFoldersNever()
    {
        using var folder = new TempFolder();
        folder.CreateFile("b.mp4");
        folder.CreateFile(Path.Combine("a", "c.mp4"));
        folder.CreateFile(Path.Combine("a", "tief", "d.mp3"));
        folder.CreateFile(Path.Combine(".wortlaut-tmp", "x", "job.mp4"));
        folder.CreateFile(Path.Combine("a", ".wortlaut-tmp", "y", "job.mp4"));

        var files = MediaFiles.FindInFolder(folder.Path, includeSubfolders: true);

        Assert.Equal(
            [Path.Combine("a", "c.mp4"), Path.Combine("a", "tief", "d.mp3"), "b.mp4"],
            files.Select(f => Path.GetRelativePath(folder.Path, f)));
    }

    [Fact]
    public void TempFoldersAreFoundForReporting()
    {
        using var folder = new TempFolder();
        folder.CreateFile(Path.Combine(".wortlaut-tmp", "x", "job.mp4"));
        folder.CreateFile(Path.Combine("sub", ".wortlaut-tmp", "y", "job.mp4"));

        Assert.Equal(
            [Path.Combine(folder.Path, ".wortlaut-tmp")],
            MediaFiles.FindTempFolders(folder.Path, includeSubfolders: false));
        Assert.Equal(2, MediaFiles.FindTempFolders(folder.Path, includeSubfolders: true).Count);
    }
}
