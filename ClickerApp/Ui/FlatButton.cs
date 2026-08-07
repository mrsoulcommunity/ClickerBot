using System.Drawing.Drawing2D;

namespace ClickerApp;

internal enum ButtonKind
{
    /// <summary>Filled accent button for the main action.</summary>
    Primary,

    /// <summary>Outlined button for secondary actions.</summary>
    Secondary,

    /// <summary>Outlined button that reads as destructive.</summary>
    Danger,
}

/// <summary>Flat, rounded, hover-aware button drawn from the <see cref="Theme"/> palette.</summary>
internal sealed class FlatButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public FlatButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.Button;
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
    }

    public ButtonKind Kind { get; set; } = ButtonKind.Secondary;

    public int CornerRadius { get; set; } = 8;

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Surface);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, CornerRadius);
        var (back, fore, border) = ResolveColors();

        using (var fill = new SolidBrush(back))
        {
            g.FillPath(fill, path);
        }

        if (border != Color.Empty)
        {
            using var pen = new Pen(border);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, bounds, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private (Color Back, Color Fore, Color Border) ResolveColors()
    {
        if (!Enabled)
        {
            return (Theme.Field, Theme.Disabled, Theme.Border);
        }

        return Kind switch
        {
            ButtonKind.Primary => (
                _pressed ? Theme.AccentHover : _hovered ? Theme.AccentHover : Theme.Accent,
                Color.White,
                Color.Empty),
            ButtonKind.Danger => (
                _hovered ? Color.FromArgb(253, 242, 242) : Theme.Surface,
                Theme.Danger,
                _hovered ? Theme.Danger : Theme.Border),
            _ => (
                _hovered ? Theme.AccentSoft : Theme.Surface,
                _hovered ? Theme.Accent : Theme.TextPrimary,
                _hovered ? Theme.Accent : Theme.Border),
        };
    }
}
