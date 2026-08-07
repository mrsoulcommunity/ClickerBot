namespace ClickerApp;

/// <summary>
/// A named set of automation settings. Everything the user can configure lives here,
/// which is what makes profiles a straight save/load of this object.
/// </summary>
internal sealed class Profile
{
    public string Name { get; set; } = "New profile";

    /// <summary>The keyboard key that gets pressed each iteration.</summary>
    public Keys Key { get; set; } = Keys.None;

    public int ClickX { get; set; }

    public int ClickY { get; set; }

    /// <summary>Delay between the key press and the mouse click.</summary>
    public DelaySetting KeyDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };

    /// <summary>Delay after the mouse click, i.e. between one click and the next.</summary>
    public DelaySetting ClickDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };

    public bool InfiniteLoop { get; set; }

    public int Repetitions { get; set; } = 10;

    public Keys StartHotkey { get; set; } = Keys.F7;

    public Keys StopHotkey { get; set; } = Keys.F8;

    [System.Text.Json.Serialization.JsonIgnore]
    public Point ClickPoint => new(ClickX, ClickY);

    public Profile Clone() => new()
    {
        Name = Name,
        Key = Key,
        ClickX = ClickX,
        ClickY = ClickY,
        KeyDelay = KeyDelay.Clone(),
        ClickDelay = ClickDelay.Clone(),
        InfiniteLoop = InfiniteLoop,
        Repetitions = Repetitions,
        StartHotkey = StartHotkey,
        StopHotkey = StopHotkey,
    };

    public override string ToString() => Name;
}
