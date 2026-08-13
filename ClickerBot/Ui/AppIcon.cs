using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Draws the window and notification-area icons rather than shipping a second copy of the
/// logo as a raster asset.
///
/// The mark is four corner brackets — a capture reticle, echoing the app's own Pick-point
/// feature — around one indicator dot. The reticle never changes; only the dot does, lit
/// amber while a run is in flight and dim grey when it is not. That makes the taskbar and
/// tray entries answer "is it running?" without the window being on screen at all. It is the
/// same mark baked into <c>Assets/AppIcon.ico</c> for the compiled .exe's own file icon —
/// that copy is fixed at the idle state, since nothing is running before the process starts.
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

            DrawTile(g, size);
            DrawReticle(g, size);
            DrawLamp(g, size, running);
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

    private static void DrawTile(Graphics g, int size)
    {
        var bounds = new Rectangle(0, 0, size - 1, size - 1);
        using var path = Theme.RoundedRect(bounds, (int)Math.Round(size * 0.195));
        using var fill = new LinearGradientBrush(
            bounds, Color.FromArgb(11, 11, 13), Color.FromArgb(23, 23, 26), LinearGradientMode.Vertical);
        g.FillPath(fill, path);
    }

    private static void DrawReticle(Graphics g, int size)
    {
        float inset = size * 0.205f;
        float arm = size * 0.150f;
        float stroke = MathF.Max(1.4f, size * 0.066f);
        var color = Color.FromArgb(232, 232, 235);

        using var brush = new SolidBrush(color);
        DrawCorner(g, brush, inset, inset, arm, stroke, dx: 1, dy: 1);
        DrawCorner(g, brush, size - inset, inset, arm, stroke, dx: -1, dy: 1);
        DrawCorner(g, brush, inset, size - inset, arm, stroke, dx: 1, dy: -1);
        DrawCorner(g, brush, size - inset, size - inset, arm, stroke, dx: -1, dy: -1);
    }

    /// <summary>
    /// One L-shaped bracket, its outer corner at (x, y). (dx, dy) is the direction the two
    /// arms point away from that corner, so the same call shape works for all four corners.
    /// </summary>
    private static void DrawCorner(Graphics g, Brush brush, float x, float y, float arm, float stroke, int dx, int dy)
    {
        g.FillRectangle(brush, Math.Min(x, x + (dx * arm)), Math.Min(y, y + (dy * stroke)), arm, stroke);
        g.FillRectangle(brush, Math.Min(x, x + (dx * stroke)), Math.Min(y, y + (dy * arm)), stroke, arm);
    }

    private static void DrawLamp(Graphics g, int size, bool running)
    {
        float cx = size / 2f;
        float cy = size / 2f;
        float dotR = size * 0.098f;

        if (running)
        {
            float haloR = size * 0.30f;
            using var path = new GraphicsPath();
            path.AddEllipse(cx - haloR, cy - haloR, haloR * 2, haloR * 2);
            using var halo = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(150, 252, 211, 77),
                SurroundColors = new[] { Color.FromArgb(0, 252, 211, 77) },
            };
            g.FillPath(halo, path);
        }

        var dotBounds = new RectangleF(cx - dotR, cy - dotR, dotR * 2, dotR * 2);
        Color top = running ? Color.FromArgb(252, 211, 77) : Color.FromArgb(113, 113, 122);
        Color bottom = running ? Color.FromArgb(217, 119, 6) : Color.FromArgb(113, 113, 122);

        using var fill = new LinearGradientBrush(dotBounds, top, bottom, LinearGradientMode.Vertical);
        g.FillEllipse(fill, dotBounds);
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
