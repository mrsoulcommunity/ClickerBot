using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Thin wrapper around the Win32 SendInput API for synthesizing keyboard and mouse input.
/// </summary>
internal static class NativeInput
{
    public static void PressKey(Keys key)
    {
        // A stored key can carry modifier bits alongside the key code; only the low byte is
        // a virtual-key, and sending the raw value would synthesize a different key entirely.
        key &= Keys.KeyCode;
        if (key == Keys.None)
        {
            return;
        }

        ushort virtualKey = (ushort)key;
        ushort scanCode = (ushort)MapVirtualKey(virtualKey, MAPVK_VK_TO_VSC);
        uint extended = IsExtendedKey(key) ? KEYEVENTF_EXTENDEDKEY : 0;

        var inputs = new[]
        {
            CreateKeyInput(virtualKey, scanCode, extended),
            CreateKeyInput(virtualKey, scanCode, extended | KEYEVENTF_KEYUP),
        };

        Send(inputs);
    }

    /// <summary>
    /// Types <paramref name="text"/> via Unicode input events rather than key presses, so any
    /// character an editor could display can be typed regardless of the active keyboard
    /// layout — no <see cref="Keys"/> value or virtual-key mapping is involved. A character
    /// outside the Basic Multilingual Plane is already a surrogate pair in a .NET string, and
    /// SendInput's documented handling of one Unicode event per UTF-16 code unit takes care of
    /// it without any extra logic here.
    /// </summary>
    public static void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var inputs = new INPUT[text.Length * 2];
        for (int i = 0; i < text.Length; i++)
        {
            ushort code = text[i];
            inputs[i * 2] = CreateUnicodeInput(code, KEYEVENTF_UNICODE);
            inputs[(i * 2) + 1] = CreateUnicodeInput(code, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);
        }

        Send(inputs);
    }

    /// <summary>
    /// Clicks <paramref name="button"/>, moving the cursor to <paramref name="screenPoint"/>
    /// first when one is given. A null point clicks wherever the cursor already is, which
    /// leaves the user free to aim the run by hand.
    /// </summary>
    public static void Click(ClickButton button, Point? screenPoint)
    {
        // Checked rather than assumed: clicking after a failed move would fire the click
        // wherever the cursor happens to be sitting, which is the one thing worse than not
        // clicking at all.
        if (screenPoint is { } point && !SetCursorPos(point.X, point.Y))
        {
            throw new InvalidOperationException(
                $"Could not move the cursor to {point.X}, {point.Y} " +
                $"(error {Marshal.GetLastWin32Error()}).");
        }

        var (down, up) = button switch
        {
            ClickButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            ClickButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
        };

        Send(new[] { CreateMouseInput(down), CreateMouseInput(up) });
    }

    private static void Send(INPUT[] inputs)
    {
        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException(
                $"SendInput failed (error {Marshal.GetLastWin32Error()}). " +
                "Input may be blocked by an elevated window.");
        }
    }

    private static INPUT CreateKeyInput(ushort virtualKey, ushort scanCode, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = virtualKey,
                wScan = scanCode,
                dwFlags = flags,
            },
        },
    };

    private static INPUT CreateUnicodeInput(ushort code, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = code,
                dwFlags = flags,
            },
        },
    };

    private static INPUT CreateMouseInput(uint flags) => new()
    {
        type = INPUT_MOUSE,
        u = new InputUnion
        {
            mi = new MOUSEINPUT { dwFlags = flags },
        },
    };

    /// <summary>Keys that live on the extended part of the keyboard and need the extended flag.</summary>
    private static bool IsExtendedKey(Keys key) => key is
        Keys.Insert or Keys.Delete or Keys.Home or Keys.End or
        Keys.PageUp or Keys.PageDown or
        Keys.Left or Keys.Right or Keys.Up or Keys.Down or
        Keys.NumLock or Keys.PrintScreen or Keys.Divide or
        Keys.RControlKey or Keys.RMenu;

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    private const uint MAPVK_VK_TO_VSC = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);
}
