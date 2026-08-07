using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>A rounded surface panel with a small heading — the basic grouping block of the UI.</summary>
internal sealed class Card : Panel, IThemedControl
{
    public Card()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Theme.Base;
        ApplyTheme();
    }

    /// <summary>Heading drawn at the top-left of the card.</summary>
    public string Title { get; set; } = string.Empty;

    // Children read Parent.BackColor to clear themselves, so it has to track the palette.
    public void ApplyTheme() => BackColor = Theme.Surface;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Cleared with the canvas color so the rounded corners blend into the window.
        g.Clear(Theme.Background);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, 10);

        using (var fill = new SolidBrush(Theme.Surface))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(Theme.Border))
        {
            g.DrawPath(pen, path);
        }

        if (Title.Length > 0)
        {
            TextRenderer.DrawText(g, Title, Theme.CardTitle, new Point(19, 14), Theme.TextSecondary,
                TextFormatFlags.NoPrefix);
        }
    }
}
