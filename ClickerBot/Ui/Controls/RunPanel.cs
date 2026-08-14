using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// The bottom bar: what the run is doing right now, and the controls to change that.
///
/// Everything that changes while a run is in flight lives here and nowhere else, so the
/// settings above can stay still. The readouts are monospace and fixed-width by design — a
/// counter that reflows as it counts is unreadable at a glance.
/// </summary>
internal sealed class RunPanel : Card, ILocalizedControl
{
    // A fixed composition, so the rows are named rather than derived from Height: a status
    // row with the lamp and the live readouts, then a transport row with the rhythm strip
    // and the buttons. The two rows never share a column, which is what keeps the counter
    // from sliding under the Start button as it grows.
    private const int PanelHeight = 120;
    private const int Edge = 20;
    private const int ButtonWidth = 104;
    private const int TestButtonWidth = 84;
    private const int ButtonGap = 8;
    private const int ButtonHeight = 38;
    private const int ButtonTop = 62;
    private const int MeterTop = 58;
    private const int MeterHeight = 46;
    private const int ReadoutTop = 16;
    private const int ReadoutColumnWidth = 108;
    private const int ReadoutColumns = 3;
    private const int ReadoutBlockWidth = ReadoutColumnWidth * ReadoutColumns;

    private readonly CadenceMeter _meter = new();
    private readonly FlatButton _test = new();
    private readonly FlatButton _start = new();
    private readonly FlatButton _pause = new();
    private readonly FlatButton _stop = new();

    private RunPhase _phase = RunPhase.Running;
    private bool _running;
    private long _iteration;
    private TimeSpan _elapsed;

    /// <summary>
    /// When <see cref="_elapsed"/> was last set. The runner reports progress once per
    /// iteration, so between two reports — which a long delay can stretch to minutes — that
    /// figure is the only thing the clock has, and it would sit frozen. The repaint timer
    /// already ticks ten times a second; this is what gives it something new to draw.
    /// </summary>
    private long _elapsedAt = Stopwatch.GetTimestamp();

    private int _countdown;
    private long? _target;
    private TimeSpan? _window;
    private string _message = string.Empty;
    private TextRole _messageRole = TextRole.Secondary;

    public RunPanel()
    {
        Title = string.Empty;

        Height = PanelHeight;
        _meter.SetBounds(Edge, MeterTop, 300, MeterHeight);
        Controls.Add(_meter);

        Configure(_test, ButtonKind.Secondary);
        Configure(_start, ButtonKind.Primary);
        Configure(_pause, ButtonKind.Secondary);
        Configure(_stop, ButtonKind.Secondary);

        _test.Click += (_, _) => TestRequested?.Invoke(this, EventArgs.Empty);
        _start.Click += (_, _) => StartRequested?.Invoke(this, EventArgs.Empty);
        _pause.Click += (_, _) => PauseRequested?.Invoke(this, EventArgs.Empty);
        _stop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);

