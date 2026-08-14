using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Captures real mouse and keyboard input as an equivalent <see cref="MacroStep"/> sequence,
/// through global low-level hooks — so it sees input aimed at any window, not just this one.
///
/// Purely an observer: the hook procedures always call <see cref="CallNextHookEx"/> and never
/// swallow an event, so recording never blocks the very input it is watching. A <see cref="Wait"/>
/// step is inserted ahead of each captured action sized to the real time since the previous one,
/// so playing the recording back reproduces the pacing it was recorded at, not just the actions.
/// </summary>
internal sealed class MacroRecorder : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;

    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;

    /// <summary>A real click's press and release rarely land on the exact same pixel; only a move past this counts as a drag.</summary>
    private const int DragThresholdPixels = 6;

    // Kept as fields rather than local lambdas: SetWindowsHookEx stores the delegate's address
    // in Windows, and nothing else in this process keeps the delegate itself alive — an
    // uncollected reference would let the GC free it while the hook is still installed.
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;

    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private readonly Stopwatch _clock = new();
    private long _lastEventMs;
    private Point? _mouseDownAt;
    private ClickButton _mouseDownButton;

    public MacroRecorder()
    {
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    public bool IsRecording => _keyboardHook != IntPtr.Zero;

    /// <summary>
    /// Raised for each step captured, on the thread that called <see cref="Start"/> — the low
    /// level hook callback runs on the installing thread's own message loop, which for this app
    /// is always the UI thread, so no further marshaling is needed here.
    /// </summary>
    public event Action<MacroStep>? StepCaptured;

    public void Start()
    {
        if (IsRecording)
        {
            return;
        }

        IntPtr module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);

        _clock.Restart();
        _lastEventMs = 0;
        _mouseDownAt = null;
    }

    public void Stop()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        _clock.Stop();
        _mouseDownAt = null;
    }

    public void Dispose() => Stop();

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();
            if (message == WmKeyDown || message == WmSysKeyDown)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                Emit(new MacroStep { Kind = StepKind.KeyPress, Key = (Keys)data.vkCode });
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var point = new Point(data.pt.X, data.pt.Y);

            switch (wParam.ToInt32())
            {
                case WmLButtonDown: _mouseDownAt = point; _mouseDownButton = ClickButton.Left; break;
                case WmRButtonDown: _mouseDownAt = point; _mouseDownButton = ClickButton.Right; break;
                case WmMButtonDown: _mouseDownAt = point; _mouseDownButton = ClickButton.Middle; break;

                case WmLButtonUp: EndPress(point, ClickButton.Left); break;
                case WmRButtonUp: EndPress(point, ClickButton.Right); break;
                case WmMButtonUp: EndPress(point, ClickButton.Middle); break;
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void EndPress(Point point, ClickButton button)
    {
        if (_mouseDownAt is not { } down || button != _mouseDownButton)
        {
            _mouseDownAt = null;
            return;
        }

        _mouseDownAt = null;

        double dx = point.X - down.X;
        double dy = point.Y - down.Y;
        if (Math.Sqrt((dx * dx) + (dy * dy)) > DragThresholdPixels)
        {
            long durationMs = _clock.ElapsedMilliseconds - _lastEventMs;
            Emit(new MacroStep
            {
                Kind = StepKind.Drag,
                Button = button,
                X = down.X,
                Y = down.Y,
                DragToX = point.X,
                DragToY = point.Y,
                DragDurationMs = (int)Math.Clamp(durationMs, Limits.MinDragDurationMs, Limits.MaxDragDurationMs),
            });
        }
        else
        {
            Emit(new MacroStep
            {
                Kind = StepKind.Click,
                Button = button,
                Target = ClickTarget.FixedPoint,
                X = down.X,
                Y = down.Y,
            });
        }
    }

    private void Emit(MacroStep step)
    {
        long now = _clock.ElapsedMilliseconds;
        long gap = now - _lastEventMs;
        _lastEventMs = now;

        // A gap under this is indistinguishable from the natural jitter between two halves of
        // one gesture (a key's own down/up, a click immediately following its own mouse-down),
        // and would just clutter the sequence with waits nobody meant to record.
        if (gap > 20)
        {
            StepCaptured?.Invoke(new MacroStep
            {
                Kind = StepKind.Wait,
                Delay = new DelaySetting { Fixed = (int)Math.Clamp(gap, Limits.MinDelayMs, Limits.MaxDelayMs) },
            });
        }

        StepCaptured?.Invoke(step);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
