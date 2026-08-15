using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A fully owner-drawn checkbox.
///
/// The stock control lets Windows paint the box itself, which stays light-on-light in a
/// dark palette no matter what BackColor is set, so the glyph is drawn here instead.
/// </summary>
internal sealed class ThemedCheckBox : CheckBox, IThemedControl
{
    private const int BoxSize = 17;
    private const int TextGap = 9;

    private bool _hovered;

    public ThemedCheckBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        AutoSize = false;
        Cursor = Cursors.Hand;
        Font = Theme.Base;
    }

    public void ApplyTheme() => Invalidate();

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        // Scaled here rather than left as literals: the framework's one auto-scale pass grows
        // the control's bounds on a high-DPI screen but not the composition painted inside
        // them, which left a 17px glyph beside text set in a font that had grown with the
        // display. See Theme.Scale.
        int size = Theme.Scale(BoxSize, this);
        int gap = Theme.Scale(TextGap, this);
        int radius = Theme.Scale(5, this);

        int top = (Height - size) / 2;
        var box = new Rectangle(0, top, size - 1, size - 1);
        using var path = Theme.RoundedRect(box, radius);

        bool on = Checked;
        Color fill = !Enabled
            ? Theme.DisabledSurface
            : on
                ? (_hovered ? Theme.AccentHover : Theme.Accent)
                : (_hovered ? Theme.FieldHover : Theme.Field);
        Color outline = !Enabled
            ? Theme.Border
            : on
                ? Color.Empty
                : (_hovered ? Theme.Accent : Theme.BorderStrong);

        using (var brush = new SolidBrush(fill))
        {
            g.FillPath(brush, path);
        }

        if (outline != Color.Empty)
        {
            using var pen = new Pen(outline);
            g.DrawPath(pen, path);
        }

        if (on)
        {
            DrawCheck(g, box, Enabled ? Theme.OnAccent : Theme.Disabled, size * 1.9f / BoxSize);
        }

        if (Text.Length > 0)
        {
            var text = new Rectangle(size + gap, 0, Width - size - gap, Height);
            TextRenderer.DrawText(g, Text, Font, text,
                Enabled ? Theme.TextPrimary : Theme.Disabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // Drawn just inside the box rather than as a halo around it. The box starts at x = 0,
        // so an outer ring sat at x = -3 and was clipped away by the control's own bounds —
        // which is what left the focus ring flat on one side and doubled-looking on the others.
        if (Focused && Enabled)
        {
            int inset = Math.Max(1, Theme.Scale(2, this));
            var ring = Rectangle.Inflate(box, -inset, -inset);
            using var ringPath = Theme.RoundedRect(ring, Math.Max(1, radius - inset));
            using var pen = new Pen(on ? Theme.OnAccent : Theme.Accent, Math.Max(1f, Theme.Scale(1, this)));
            g.DrawPath(pen, ringPath);
        }
    }

    private static void DrawCheck(Graphics g, Rectangle box, Color color, float stroke)
    {
        using var pen = new Pen(color, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        float left = box.Left + (box.Width * 0.26f);
        float mid = box.Left + (box.Width * 0.44f);
        float right = box.Left + (box.Width * 0.76f);

        g.DrawLines(pen, new[]
        {
            new PointF(left, box.Top + (box.Height * 0.52f)),
            new PointF(mid, box.Top + (box.Height * 0.71f)),
            new PointF(right, box.Top + (box.Height * 0.31f)),
        });
    }
}
