using Wortlaut.Core;

namespace Wortlaut.Tests;

public class OutputFormatTests
{
    [Theory]
    [InlineData(OutputFormat.Text, ".txt")]
    [InlineData(OutputFormat.Json, ".json")]
    [InlineData(OutputFormat.Srt, ".srt")]
    [InlineData(OutputFormat.Vtt, ".vtt")]
    public void FormatMapsToExtension(OutputFormat format, string expectedExtension)
    {
        Assert.Equal(expectedExtension, format.GetExtension());
    }

    [Theory]
    [InlineData(OutputFormat.Text, "text")]
    [InlineData(OutputFormat.Json, "json")]
    [InlineData(OutputFormat.Srt, "srt")]
    [InlineData(OutputFormat.Vtt, "vtt")]
    public void FormatMapsToCommandLineValue(OutputFormat format, string expectedCliName)
    {
        Assert.Equal(expectedCliName, format.GetCliName());
    }

    [Fact]
    public void EveryFormatIsMappedExactlyOnce()
    {
        var mapped = OutputFormats.All.Select(info => info.Format).ToList();

        Assert.Equal(Enum.GetValues<OutputFormat>().OrderBy(f => f), mapped.OrderBy(f => f));
        Assert.Equal(mapped.Count, mapped.Distinct().Count());
    }

    [Fact]
    public void TextFormatAcceptsBothFileNamesFasterWhisperUses()
    {
        // Faster-Whisper-XXL r239 writes ".text" for --output_format text, older builds ".txt".
        Assert.Equal([".text", ".txt"], OutputFormats.Get(OutputFormat.Text).ProducedExtensions);
    }
}
