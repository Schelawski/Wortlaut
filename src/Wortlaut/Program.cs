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
        Application.Run(new Form { Text = "Wortlaut" });
    }
}
