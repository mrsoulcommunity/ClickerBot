using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A pill switch that flips between the light and dark palettes.
///
/// The knob glides between ends over a short eased animation; the sun and moon are drawn
/// as geometry rather than glyphs so the control does not depend on an icon font being
/// present.
/// </summary>
internal sealed class ThemeToggle : Control, IThemedControl
{
    private const int AnimationMs = 170;
    private const int FrameMs = 15;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = FrameMs };
    private readonly ToolTip _tip = new() { InitialDelay = 400 };

    private float _position;
    private float _target;
    private bool _hovered;

    public ThemeToggle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        TabStop = true;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Size = new Size(58, 30);

        _timer.Tick += Timer_Tick;
        _position = _target = Theme.IsDark ? 1f : 0f;
        UpdateTooltip();
    }

    /// <summary>Raised after the user flips the switch, with the mode they asked for.</summary>
    public event Action<ThemeMode>? ModeRequested;

    public void ApplyTheme()
    {
        _target = Theme.IsDark ? 1f : 0f;
        UpdateTooltip();

        if (!_timer.Enabled)
        {
            _position = _target;
        }

        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _tip.Dispose();
        }

        base.Dispose(disposing);
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

    protected override void OnClick(EventArgs e)
    {
        Toggle();
        base.OnClick(e);
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Toggle();
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

    private void Toggle()
    {
        Focus();
        var requested = Theme.Opposite;
        _target = requested == ThemeMode.Dark ? 1f : 0f;
        _timer.Start();
        ModeRequested?.Invoke(requested);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        float step = (float)FrameMs / AnimationMs;
        float delta = _target - _position;

        if (Math.Abs(delta) <= step)
        {
            _position = _target;
            _timer.Stop();
        }
        else
        {
            _position += Math.Sign(delta) * step;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        // Ease-in-out so the knob settles instead of stopping dead.
        float t = _position < 0.5f
            ? 2f * _position * _position
            : 1f - (float)Math.Pow((-2f * _position) + 2f, 2) / 2f;

        var track = new Rectangle(0, 0, Width - 1, Height - 1);
        using var trackPath = Theme.RoundedRect(track, Height / 2);

        using (var fill = new SolidBrush(Theme.Mix(Theme.Field, Theme.AccentSoft, t)))
        {
            g.FillPath(fill, trackPath);
        }

        using (var pen = new Pen(_hovered || Focused ? Theme.Accent : Theme.Border))
        {
            g.DrawPath(pen, trackPath);
        }

        int inset = 3;
        int knobSize = Height - (inset * 2) - 1;
        int travel = Width - knobSize - (inset * 2) - 1;
        var knob = new Rectangle(inset + (int)(travel * t), inset, knobSize, knobSize);

        using (var shadow = new SolidBrush(Color.FromArgb(Theme.IsDark ? 70 : 34, 0, 0, 0)))
        {
            g.FillEllipse(shadow, knob.X, knob.Y + 1, knob.Width, knob.Height);
        }

        using (var fill = new SolidBrush(Theme.Mix(Theme.Surface, Theme.Accent, t)))
        {
            g.FillEllipse(fill, knob);
        }

        Color icon = Theme.Mix(Theme.TextSecondary, Theme.OnAccent, t);
        if (t < 0.5f)
        {
            DrawSun(g, knob, icon, 1f - (t * 2f));
        }
        else
        {
            DrawMoon(g, knob, icon, (t - 0.5f) * 2f);
        }
    }

    private static void DrawSun(Graphics g, Rectangle knob, Color color, float strength)
    {
        using var brush = new SolidBrush(Color.FromArgb((int)(255 * strength), color));
        using var pen = new Pen(brush.Color, 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        float cx = knob.X + (knob.Width / 2f);
        float cy = knob.Y + (knob.Height / 2f);
        float core = knob.Width * 0.20f;

        g.FillEllipse(brush, cx - core, cy - core, core * 2, core * 2);

        float inner = core + 2.0f;
        float outer = core + 4.2f;
        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4;
            float dx = (float)Math.Cos(angle);
            float dy = (float)Math.Sin(angle);
            g.DrawLine(pen, cx + (dx * inner), cy + (dy * inner), cx + (dx * outer), cy + (dy * outer));
        }
    }

    private static void DrawMoon(Graphics g, Rectangle knob, Color color, float strength)
    {
        using var brush = new SolidBrush(Color.FromArgb((int)(255 * strength), color));

        float cx = knob.X + (knob.Width / 2f);
        float cy = knob.Y + (knob.Height / 2f);
        float r = knob.Width * 0.30f;

        // A crescent: a full disc with an offset disc subtracted from it.
        using var path = new GraphicsPath();
        path.AddEllipse(cx - r, cy - r, r * 2, r * 2);

        using var bite = new GraphicsPath();
        bite.AddEllipse(cx - r + (r * 0.62f), cy - r - (r * 0.30f), r * 2, r * 2);

        using var region = new Region(path);
        region.Exclude(bite);
        g.FillRegion(brush, region);
    }

    private void UpdateTooltip() =>
        _tip.SetToolTip(this, Theme.IsDark ? "Switch to light mode" : "Switch to dark mode");
}
