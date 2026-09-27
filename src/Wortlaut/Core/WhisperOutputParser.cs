using System.Globalization;
using System.Text.RegularExpressions;

namespace Wortlaut.Core;

/// <summary>
/// Extracts progress information from faster-whisper console output.
/// </summary>
public static partial class WhisperOutputParser
{
    // "mm:ss.fff" or "hh:mm:ss.fff"
    private const string TimePattern = @"\d{1,3}(?::\d{2}){1,2}\.\d{1,3}";

    /// <summary>Segment line, e.g. <c>[00:03.120 --> 00:08.240]  text</c> or <c>[01:02:03.456 --> 01:02:05.000]</c>.</summary>
    [GeneratedRegex(@"^\s*\[(?<start>" + TimePattern + @")\s*-->\s*(?<end>" + TimePattern + @")\]")]
    private static partial Regex SegmentRegex();

    /// <summary>Printed with <c>--verbose True</c>: <c>Processing audio with duration 00:25.008</c>.</summary>
    [GeneratedRegex(@"Processing audio with duration\s+(?<duration>" + TimePattern + ")")]
    private static partial Regex DurationRegex();

    /// <summary>
    /// Printed after all output files are written:
    /// <c>Subtitles are written to '…' directory.</c> and <c>Operation finished in:  0:08:32.858</c>.
    /// </summary>
    [GeneratedRegex(@"^\s*(Subtitles are written to\b|Operation finished in:)")]
    private static partial Regex CompletionRegex();

    /// <summary>True for the lines faster-whisper prints once all output files are written.</summary>
    public static bool IsCompletionLine(string? line) =>
        !string.IsNullOrEmpty(line) && CompletionRegex().IsMatch(line);

    /// <summary>Tries to read the end time of a segment line.</summary>
    public static bool TryParseSegmentEnd(string? line, out TimeSpan end)
    {
        end = default;
        if (string.IsNullOrEmpty(line))
            return false;

        var match = SegmentRegex().Match(line);
        return match.Success && TryParseTimestamp(match.Groups["end"].Value, out end);
    }

    /// <summary>Tries to read the total audio duration.</summary>
    public static bool TryParseDuration(string? line, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrEmpty(line))
            return false;

        var match = DurationRegex().Match(line);
        return match.Success && TryParseTimestamp(match.Groups["duration"].Value, out duration);
    }

    /// <summary>Parses "mm:ss.fff" and "hh:mm:ss.fff".</summary>
    public static bool TryParseTimestamp(string value, out TimeSpan time)
    {
        time = default;
        var parts = value.Split(':');
        if (parts.Length is < 2 or > 3)
            return false;

        if (!double.TryParse(parts[^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
            return false;

        var minutesIndex = parts.Length - 2;
        if (!int.TryParse(parts[minutesIndex], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
            return false;

        var hours = 0;
        if (parts.Length == 3 && !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours))
            return false;

        time = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        return true;
    }
}
