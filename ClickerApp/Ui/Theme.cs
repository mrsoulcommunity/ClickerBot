using System.Drawing.Drawing2D;

namespace ClickerApp;

/// <summary>Central palette, fonts and drawing helpers so the UI stays visually consistent.</summary>
internal static class Theme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#F4F5F7");
    public static readonly Color Surface = Color.White;
    public static readonly Color Field = ColorTranslator.FromHtml("#F9FAFB");
    public static readonly Color Border = ColorTranslator.FromHtml("#E3E6EB");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1B1F2A");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#7A8194");
    public static readonly Color Accent = ColorTranslator.FromHtml("#4F46E5");
    public static readonly Color AccentHover = ColorTranslator.FromHtml("#4338CA");
    public static readonly Color AccentSoft = ColorTranslator.FromHtml("#EEF0FE");
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color Success = ColorTranslator.FromHtml("#15803D");
    public static readonly Color Disabled = ColorTranslator.FromHtml("#C7CBD4");

    public static readonly Font Base = new("Segoe UI", 9F);
    public static readonly Font Title = new("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font CardTitle = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font Button = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 8.25F);
    public static readonly Font Caption = new("Segoe UI", 8F, FontStyle.Bold);

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
}
