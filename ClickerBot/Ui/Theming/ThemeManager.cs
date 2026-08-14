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
/// Implemented by controls that carry their own translatable text — a button, a delay
/// editor's "Random"/"to" labels — as opposed to text <see cref="MainForm"/> sets directly on
/// a field it owns. <see cref="ApplyLanguage"/> is called once at startup and again after
/// every language switch, the same way <see cref="IThemedControl.ApplyTheme"/> is for colors.
/// </summary>
internal interface ILocalizedControl
{
    void ApplyLanguage();
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
    /// form is disposed. <paramref name="applyLanguage"/>, if given, is run once immediately
    /// and again on every language switch, each time followed by the same flicker-free
    /// tree-walk a theme switch gets — a language change is a lot of <c>.Text</c> assignments
    /// in quick succession, and it deserves the same single-frame redraw suspension.
    /// </summary>
    public static void Attach(Form form, Action? applyLanguage = null)
    {
        void ThemeHandler() => Refresh(form);
        void LanguageHandler()
        {
            applyLanguage?.Invoke();
            Refresh(form);
        }

        Theme.Changed += ThemeHandler;
        Loc.Changed += LanguageHandler;
        form.Disposed += (_, _) =>
        {
            Theme.Changed -= ThemeHandler;
            Loc.Changed -= LanguageHandler;
        };

        applyLanguage?.Invoke();
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

            if (child is ILocalizedControl localized)
            {
                localized.ApplyLanguage();
            }

            child.Invalidate();
            ApplyTo(child);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
