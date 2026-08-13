using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// The bottom bar: what the run is doing right now, and the controls to change that.
///
/// Everything that changes while a run is in flight lives here and nowhere else, so the
/// settings above can stay still. The readouts are monospace and fixed-width by design — a
/// counter that reflows as it counts is unreadable at a glance.
/// </summary>
internal sealed class RunPanel : Card
{
    // A fixed composition, so the rows are named rather than derived from Height: a status
    // row with the lamp and the live readouts, then a transport row with the rhythm strip
    // and the buttons. The two rows never share a column, which is what keeps the counter
    // from sliding under the Start button as it grows.
    private const int PanelHeight = 120;
    private const int Edge = 20;
    private const int ButtonWidth = 104;
    private const int ButtonHeight = 38;
    private const int ButtonTop = 62;
    private const int MeterTop = 58;
    private const int MeterHeight = 46;
    private const int ReadoutTop = 16;
    private const int ReadoutColumnWidth = 108;
    private const int ReadoutColumns = 3;
    private const int ReadoutBlockWidth = ReadoutColumnWidth * ReadoutColumns;

    private readonly CadenceMeter _meter = new();
    private readonly FlatButton _start = new();
    private readonly FlatButton _pause = new();
    private readonly FlatButton _stop = new();

    private RunPhase _phase = RunPhase.Running;
    private bool _running;
    private long _iteration;
    private TimeSpan _elapsed;
    private int _countdown;
    private long? _target;
    private TimeSpan? _window;
    private string _message = "Ready";
    private TextRole _messageRole = TextRole.Secondary;

