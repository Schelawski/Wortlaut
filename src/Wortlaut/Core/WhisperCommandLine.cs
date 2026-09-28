namespace Wortlaut.Core;

/// <summary>
/// Builds the faster-whisper argument list. The list is handed to
/// <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> as is, so no quoting is needed.
/// </summary>
public static class WhisperCommandLine
{
    public static IReadOnlyList<string> BuildArguments(WhisperSettings settings, string inputPath, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var arguments = new List<string>
        {
            "--model", settings.Model.Trim(),
            "--device", settings.Device.Trim(),
            "--output_format", settings.Format.GetCliName(),
        };

        if (!settings.IsAutoLanguage)
        {
            arguments.Add("--language");
            arguments.Add(settings.Language.Trim());
        }

        if (settings.WholeSentences)
            arguments.Add("--sentence");

        arguments.Add("--output_dir");
        arguments.Add(outputDirectory);

        // --output_format accepts several values, so the input file must not follow it directly.
        arguments.Add(inputPath);
        return arguments;
    }

    /// <summary>
    /// Renders a command line for the log. Only for display; the process is never started from this string.
    /// </summary>
    public static string ToDisplayString(string exePath, IEnumerable<string> arguments) =>
        string.Join(' ', new[] { exePath }.Concat(arguments).Select(Quote));

    private static string Quote(string value) =>
        value.Length == 0 || value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
}
