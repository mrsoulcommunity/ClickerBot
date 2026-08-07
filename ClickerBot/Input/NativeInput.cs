using System.Runtime.InteropServices;

namespace ClickerBot;

/// <summary>
/// Thin wrapper around the Win32 SendInput API for synthesizing keyboard and mouse input.
/// </summary>
internal static class NativeInput
{
    public static void PressKey(Keys key)
    {
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

    public static void LeftClick(Point screenPoint)
    {
        SetCursorPos(screenPoint.X, screenPoint.Y);

        var inputs = new[]
        {
            CreateMouseInput(MOUSEEVENTF_LEFTDOWN),
            CreateMouseInput(MOUSEEVENTF_LEFTUP),
        };

        Send(inputs);
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

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

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
