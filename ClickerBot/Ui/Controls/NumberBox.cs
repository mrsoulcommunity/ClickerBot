using System.Drawing.Drawing2D;
using System.Globalization;

namespace ClickerBot;

/// <summary>
/// An integer input with a rounded field and −/+ steppers on either side.
///
/// This replaces <see cref="NumericUpDown"/>, whose spin buttons are painted by the system
/// and stay light in a dark palette. Text entry is delegated to a borderless child text box
/// so caret behaviour, selection and IME all stay native; only the frame and steppers are
/// drawn here.
/// </summary>
internal sealed class NumberBox : Control, IThemedControl
{
    private const int StepperWidth = 26;
    private const int Radius = 8;

    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        TextAlign = HorizontalAlignment.Center,
    };

    private int _value;
    private int _minimum;
    private int _maximum = 100;
    private int _hoveredStep;
    private bool _syncing;

    public NumberBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Theme.Base;
        Size = new Size(90, 30);

        // The field carries a number, so it is set in the monospace face like every other
        // number in the window — and the digits keep their column as the value changes.
        _input.Font = Theme.Mono;
        _input.GotFocus += (_, _) => Invalidate();
        _input.LostFocus += (_, _) =>
        {
            Normalize();
            Invalidate();
        };
        _input.TextChanged += Input_TextChanged;
        _input.KeyDown += Input_KeyDown;
        _input.MouseWheel += (_, e) => Step(e.Delta > 0 ? 1 : -1);
        Controls.Add(_input);

        ApplyTheme();
        SyncText();
    }

    public event EventHandler? ValueChanged;

    public int Minimum => _minimum;

    public int Maximum => _maximum;

    /// <summary>
    /// Sets both ends of the range at once. Deliberately not two separate setters: assigning
    /// them one after another leaves a moment where the minimum is above the maximum, and the
    /// clamp below throws on exactly that.
    /// </summary>
    public void SetRange(int minimum, int maximum)
    {
        _minimum = Math.Min(minimum, maximum);
        _maximum = Math.Max(minimum, maximum);
        Value = _value;
    }

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
            {
                SyncText();
                return;
            }

            _value = clamped;
            SyncText();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Applies whatever is typed in the field right now, exactly as leaving it would.
    /// Half-typed text is only reconciled on focus loss, so anything that acts on the value
    /// without the field losing focus first — a global hotkey, say — has to ask for it.
    /// </summary>
    public void Commit() => Normalize();

    public void ApplyTheme()
    {
        _input.BackColor = Theme.Field;
        _input.ForeColor = Theme.TextPrimary;
        Invalidate();
    }

    /// <summary>
    /// Windows paints a disabled <see cref="TextBox"/> itself, in system colors, ignoring
    /// whatever BackColor and ForeColor were assigned — which on a dark palette leaves the
    /// number effectively unreadable. So a disabled field hides the text box entirely and
    /// <see cref="OnPaint"/> draws the value instead, in palette colors we control.
    ///
    /// This matters far beyond the odd greyed-out field: every setting card is disabled for
    /// the duration of a run, so without it every number in the window goes unreadable at
    /// exactly the moment there is something worth reading.
    ///
    /// <see cref="Control.Enabled"/> reports the effective state — false when any ancestor is
    /// disabled — and OnEnabledChanged reaches children when a parent's state changes, so a
    /// card disabling itself lands here the same way this field disabling itself does.
    /// </summary>
    protected override void OnEnabledChanged(EventArgs e)
    {
        _input.Visible = Enabled;
        ApplyTheme();
        base.OnEnabledChanged(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        LayoutInput();
        base.OnFontChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        LayoutInput();
        base.OnResize(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        int zone = ZoneAt(e.Location);
        if (zone != 0)
        {
            Step(zone);
        }
        else
        {
            _input.Focus();
            _input.SelectAll();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int zone = ZoneAt(e.Location);
        if (zone != _hoveredStep)
        {
            _hoveredStep = zone;
            Cursor = zone == 0 ? Cursors.IBeam : Cursors.Hand;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hoveredStep != 0)
        {
            _hoveredStep = 0;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(bounds, Radius);

        bool focused = _input.Focused;
        using (var fill = new SolidBrush(Enabled ? Theme.Field : Theme.DisabledSurface))
        {
            g.FillPath(fill, path);
        }

        using (var pen = new Pen(!Enabled ? Theme.Border : focused ? Theme.Accent : Theme.Border))
        {
            g.DrawPath(pen, path);
        }

        // Stands in for the hidden text box — see OnEnabledChanged.
        if (!Enabled)
        {
            TextRenderer.DrawText(g, _input.Text, _input.Font,
                new Rectangle(StepperWidth, 0, Math.Max(10, Width - (StepperWidth * 2)), Height),
                Theme.Disabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        DrawStepper(g, new Rectangle(1, 1, StepperWidth, Height - 3), minus: true);
        DrawStepper(g, new Rectangle(Width - StepperWidth - 1, 1, StepperWidth, Height - 3), minus: false);
    }

    private void DrawStepper(Graphics g, Rectangle area, bool minus)
    {
        int zone = minus ? -1 : 1;
        bool active = Enabled && _hoveredStep == zone;
        bool exhausted = minus ? _value <= _minimum : _value >= _maximum;

        if (active && !exhausted)
        {
            using var path = Theme.RoundedRect(area, 6);
            using var fill = new SolidBrush(Theme.FieldHover);
            g.FillPath(fill, path);
        }

        Color glyph = !Enabled || exhausted
            ? Theme.Disabled
            : active ? Theme.Accent : Theme.TextSecondary;

        using var pen = new Pen(glyph, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        int cx = area.Left + (area.Width / 2);
        int cy = area.Top + (area.Height / 2);
        const int arm = 4;

        g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
        if (!minus)
        {
            g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
        }
    }

    /// <summary>-1 for the decrement zone, 1 for increment, 0 for the text area.</summary>
    private int ZoneAt(Point point)
    {
        if (point.X < StepperWidth)
        {
            return -1;
        }

        return point.X > Width - StepperWidth ? 1 : 0;
    }

    private void Step(int direction)
    {
        if (!Enabled)
        {
            return;
        }

        Value = _value + direction;
        _input.SelectionStart = _input.TextLength;
    }

    private void LayoutInput()
    {
        int width = Math.Max(10, Width - (StepperWidth * 2));
        _input.SetBounds(StepperWidth, (Height - _input.Height) / 2, width, _input.Height);
    }

    private void SyncText()
    {
        string text = _value.ToString(CultureInfo.InvariantCulture);
        if (_input.Text == text)
        {
            return;
        }

        _syncing = true;
        _input.Text = text;
        _input.SelectionStart = _input.TextLength;
        _syncing = false;
    }

    private void Input_TextChanged(object? sender, EventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        // Accept the value while typing, but leave the raw text alone so the caret does not
        // jump. Anything unparseable (an empty box, a lone "-") is normalized on leave.
        if (int.TryParse(_input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) &&
            parsed >= _minimum && parsed <= _maximum &&
            parsed != _value)
        {
            _value = parsed;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        Invalidate();
    }

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                Step(1);
                e.Handled = e.SuppressKeyPress = true;
                break;
            case Keys.Down:
                Step(-1);
                e.Handled = e.SuppressKeyPress = true;
                break;
            case Keys.Enter:
                Normalize();
                e.Handled = e.SuppressKeyPress = true;
                break;
        }
    }

    /// <summary>Pulls the text back in line with <see cref="Value"/> after free-form editing.</summary>
    private void Normalize()
    {
        if (int.TryParse(_input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            Value = parsed;
        }

        SyncText();
    }
}
