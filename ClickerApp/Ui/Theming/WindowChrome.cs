using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ClickerApp;

/// <summary>
/// The parts of the window Windows draws for us: the title bar, and the user's
/// system-wide light/dark preference.
/// </summary>
internal static class WindowChrome
{
    // Renamed in Windows 10 20H1. Older builds answer to 19, newer ones to 20.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;

    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Repaints the title bar to match the active theme. Silently does nothing on builds
    /// that do not support the attribute — a light title bar is a cosmetic loss, not a failure.
    /// </summary>
    public static void ApplyTheme(Form form)
    {
        if (!form.IsHandleCreated)
        {
            return;
        }

        int enabled = Theme.IsDark ? 1 : 0;

        if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref enabled, sizeof(int));
        }

        // The attribute alone does not always repaint an already-visible frame, and
        // Invalidate only covers the client area — a frame change is what redraws the caption.
        if (form.Visible)
        {
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
    }

    /// <summary>
    /// The appearance Windows itself is using, so a first run matches the rest of the desktop.
    /// Falls back to light if the value is missing or unreadable.
    /// </summary>
    public static ThemeMode SystemPreference()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            if (key?.GetValue("AppsUseLightTheme") is int useLight)
            {
                return useLight == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // Not worth surfacing: light is a safe default.
        }

        return ThemeMode.Light;
    }

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
}
