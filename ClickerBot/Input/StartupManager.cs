using Microsoft.Win32;

namespace ClickerBot;

/// <summary>
/// Toggles whether ClickerBot launches when you sign in, via the per-user "Run" registry key
/// — the same mechanism Windows itself uses for startup apps, so no installer or scheduled
/// task is needed.
///
/// Deliberately stateless: the registry is the only source of truth. A checkbox mirroring a
/// separate stored flag could drift from what is actually registered; reading the key itself
/// on every check cannot.
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClickerBot";

    /// <summary>
    /// True when the Run key holds exactly the command this build would register — a value
    /// left over from a different install path counts as "not enabled" rather than a match.
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string existing &&
                string.Equals(existing, CommandLine(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Adds or removes the Run-key entry. Throws on failure rather than swallowing it, since a
    /// checkbox that silently didn't take effect is worse than one that visibly refused.
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(ValueName, CommandLine());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    // Launched with --minimized so signing in does not pop the window open on its own; see
    // Program.cs and MainForm's SetVisibleCore override.
    private static string CommandLine() => $"\"{Application.ExecutablePath}\" --minimized";
}
