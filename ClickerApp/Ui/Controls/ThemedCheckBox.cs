using System.Drawing.Drawing2D;

namespace ClickerApp;

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

        int top = (Height - BoxSize) / 2;
        var box = new Rectangle(0, top, BoxSize - 1, BoxSize - 1);
        using var path = Theme.RoundedRect(box, 5);

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
            DrawCheck(g, box, Enabled ? Theme.OnAccent : Theme.Disabled);
        }

        if (Text.Length > 0)
        {
            var text = new Rectangle(BoxSize + TextGap, 0, Width - BoxSize - TextGap, Height);
            TextRenderer.DrawText(g, Text, Font, text,
                Enabled ? Theme.TextPrimary : Theme.Disabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        if (Focused)
        {
            var ring = Rectangle.Inflate(box, 3, 3);
            using var ringPath = Theme.RoundedRect(ring, 7);
            using var pen = new Pen(Theme.Accent);
            g.DrawPath(pen, ringPath);
        }
    }

    private static void DrawCheck(Graphics g, Rectangle box, Color color)
    {
        using var pen = new Pen(color, 1.9f)
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