    public RunPanel()
    {
        Title = string.Empty;

        Height = PanelHeight;
        _meter.SetBounds(Edge, MeterTop, 300, MeterHeight);
        Controls.Add(_meter);

        Configure(_start, "Start", ButtonKind.Primary);
        Configure(_pause, "Pause", ButtonKind.Secondary);
        Configure(_stop, "Stop", ButtonKind.Secondary);

        _start.Click += (_, _) => StartRequested?.Invoke(this, EventArgs.Empty);
        _pause.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);
        _stop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { _start, _pause, _stop });
    }

    public event EventHandler? StartRequested;

    public event EventHandler? PauseRequested;

    public event EventHandler? StopRequested;

    /// <summary>Records one completed iteration on the cadence strip.</summary>
    public void MarkIteration() => _meter.Mark();

    public void BeginRun(long? target, TimeSpan? window)
    {
        _target = target;
        _window = window;
        _iteration = 0;
        _elapsed = TimeSpan.Zero;
        _countdown = 0;
        _running = true;
        _phase = RunPhase.Running;
        _meter.Reset();
        _meter.Live = true;
        Refresh(idleMessage: null);
    }

    public void EndRun(string message, TextRole role)
    {
        _running = false;
        _meter.Live = false;
        Refresh(idleMessage: message, role);
    }

    public void Update(RunProgress progress)
    {
        _phase = progress.Phase;
        _iteration = progress.Iteration;
        _elapsed = progress.Elapsed;
        _countdown = progress.CountdownSeconds;
        Refresh(idleMessage: null);
    }

    /// <summary>Sets the resting message shown when nothing is running.</summary>
    public void SetIdleMessage(string message, TextRole role = TextRole.Secondary)
    {
        if (_running)
        {
            return;
        }

        Refresh(message, role);
    }

    public void SetStartEnabled(bool enabled) => _start.Enabled = enabled;

    public override void ApplyTheme()
    {
        base.ApplyTheme();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        LayoutChildren();
        base.OnResize(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DrawStatusLine(g);
        DrawReadouts(g);
        DrawProgress(g);
    }

    private void LayoutChildren()
    {
        int right = Width - Edge;

        _stop.SetBounds(right - ButtonWidth, ButtonTop, ButtonWidth, ButtonHeight);
        _pause.SetBounds(right - (ButtonWidth * 2) - 8, ButtonTop, ButtonWidth, ButtonHeight);
        _start.SetBounds(right - (ButtonWidth * 3) - 16, ButtonTop, ButtonWidth, ButtonHeight);

        // The meter takes whatever the buttons leave, which keeps the rhythm strip as wide
        // as the window allows.
        int meterRight = _start.Left - 24;
        _meter.SetBounds(Edge, MeterTop, Math.Max(80, meterRight - Edge), MeterHeight);
    }

    private void DrawStatusLine(Graphics g)
    {
        // A lamp, not a label: colour carries the state and the words only name it.
        var lamp = new Rectangle(21, 24, 9, 9);
        Color lampColor = !_running ? Theme.Disabled
            : _phase == RunPhase.Paused ? Theme.TextSecondary
            : Theme.Accent;

        using (var brush = new SolidBrush(lampColor))
        {
            g.FillEllipse(brush, lamp);
        }

        if (_running && _phase != RunPhase.Paused)
        {
            using var halo = new SolidBrush(Color.FromArgb(48, lampColor));
            g.FillEllipse(halo, Rectangle.Inflate(lamp, 4, 4));
        }

        Color text = _messageRole switch
        {
            TextRole.Danger => Theme.Danger,
            TextRole.Success => Theme.Success,
            TextRole.Accent => Theme.Accent,
            TextRole.Primary => Theme.TextPrimary,
            _ => Theme.TextSecondary,
        };

        // Stops short of the readout columns on the right rather than running under them.
        int available = Width - 38 - ReadoutBlockWidth - Edge - 16;
        TextRenderer.DrawText(g, _message, Theme.Base,
            new Rectangle(38, 18, Math.Max(60, available), 22), text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void DrawReadouts(Graphics g)
    {
        int top = ReadoutTop;
        int right = Width - Edge;

        string rate = _elapsed.TotalSeconds > 0.25 && _iteration > 0
            ? (_iteration / _elapsed.TotalSeconds).ToString("0.0") + "/s"
            : "—";

        var columns = new (string Label, string Value)[]
        {
            ("ITERATIONS", _target is { } total ? $"{_iteration:N0} / {total:N0}" : $"{_iteration:N0}"),
            ("ELAPSED", Format(_elapsed)),
            ("RATE", rate),
        };

        // Right-aligned as a block, so the numbers sit in a stable column instead of drifting
        // as the values change width.
        int x = right;
        for (int i = columns.Length - 1; i >= 0; i--)
        {
            x -= ReadoutColumnWidth;
            var cell = new Rectangle(x, top, ReadoutColumnWidth, 44);

            TextRenderer.DrawText(g, columns[i].Label, Theme.Caption,
                new Rectangle(cell.X, cell.Y, cell.Width, 14), Theme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, columns[i].Value, Theme.Mono,
                new Rectangle(cell.X, cell.Y + 15, cell.Width, 20),
                _running ? Theme.TextPrimary : Theme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }

    private void DrawProgress(Graphics g)
    {
        float fraction = Fraction();
        if (fraction < 0)
        {
            return;
        }

        // A hairline across the very top of the card: present when there is an end to
        // progress towards, invisible when the run is open-ended.
        var trough = new Rectangle(1, 1, Width - 3, 3);
        using (var path = Theme.RoundedRect(trough, 2))
        using (var fill = new SolidBrush(Theme.Track))
        {
            g.FillPath(fill, path);
        }

        int filled = (int)((trough.Width - 1) * Math.Clamp(fraction, 0f, 1f));
        if (filled <= 0)
        {
            return;
        }

        using (var path = Theme.RoundedRect(new Rectangle(trough.X, trough.Y, filled, trough.Height), 2))
        using (var fill = new SolidBrush(_running ? Theme.Accent : Theme.Disabled))
        {
            g.FillPath(fill, path);
        }
    }

    /// <summary>Progress towards the run's stop condition, or -1 when it has none.</summary>
    private float Fraction()
    {
        if (_target is { } total && total > 0)
        {
            return (float)((double)_iteration / total);
        }

        if (_window is { } window && window.TotalMilliseconds > 0)
        {
            return (float)(_elapsed.TotalMilliseconds / window.TotalMilliseconds);
        }

        return -1f;
    }

    private void Refresh(string? idleMessage, TextRole role = TextRole.Secondary)
    {
        if (idleMessage is not null)
        {
            _message = idleMessage;
            _messageRole = role;
        }
        else
        {
            (_message, _messageRole) = _phase switch
            {
                RunPhase.CountingDown => ($"Starting in {_countdown}…", TextRole.Accent),
                RunPhase.Paused => ("Paused", TextRole.Primary),
                _ => ("Running", TextRole.Accent),
            };
        }

        _pause.Text = _phase == RunPhase.Paused && _running ? "Resume" : "Pause";
        _pause.Enabled = _running;
        _pause.Kind = _phase == RunPhase.Paused && _running ? ButtonKind.Primary : ButtonKind.Secondary;
        _stop.Enabled = _running;
        _stop.Kind = _running ? ButtonKind.Danger : ButtonKind.Secondary;
        _start.Enabled = !_running;

        Invalidate();
    }

    private static string Format(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{value.Minutes:00}:{value.Seconds:00}";

    private static void Configure(FlatButton button, string text, ButtonKind kind)
    {
        button.Text = text;
        button.Kind = kind;
    }
}
