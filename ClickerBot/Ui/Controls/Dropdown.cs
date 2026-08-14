using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A list you pick one value from, drawn entirely by this app.
///
/// Replaces <see cref="ComboBox"/> for the same reason the checkbox glyph and the numeric
/// field's steppers are drawn by hand: Windows paints a combo box's frame, its drop-down
/// arrow and the whole popup itself, in system colors it ignores BackColor for — which on the
/// dark palette leaves one conspicuously light control in an otherwise fully themed window.
///
/// Used where <see cref="Segmented"/> would not fit: the eight step kinds are too many, and
/// their names too long, to sit side by side in one pill.
/// </summary>
internal sealed class Dropdown : Control, IThemedControl
{
    private const int Radius = 8;
    private const int Inset = 11;
    private const int ArrowBox = 26;

    private string[] _items = Array.Empty<string>();
    private int _selectedIndex = -1;
    private bool _hovered;
    private ContextMenuStrip? _popup;

    public Dropdown()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = Theme.Base;
        Size = new Size(200, 30);
    }

    public event EventHandler? SelectedIndexChanged;

    /// <summary>The option labels, in order. Assigning this never raises <see cref="SelectedIndexChanged"/>.</summary>
    public string[] Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<string>();
            _selectedIndex = Math.Min(_selectedIndex, _items.Length - 1);
            Invalidate();
        }
    }

    /// <summary>The chosen index, or -1 for nothing chosen.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = _items.Length == 0 ? -1 : Math.Clamp(value, -1, _items.Length - 1);
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

    // Up/Down step through the options without opening the popup, the way a real combo box does.
    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                SelectedIndex = Math.Min(_selectedIndex + 1, _items.Length - 1);
                e.Handled = e.SuppressKeyPress = true;
                break;
            case Keys.Up:
                SelectedIndex = Math.Max(_selectedIndex - 1, 0);
                e.Handled = e.SuppressKeyPress = true;
                break;
            case Keys.Space:
            case Keys.Enter:
                OpenPopup();
                e.Handled = e.SuppressKeyPress = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        OpenPopup();
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

    /// <summary>
    /// Opens the list. Built fresh each time rather than kept and reused, so the item labels
    /// follow a language switch and the widths re-measure with them; the previous one is
    /// released here rather than from its own Closed event, which would dispose it mid-close.
    /// </summary>
    private void OpenPopup()
    {
        ClosePopup();

        if (_items.Length == 0 || !Enabled)
        {
            return;
        }

        var popup = new ContextMenuStrip
        {
            Renderer = new ThemedMenuRenderer(),
            ShowImageMargin = false,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            Font = Font,
        };

        for (int i = 0; i < _items.Length; i++)
        {
            int index = i;
            var item = new ToolStripMenuItem(_items[i]);
            item.Click += (_, _) => SelectedIndex = index;
            popup.Items.Add(item);
        }

        popup.Closed += (_, _) => Invalidate();

        _popup = popup;
        popup.MinimumSize = new Size(Width, 0);
        popup.Show(this, new Point(0, Height));
    }

    private void ClosePopup()
    {
        _popup?.Dispose();
        _popup = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClosePopup();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        int S(int value) => Theme.Scale(value, this);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, S(Radius));

        bool open = _popup is { Visible: true };
        Color back = !Enabled ? Theme.DisabledSurface
            : open || Focused ? Theme.AccentSoft
            : _hovered ? Theme.FieldHover
            : Theme.Field;
        Color border = !Enabled ? Theme.Border
            : open || Focused || _hovered ? Theme.Accent
            : Theme.Border;

        using (var fill = new SolidBrush(back))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(border))
        {
            g.DrawPath(pen, path);
        }

        bool empty = _selectedIndex < 0 || _selectedIndex >= _items.Length;
        Color fore = !Enabled ? Theme.Disabled
            : empty ? Theme.TextSecondary
            : open || Focused ? Theme.Accent
            : Theme.TextPrimary;

        int arrowBox = S(ArrowBox);
        var textArea = new Rectangle(S(Inset), 0, Math.Max(S(10), Width - S(Inset) - arrowBox), Height);
        TextRenderer.DrawText(g, empty ? string.Empty : _items[_selectedIndex], Font, textArea, fore,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        DrawChevron(g, new Rectangle(Width - arrowBox, 0, arrowBox, Height), fore, S(4));
    }

    private static void DrawChevron(Graphics g, Rectangle area, Color color, int arm)
    {
        using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        int cx = area.Left + (area.Width / 2);
        int cy = area.Top + (area.Height / 2);
        int drop = Math.Max(2, arm / 2);

        g.DrawLines(pen, new[]
        {
            new Point(cx - arm, cy - drop),
            new Point(cx, cy + drop),
            new Point(cx + arm, cy - drop),
        });
    }
}