        Controls.AddRange(new Control[] { _test, _start, _pause, _stop });
    }

    /// <summary>Fires exactly one iteration of the configured action, without starting a run.</summary>
    public event EventHandler? TestRequested;

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
        _elapsedAt = Stopwatch.GetTimestamp();
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
        _elapsedAt = Stopwatch.GetTimestamp();
        _countdown = progress.CountdownSeconds;
        Refresh(idleMessage: null);
    }

    /// <summary>
    /// Moves between running and paused straight away, leaving the counters alone.
    ///
    /// The run reports its own phase, but only once it reaches the top of the next iteration —
    /// on the far side of a delay that can be minutes long — so a pause would otherwise sit
    /// there looking like it had not registered. Reporting a whole <see cref="RunProgress"/>
    /// from here instead is what this replaces, and that carried an iteration count and an
    /// elapsed time the caller does not actually know, blanking both readouts until the run
    /// caught up and refilled them.
    /// </summary>
    public void SetPhase(RunPhase phase)
    {
        if (!_running || _phase == phase)
        {
            return;
        }

        // Bank the time shown since the last report before the clock stops, so pausing settles
        // on the figure that was on screen rather than snapping backwards to the last report.
        _elapsed = LiveElapsed();
        _elapsedAt = Stopwatch.GetTimestamp();
        _phase = phase;
        Refresh(idleMessage: null);
    }

    /// <summary>
    /// The elapsed figure to draw: the last one reported, carried forward to now while the run
    /// is actually running. A paused or finished run holds still, which is the truth in both
    /// cases — the runner's own clock stops with it.
    /// </summary>
    private TimeSpan LiveElapsed() => _running && _phase == RunPhase.Running
        ? _elapsed + Stopwatch.GetElapsedTime(_elapsedAt)
        : _elapsed;

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

    public void SetTestEnabled(bool enabled) => _test.Enabled = enabled;

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

    /// <summary>
    /// This panel's own measurements, in the pixels this display actually uses. Every constant
    /// above is a 96-DPI design value, and both the layout below and the painting further down
    /// run long after the framework's one-time rescale — see <see cref="Theme.Scale"/>.
    /// </summary>
    private int S(int value) => Theme.Scale(value, this);

    private void LayoutChildren()
    {
        int edge = S(Edge);
        int buttonWidth = S(ButtonWidth);
        int gap = S(ButtonGap);
        int buttonHeight = S(ButtonHeight);
        int top = S(ButtonTop);
        int right = Width - edge;

        _stop.SetBounds(right - buttonWidth, top, buttonWidth, buttonHeight);
        _pause.SetBounds(_stop.Left - gap - buttonWidth, top, buttonWidth, buttonHeight);
        _start.SetBounds(_pause.Left - gap - buttonWidth, top, buttonWidth, buttonHeight);
        _test.SetBounds(_start.Left - gap - S(TestButtonWidth), top, S(TestButtonWidth), buttonHeight);

        // The meter takes whatever the buttons leave, which keeps the rhythm strip as wide
        // as the window allows.
        int meterRight = _test.Left - S(24);
        _meter.SetBounds(edge, S(MeterTop), Math.Max(S(80), meterRight - edge), S(MeterHeight));
    }

    private void DrawStatusLine(Graphics g)
    {
        // A lamp, not a label: colour carries the state and the words only name it. Held —
        // paused by the user or waiting on the target window — reads the same as idle: nothing
        // is actually happening right now, whatever the reason.
        bool held = _phase is RunPhase.Paused or RunPhase.WaitingForWindow;
        var lamp = new Rectangle(S(21), S(24), S(9), S(9));
        Color lampColor = !_running ? Theme.Disabled
            : held ? Theme.TextSecondary
            : Theme.Accent;

        using (var brush = new SolidBrush(lampColor))
        {
            g.FillEllipse(brush, lamp);
        }

        if (_running && !held)
        {
            using var halo = new SolidBrush(Color.FromArgb(48, lampColor));
            g.FillEllipse(halo, Rectangle.Inflate(lamp, S(4), S(4)));
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
        int available = Width - S(38) - S(ReadoutBlockWidth) - S(Edge) - S(16);
        TextRenderer.DrawText(g, _message, Theme.Base,
            new Rectangle(S(38), S(18), Math.Max(S(60), available), S(22)), text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void DrawReadouts(Graphics g)
    {
        int top = S(ReadoutTop);
        int columnWidth = S(ReadoutColumnWidth);
        int right = Width - S(Edge);

        TimeSpan elapsed = LiveElapsed();
        string rate = elapsed.TotalSeconds > 0.25 && _iteration > 0
            ? (_iteration / elapsed.TotalSeconds).ToString("0.0") + "/s"
            : "—";

        var columns = new (string Label, string Value)[]
        {
            (Loc.IterationsCaption, _target is { } total ? $"{_iteration:N0} / {total:N0}" : $"{_iteration:N0}"),
            (Loc.ElapsedCaption, Format(elapsed)),
            (Loc.RateCaption, rate),
        };

        // Right-aligned as a block, so the numbers sit in a stable column instead of drifting
        // as the values change width.
        int x = right;
        for (int i = columns.Length - 1; i >= 0; i--)
        {
            x -= columnWidth;

            TextRenderer.DrawText(g, columns[i].Label, Theme.Caption,
                new Rectangle(x, top, columnWidth, S(14)), Theme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, columns[i].Value, Theme.Mono,
                new Rectangle(x, top + S(15), columnWidth, S(20)),
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
            return (float)(LiveElapsed().TotalMilliseconds / window.TotalMilliseconds);
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
                RunPhase.CountingDown => (Loc.StartingIn(_countdown), TextRole.Accent),
                RunPhase.Paused => (Loc.PausedStatus, TextRole.Primary),
                RunPhase.WaitingForWindow => (Loc.WaitingForWindow, TextRole.Primary),
                _ => (Loc.Running, TextRole.Accent),
            };
        }

        _pause.Text = _phase == RunPhase.Paused && _running ? Loc.Resume : Loc.Pause;
        _pause.Enabled = _running;
        _pause.Kind = _phase == RunPhase.Paused && _running ? ButtonKind.Primary : ButtonKind.Secondary;
        _stop.Enabled = _running;
        _stop.Kind = _running ? ButtonKind.Danger : ButtonKind.Secondary;
        _start.Enabled = !_running;
        _test.Enabled = !_running;

        Invalidate();
    }

    private static string Format(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{value.Minutes:00}:{value.Seconds:00}";

    private static void Configure(FlatButton button, ButtonKind kind)
    {
        button.Kind = kind;
    }

    public void ApplyLanguage()
    {
        _test.Text = Loc.Test;
        _start.Text = Loc.Start;
        _stop.Text = Loc.Stop;
        _pause.Text = _phase == RunPhase.Paused && _running ? Loc.Resume : Loc.Pause;

        // Anything phase-derived corrects itself on the next progress report; the idle
        // message would not get one, so it is reset here instead.
        if (!_running)
        {
            _message = Loc.Ready;
            _messageRole = TextRole.Secondary;
        }

        Invalidate();
    }
}
