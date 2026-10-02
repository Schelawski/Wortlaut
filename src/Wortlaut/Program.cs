using System.Globalization;
using Wortlaut.Core;
using Wortlaut.UI;

namespace Wortlaut;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Applies the settings from the project file (PerMonitorV2 high DPI, visual styles, default font).
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(UiText.UnexpectedError(e.Exception.Message), UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Remember the Windows display language before Wortlaut changes the UI culture.
        var windowsCulture = CultureInfo.CurrentUICulture;
        var settingsStore = new SettingsStore();
        var settings = settingsStore.Load();
        MainForm.WindowLayout? layout = null;

        // Switching the UI language closes the window; it is then rebuilt with the new texts.
        while (true)
        {
            UiLanguages.Apply(UiLanguages.Resolve(settings.UiLanguage, windowsCulture));

            using var form = new MainForm(settingsStore, settings, layout);
            Application.Run(form);

            if (!form.LanguageChangeRequested)
                break;
            layout = form.LastLayout;
        }
    }
}
