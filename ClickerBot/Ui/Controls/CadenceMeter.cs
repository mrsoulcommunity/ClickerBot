using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>
/// A live strip of the run's rhythm: one tick per completed iteration, laid out on a real
/// time axis with "now" at the right edge and older ticks sliding left until they fall off.
///
/// This is the one thing in the window that shows what the tool actually does. A counter
/// tells you a run is progressing; the spacing here tells you how it is progressing — an even
/// comb for a fixed delay, a ragged one for a random range — and it does that from across the
/// desk, without reading a single number. Which matters, because this window spends most of
/// its life behind the window being automated.
/// </summary>
internal sealed class CadenceMeter : Control, IThemedControl
{
    /// <summary>Ticks older than the visible window are never drawn, so the buffer is small.</summary>
    private const int Capacity = 512;

    private const int MinWindowMs = 1_500;
    private const int MaxWindowMs = 120_000;

    private readonly long[] _ticks = new long[Capacity];
    private int _count;
    private int _next;

    private long _windowMs = 6_000;
    private bool _live;

    public CadenceMeter()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = Theme.Base;
        Size = new Size(400, 46);
    }

    /// <summary>
    /// Whether the strip is showing a run in progress. A stopped run keeps its last ticks on
    /// screen, dimmed, rather than blanking the moment you press Stop.
    /// </summary>
    public bool Live
    {
        get => _live;
        set
        {
            if (_live == value)
            {
                return;
            }

            _live = value;
            Invalidate();
        }
    }

    /// <summary>Records one completed iteration at the current moment.</summary>
    public void Mark()
    {
        long now = Environment.TickCount64;

        _ticks[_next] = now;
        _next = (_next + 1) % Capacity;
        _count = Math.Min(_count + 1, Capacity);

        RescaleWindow(now);
    }

    /// <summary>Clears the strip for a fresh run.</summary>
    public void Reset()
    {
        _count = 0;
        _next = 0;
        _windowMs = 6_000;
        Invalidate();
    }

    public void ApplyTheme() => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackdropOf(this));

        if (Width <= 4 || Height <= 4)
        {
            return;
        }

        var bed = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Theme.RoundedRect(bed, 8))
        {
            using var fill = new SolidBrush(Theme.Track);
            g.FillPath(fill, path);

            using var pen = new Pen(Theme.Border);
            g.DrawPath(pen, path);
        }

        // The baseline doubles as the empty state: a run that has not started yet is a flat
        // line waiting for something to happen, which is exactly what it is.
        int baseline = bed.Bottom - 9;
        using (var pen = new Pen(Theme.Border))
        {
            g.DrawLine(pen, 8, baseline, bed.Right - 8, baseline);
        }

        DrawTicks(g, bed, baseline);
    }

    private void DrawTicks(Graphics g, Rectangle bed, int baseline)
    {
        if (_count == 0)
        {
            TextRenderer.DrawText(g, "waiting for the first iteration", Theme.MonoSmall, bed,
                Theme.Disabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        long now = Environment.TickCount64;
        int left = bed.Left + 8;
        int right = bed.Right - 8;
        int span = right - left;
        if (span <= 0)
        {
            return;
        }

        int top = bed.Top + 9;
        int height = baseline - top;
        if (height <= 2)
        {
            return;
        }

        Color live = Theme.Accent;
        Color dormant = Theme.Disabled;

        for (int i = 0; i < _count; i++)
        {
            // Walk newest first so the loop can stop as soon as it leaves the window.
            int index = ((_next - 1 - i) % Capacity + Capacity) % Capacity;
            long age = now - _ticks[index];
            if (age < 0 || age > _windowMs)
            {
                break;
            }

            float position = 1f - ((float)age / _windowMs);
            int x = left + (int)(span * position);

            // Older ticks fade out, so the eye is pulled to the leading edge where the run is.
            int alpha = (int)(235 * Math.Pow(position, 0.65));
            alpha = Math.Clamp(alpha, 18, 255);

            using var pen = new Pen(Color.FromArgb(alpha, _live ? live : dormant), 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };

            // The newest tick stands full height; the rest sit shorter so the leading edge
            // reads as the current beat rather than one stripe among many.
            int tickHeight = i == 0 ? height : (int)(height * 0.62f);
            g.DrawLine(pen, x, baseline - tickHeight, x, baseline - 1);
        }
    }

    /// <summary>
    /// Keeps roughly a dozen beats on screen whatever the pace, so a 20 ms loop and a 5 s loop
    /// both read as a rhythm rather than a solid block or a single lonely mark.
    /// </summary>
    private void RescaleWindow(long now)
    {
        if (_count < 2)
        {
            return;
        }

        int previous = ((_next - 2) % Capacity + Capacity) % Capacity;
        long interval = Math.Max(1, now - _ticks[previous]);
        long target = Math.Clamp(interval * 12, MinWindowMs, MaxWindowMs);

        // Eased rather than snapped: a single slow iteration should not make the whole strip
        // jump scale under the user's eye.
        _windowMs += (target - _windowMs) / 4;
        _windowMs = Math.Clamp(_windowMs, MinWindowMs, MaxWindowMs);
    }
}
