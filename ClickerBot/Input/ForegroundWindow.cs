using System.Runtime.InteropServices;
using System.Text;

namespace ClickerBot;

/// <summary>Reads which window is currently in front, for a macro that is restricted to one.</summary>
internal static class ForegroundWindow
{
    /// <summary>The title of whatever window currently has focus, or empty if none does.</summary>
    public static string Title()
    {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        int length = GetWindowTextLength(handle);
        if (length == 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        GetWindowText(handle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    /// <summary>
    /// Whether the foreground window's title contains <paramref name="titleSubstring"/>,
    /// case-insensitively. An empty target never matches — a profile with the toggle on but
    /// nothing typed in yet should hold, not run against whatever happens to be focused.
    /// </summary>
    public static bool Matches(string titleSubstring) =>
        !string.IsNullOrWhiteSpace(titleSubstring) &&
        Title().Contains(titleSubstring, StringComparison.OrdinalIgnoreCase);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}
