using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Draws the window and notification-area icons rather than shipping .ico files.
///
/// The mark is the same indicator lamp the run panel uses: a ring with a filled centre, lit
/// amber while a run is in flight and grey when it is not. That makes the taskbar and tray
/// entries answer "is it running?" without the window being on screen at all.
/// </summary>
internal static class AppIcon
{
    private static Icon? _idle;
    private static Icon? _running;

    public static Icon Idle => _idle ??= Render(running: false);

    public static Icon Running => _running ??= Render(running: true);

    public static Icon For(bool running) => running ? Running : Idle;

    /// <summary>Releases both icons and the unmanaged handles behind them.</summary>
    public static void Dispose()
    {
        Release(ref _idle);
        Release(ref _running);
    }

    private static Icon Render(bool running)
    {
        const int size = 32;

        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            Color lamp = running ? Color.FromArgb(0xF5, 0x9E, 0x0B) : Color.FromArgb(0x8A, 0x8A, 0x93);

            // A grey ring in both states so the silhouette stays recognisable on any taskbar
            // colour; only the centre changes, which is the part that reads as a state.
            using (var ring = new Pen(Color.FromArgb(running ? 200 : 150, lamp), 3.4f))
            {
                g.DrawEllipse(ring, 4.5f, 4.5f, size - 9f, size - 9f);
            }

            using var core = new SolidBrush(lamp);
            g.FillEllipse(core, 11f, 11f, 10f, 10f);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            // Cloned because Icon.FromHandle does not own its handle: the clone survives the
            // DestroyIcon below, which is what keeps this from leaking a GDI object per call.
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void Release(ref Icon? icon)
    {
        icon?.Dispose();
        icon = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
