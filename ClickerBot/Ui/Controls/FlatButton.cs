using System.Drawing.Drawing2D;

namespace ClickerBot;

internal enum ButtonKind
{
    /// <summary>Filled accent button for the main action.</summary>
    Primary,

    /// <summary>Outlined button for secondary actions.</summary>
    Secondary,

    /// <summary>Outlined button that reads as destructive.</summary>
    Danger,
}

/// <summary>Flat, rounded, hover-aware button drawn from the active <see cref="Theme"/>.</summary>
internal sealed class FlatButton : Button, IThemedControl
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

    private ButtonKind _kind = ButtonKind.Secondary;

    public ButtonKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
            {
                return;
            }

            _kind = value;
            Invalidate();
        }
    }

    public int CornerRadius { get; set; } = 8;

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
        g.Clear(Theme.BackdropOf(this));

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

        if (Focused && Enabled)
        {
            using var ring = Theme.RoundedRect(Rectangle.Inflate(bounds, -3, -3), Math.Max(2, CornerRadius - 3));
            using var pen = new Pen(Color.FromArgb(120, fore));
            g.DrawPath(pen, ring);
        }

        TextRenderer.DrawText(g, Text, Font, bounds, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private (Color Back, Color Fore, Color Border) ResolveColors()
    {
        if (!Enabled)
        {
            return (Theme.DisabledSurface, Theme.Disabled, Theme.Border);
        }

        return _kind switch
        {
            ButtonKind.Primary => (
                _pressed || _hovered ? Theme.AccentHover : Theme.Accent,
                Theme.OnAccent,
                Color.Empty),
            ButtonKind.Danger => (
                _hovered ? Theme.DangerSoft : Theme.Surface,
                Theme.Danger,
                _hovered ? Theme.Danger : Theme.Border),
            _ => (
                _hovered ? Theme.AccentSoft : Theme.Surface,
                _hovered ? Theme.Accent : Theme.TextPrimary,
                _hovered ? Theme.Accent : Theme.BorderStrong),
        };
    }
}
