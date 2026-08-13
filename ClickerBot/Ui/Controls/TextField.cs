using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A single-line text field with a rounded, themed frame — the free-text counterpart to
/// <see cref="NumberBox"/>. Text entry is delegated to a borderless child text box so caret
/// behaviour, selection and IME all stay native; only the frame is drawn here.
/// </summary>
internal sealed class TextField : Control, IThemedControl
{
    private const int Radius = 8;
    private const int Inset = 10;

    private readonly TextBox _input = new() { BorderStyle = BorderStyle.None };

    public TextField()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Theme.Base;
        Size = new Size(160, 32);

        _input.Font = Theme.Base;
        _input.GotFocus += (_, _) => Invalidate();
        _input.LostFocus += (_, _) => Invalidate();
        _input.TextChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(_input);

        ApplyTheme();
        LayoutInput();
    }

    public event EventHandler? ValueChanged;

    public string Value
    {
        get => _input.Text;
        set
        {
            string normalized = value ?? string.Empty;
            if (_input.Text != normalized)
            {
                _input.Text = normalized;
            }
        }
    }

    public int MaxLength
    {
        get => _input.MaxLength;
        set => _input.MaxLength = value;
    }

    public void ApplyTheme()
    {
        _input.BackColor = Enabled ? Theme.Field : Theme.DisabledSurface;
        _input.ForeColor = Enabled ? Theme.TextPrimary : Theme.Disabled;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        _input.Enabled = Enabled;
        ApplyTheme();
        base.OnEnabledChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        LayoutInput();
        base.OnResize(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _input.Focus();
        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, Radius);

        using (var fill = new SolidBrush(Enabled ? Theme.Field : Theme.DisabledSurface))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(!Enabled ? Theme.Border : _input.Focused ? Theme.Accent : Theme.Border))
        {
            g.DrawPath(pen, path);
        }
    }

    private void LayoutInput()
    {
        int height = _input.PreferredHeight;
        _input.SetBounds(Inset, (Height - height) / 2, Math.Max(10, Width - (Inset * 2)), height);
    }
}
