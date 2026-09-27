using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wortlaut.Core;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as <c>Wortlaut.settings.json</c> next to the executable.
/// Falls back to <c>%APPDATA%\Wortlaut\</c> when the executable's folder is not writable.
/// </summary>
public sealed class SettingsStore
{
    public const string FileName = "Wortlaut.settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // Keep Cyrillic paths readable in the file.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _primaryPath;
    private readonly string _fallbackPath;
    private bool _primaryWritable = true;

    /// <summary>Uses the executable's folder and <c>%APPDATA%\Wortlaut</c>.</summary>
    public SettingsStore()
        : this(
            AppContext.BaseDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wortlaut"))
    {
    }

    public SettingsStore(string primaryDirectory, string fallbackDirectory)
    {
        _primaryPath = Path.Combine(primaryDirectory, FileName);
        _fallbackPath = Path.Combine(fallbackDirectory, FileName);
        CurrentPath = _primaryPath;
    }

    /// <summary>Where the settings were last loaded from or saved to.</summary>
    public string CurrentPath { get; private set; }

    /// <summary>True when the settings live in the <c>%APPDATA%</c> fallback folder.</summary>
    public bool UsesFallback => string.Equals(CurrentPath, _fallbackPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loads the settings. Returns defaults if there is no file or it cannot be read.
    /// If both locations have a file, the newer one wins.
    /// </summary>
    public AppSettings Load()
    {
        var candidates = new[] { _primaryPath, _fallbackPath }
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc);

        foreach (var path in candidates)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions);
                if (settings is null)
                    continue;

                settings.Normalize();
                CurrentPath = path;
                return settings;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // Unreadable or corrupt file: try the other location, otherwise use defaults.
            }
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves the settings next to the executable, or in the fallback folder if that fails.
    /// </summary>
    /// <exception cref="IOException">Neither location is writable.</exception>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var json = JsonSerializer.Serialize(settings, JsonOptions);

        if (_primaryWritable)
        {
            try
            {
                WriteAtomically(_primaryPath, json);
                CurrentPath = _primaryPath;
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // E.g. the exe lives in "C:\Program Files": use %APPDATA% from now on.
                _primaryWritable = false;
            }
        }

        try
        {
            WriteAtomically(_fallbackPath, json);
            CurrentPath = _fallbackPath;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException(ex.Message, ex);
        }
    }

    /// <summary>Writes to a temporary file first, so a crash never leaves a half-written settings file.</summary>
    private static void WriteAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }
}
