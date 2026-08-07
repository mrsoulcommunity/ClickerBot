using System.Drawing.Drawing2D;

namespace ClickerApp;

/// <summary>
/// The active palette, plus the fonts and drawing helpers shared by every control.
///
/// Controls read these properties inside <c>OnPaint</c> rather than caching colors at
/// construction time, so switching <see cref="Mode"/> at runtime is just a repaint.
/// </summary>
internal static class Theme
{
    /// <summary>Raised after <see cref="Apply"/> changes the palette.</summary>
    public static event Action? Changed;

    public static ThemeMode Mode { get; private set; } = ThemeMode.Light;

    public static Palette Current { get; private set; } = Palette.Light;

    public static bool IsDark => Current.IsDark;

    /// <summary>Switches the palette and notifies listeners. A no-op if already applied.</summary>
    public static void Apply(ThemeMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        Current = Palette.For(mode);
        Changed?.Invoke();
    }

    public static ThemeMode Opposite => Mode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;

    // --- Palette shortcuts ----------------------------------------------
    // These forward to Current so existing paint code stays terse and always
    // reads the live palette.

    public static Color Background => Current.Background;

    public static Color Surface => Current.Surface;

    public static Color Field => Current.Field;

    public static Color FieldHover => Current.FieldHover;

    public static Color Border => Current.Border;

    public static Color BorderStrong => Current.BorderStrong;

    public static Color TextPrimary => Current.TextPrimary;

    public static Color TextSecondary => Current.TextSecondary;

    public static Color Accent => Current.Accent;

    public static Color AccentHover => Current.AccentHover;

    public static Color AccentSoft => Current.AccentSoft;

    public static Color OnAccent => Current.OnAccent;

    public static Color Danger => Current.Danger;

    public static Color DangerSoft => Current.DangerSoft;

    public static Color Success => Current.Success;

    public static Color Disabled => Current.Disabled;

    public static Color DisabledSurface => Current.DisabledSurface;

    // --- Typography ------------------------------------------------------

    public static readonly Font Base = new("Segoe UI", 9F);
    public static readonly Font Title = new("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font CardTitle = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font Button = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 8.25F);
    public static readonly Font Caption = new("Segoe UI", 8F, FontStyle.Bold);

    // --- Drawing helpers -------------------------------------------------

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// The color an owner-drawn control should clear itself with.
    ///
    /// A control cannot simply use <c>Parent.BackColor</c>: a transparent parent reports
    /// <see cref="Color.Transparent"/>, and clearing with that paints solid black. So walk
    /// up until an opaque ancestor is found.
    /// </summary>
    public static Color BackdropOf(Control control)
    {
        for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent.BackColor.A == 255)
            {
                return parent.BackColor;
            }
        }

        return Background;
    }

    /// <summary>Blends <paramref name="from"/> towards <paramref name="to"/>; used for hover fades.</summary>
    public static Color Mix(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.R + ((to.R - from.R) * amount)),
            (int)(from.G + ((to.G - from.G) * amount)),
            (int)(from.B + ((to.B - from.B) * amount)));
    }
}
