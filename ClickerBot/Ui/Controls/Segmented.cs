using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A row of mutually exclusive options in one pill, with the selected one filled.
///
/// Used instead of radio buttons throughout: the choices here are short, always few, and
/// always worth showing side by side — "Left / Right / Middle" reads as one decision, where
/// three stacked radios read as three.
/// </summary>
internal sealed class Segmented : Control, IThemedControl
{
    private const int Inset = 3;

    private string[] _items = Array.Empty<string>();
    private int _selectedIndex;
    private int _hoveredIndex = -1;

    public Segmented()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        TabStop = true;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Font = Theme.Base;
        Size = new Size(200, 30);
    }

    public event EventHandler? SelectedIndexChanged;

    /// <summary>The option labels, left to right. Setting this resets nothing but the layout.</summary>
    public string[] Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<string>();
            _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, _items.Length - 1));
            Invalidate();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = _items.Length == 0 ? 0 : Math.Clamp(value, 0, _items.Length - 1);
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyTheme() => Invalidate();

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();

        int index = IndexAt(e.X);
        if (index >= 0)
        {
            SelectedIndex = index;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int index = IndexAt(e.X);
        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hoveredIndex != -1)
        {
            _hoveredIndex = -1;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Right)
        {
            SelectedIndex = _selectedIndex + (e.KeyCode == Keys.Right ? 1 : -1);
            e.Handled = e.SuppressKeyPress = true;
        }

        base.OnKeyDown(e);
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        if (_items.Length == 0 || Width <= 2 || Height <= 2)
        {
            return;
        }

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var track = Theme.RoundedRect(bounds, Height / 2))
        {
            using var fill = new SolidBrush(Enabled ? Theme.Field : Theme.DisabledSurface);
            g.FillPath(fill, track);

            using var pen = new Pen(Enabled && Focused ? Theme.Accent : Theme.Border);
            g.DrawPath(pen, track);
        }

        for (int i = 0; i < _items.Length; i++)
        {
            var cell = CellAt(i);
            bool selected = i == _selectedIndex;
            bool hovered = Enabled && i == _hoveredIndex && !selected;

            if (selected)
            {
                using var path = Theme.RoundedRect(cell, cell.Height / 2);
                using var fill = new SolidBrush(Enabled ? Theme.Accent : Theme.Disabled);
                g.FillPath(fill, path);
            }
            else if (hovered)
            {
                using var path = Theme.RoundedRect(cell, cell.Height / 2);
                using var fill = new SolidBrush(Theme.FieldHover);
                g.FillPath(fill, path);
            }

            Color text = !Enabled ? Theme.Disabled
                : selected ? Theme.OnAccent
                : hovered ? Theme.TextPrimary
                : Theme.TextSecondary;

            TextRenderer.DrawText(g, _items[i], Font, cell, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    private Rectangle CellAt(int index)
    {
        // Laid out from cumulative edges rather than index * width, so rounding never leaves
        // a one-pixel gap between two segments.
        int span = Width - (Inset * 2);
        int left = Inset + (span * index / _items.Length);
        int right = Inset + (span * (index + 1) / _items.Length);

        return new Rectangle(left, Inset, Math.Max(1, right - left), Math.Max(1, Height - (Inset * 2)));
    }

    private int IndexAt(int x)
    {
        if (!Enabled || _items.Length == 0)
        {
            return -1;
        }

        for (int i = 0; i < _items.Length; i++)
        {
            var cell = CellAt(i);
            if (x >= cell.Left && x < cell.Right)
            {
                return i;
            }
        }

        return -1;
    }
}
