using System.Runtime.InteropServices;

namespace ClickerApp;

/// <summary>
/// Registers system-wide hotkeys under a name (e.g. "start", "stop") so they can be
/// reassigned at any time, and raises <see cref="HotkeyPressed"/> on the UI thread.
/// </summary>
internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly Dictionary<string, int> _idsByName = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> _namesById = new();
    private int _nextId = 1;

    public HotkeyManager(Form owner)
    {
        AssignHandle(owner.Handle);
    }

    /// <summary>Raised with the name passed to <see cref="Assign"/> when that hotkey fires.</summary>
    public event Action<string>? HotkeyPressed;

    /// <summary>
    /// Assigns <paramref name="key"/> to <paramref name="name"/>, replacing any previous
    /// binding for that name. <see cref="Keys.None"/> just clears it.
    /// </summary>
    /// <returns>False if Windows refused the key because another app already owns it.</returns>
    public bool Assign(string name, Keys key)
    {
        Release(name);

        if (key == Keys.None)
        {
            return true;
        }

        int id = _nextId++;
        if (!RegisterHotKey(Handle, id, MOD_NOREPEAT, (uint)key))
        {
            return false;
        }

        _idsByName[name] = id;
        _namesById[id] = name;
        return true;
    }

    public void Release(string name)
    {
        if (_idsByName.Remove(name, out int id))
        {
            UnregisterHotKey(Handle, id);
            _namesById.Remove(id);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && _namesById.TryGetValue(m.WParam.ToInt32(), out string? name))
        {
            HotkeyPressed?.Invoke(name);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        foreach (int id in _idsByName.Values)
        {
            UnregisterHotKey(Handle, id);
        }

        _idsByName.Clear();
        _namesById.Clear();
        ReleaseHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
