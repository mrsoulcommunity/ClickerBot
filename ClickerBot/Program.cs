namespace ClickerBot;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Set by StartupManager's registered launch command, so a sign-in autostart goes
        // straight to the tray instead of popping the window open every boot.
        bool startMinimized = Array.Exists(args, arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

        Application.Run(new MainForm(startMinimized));
    }
}
