using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Implemented by controls that own their appearance. <see cref="ApplyTheme"/> is called
/// once at startup and again after every theme switch.
/// </summary>
internal interface IThemedControl
{
    void ApplyTheme();
}

/// <summary>
/// Pushes the active <see cref="Theme"/> through a form's control tree.
///
/// Owner-drawn controls only need a repaint, but stock controls hold their colors in
/// properties, so the tree is walked and every <see cref="IThemedControl"/> is asked to
/// restyle itself. Painting is suspended for the duration so the switch lands in one frame
/// instead of cascading visibly control by control.
/// </summary>
internal static class ThemeManager
{
    private const int WM_SETREDRAW = 0x000B;

    /// <summary>
    /// Applies the current theme to <paramref name="form"/> and keeps it in sync until the
    /// form is disposed.
    /// </summary>
    public static void Attach(Form form)
    {
        void Handler() => Refresh(form);

        Theme.Changed += Handler;
        form.Disposed += (_, _) => Theme.Changed -= Handler;

        Refresh(form);
    }

    private static void Refresh(Form form)
    {
        if (form.IsDisposed)
        {
            return;
        }

        bool locked = form.IsHandleCreated;
        if (locked)
        {
            SendMessage(form.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        try
        {
            form.BackColor = Theme.Background;
            ApplyTo(form);
        }
        finally
        {
            if (locked)
            {
                SendMessage(form.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                form.Invalidate(invalidateChildren: true);
            }
        }

        // Outside the redraw lock: the title bar is non-client area and repaints on its own.
        WindowChrome.ApplyTheme(form);
    }

    private static void ApplyTo(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is IThemedControl themed)
            {
                themed.ApplyTheme();
            }

            child.Invalidate();
            ApplyTo(child);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
