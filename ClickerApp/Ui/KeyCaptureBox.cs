using System.Drawing.Drawing2D;

namespace ClickerApp;

/// <summary>
/// A field that records whichever key the user presses while it has focus.
/// Works for every key including Tab, Enter and Space; Esc clears the assignment.
/// </summary>
internal sealed class KeyCaptureBox : Control
{
    private Keys _key = Keys.None;
    private bool _hovered;

    public KeyCaptureBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = Theme.Base;
        Size = new Size(150, 32);
    }

    public event EventHandler? KeyValueChanged;

    public Keys Key
    {
        get => _key;
        set
        {
            if (_key == value)
            {
                return;
            }

            _key = value;
            Invalidate();
            KeyValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Placeholder { get; set; } = "Press a key";

    // Every key should reach OnKeyDown instead of being handled as a dialog key.
    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        base.OnMouseDown(e);
    }

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

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        Key = e.KeyCode switch
        {
            Keys.Escape => Keys.None,
            Keys.None => Key,
            _ => e.KeyCode,
        };

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Surface);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, 8);

        Color back = Focused ? Theme.AccentSoft : _hovered ? Theme.Field : Theme.Field;
        Color border = Focused ? Theme.Accent : Theme.Border;

        using (var fill = new SolidBrush(Enabled ? back : Theme.Field))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(border))
        {
            g.DrawPath(pen, path);
        }

        bool empty = Key == Keys.None;
        string text = Focused ? (empty ? "Press any key…" : KeyNames.Describe(Key)) : empty ? Placeholder : KeyNames.Describe(Key);
        Color fore = !Enabled ? Theme.Disabled : empty ? Theme.TextSecondary : Focused ? Theme.Accent : Theme.TextPrimary;

        TextRenderer.DrawText(g, text, Font, new Rectangle(8, 0, Width - 16, Height), fore,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
