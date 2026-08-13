namespace ClickerBot;

/// <summary>
/// A named set of automation settings. Everything the user can configure lives here,
/// which is what makes profiles a straight save/load of this object.
/// </summary>
internal sealed class Profile
{
    public string Name { get; set; } = "New profile";

    /// <summary>Whether an iteration presses a key, clicks, or does both.</summary>
    public ActionMode Mode { get; set; } = ActionMode.KeyAndClick;

    /// <summary>The keyboard key that gets pressed each iteration.</summary>
    public Keys Key { get; set; } = Keys.None;

    public ClickButton Button { get; set; } = ClickButton.Left;

    /// <summary>Send a second click straight after the first, as a double-click.</summary>
    public bool DoubleClick { get; set; }

    public ClickTarget Target { get; set; } = ClickTarget.FixedPoint;

    /// <summary>The line <see cref="ActionMode.TypeText"/> types each iteration.</summary>
    public string Text { get; set; } = string.Empty;

    public int ClickX { get; set; }

    public int ClickY { get; set; }

    /// <summary>
    /// Radius in pixels of a random offset applied to every click. Zero hits the exact
    /// coordinate every time, which is the giveaway that a machine is doing the clicking.
    /// </summary>
    public int Scatter { get; set; }

    /// <summary>Delay between the key press and the mouse click.</summary>
    public DelaySetting KeyDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };

    /// <summary>Delay after the mouse click, i.e. between one click and the next.</summary>
    public DelaySetting ClickDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };

    /// <summary>Countdown before the first iteration, to give you time to change windows.</summary>
    public int StartDelaySeconds { get; set; } = 3;

    public RepeatMode Repeat { get; set; } = RepeatMode.Count;

    public int Repetitions { get; set; } = 100;

    public int DurationMinutes { get; set; } = 5;

    public Keys StartHotkey { get; set; } = Keys.F7;

    public Keys PauseHotkey { get; set; } = Keys.F8;

    public Keys StopHotkey { get; set; } = Keys.F9;

    public Keys PickHotkey { get; set; } = Keys.F10;

    [System.Text.Json.Serialization.JsonIgnore]
    public Point ClickPoint => new(ClickX, ClickY);

    // Written as explicit positive lists rather than a negation of the other modes: with more
    // than two modes, "not ClickOnly" and "the modes that use a key" stop being the same set,
    // and a negation would silently start claiming TypeText needs a key.

    /// <summary>True when this profile presses a key, which is what makes the key field matter.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UsesKey => Mode is ActionMode.KeyAndClick or ActionMode.KeyOnly;

    /// <summary>True when this profile clicks, which is what makes the mouse settings matter.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UsesMouse => Mode is ActionMode.KeyAndClick or ActionMode.ClickOnly;

    /// <summary>True when this profile types text, which is what makes the text field matter.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UsesText => Mode == ActionMode.TypeText;

    public Profile Clone() => new()
    {
        Name = Name,
        Mode = Mode,
        Key = Key,
        Button = Button,
        DoubleClick = DoubleClick,
        Target = Target,
        Text = Text,
        ClickX = ClickX,
        ClickY = ClickY,
        Scatter = Scatter,
        KeyDelay = KeyDelay.Clone(),
        ClickDelay = ClickDelay.Clone(),
        StartDelaySeconds = StartDelaySeconds,
        Repeat = Repeat,
        Repetitions = Repetitions,
        DurationMinutes = DurationMinutes,
        StartHotkey = StartHotkey,
        PauseHotkey = PauseHotkey,
        StopHotkey = StopHotkey,
        PickHotkey = PickHotkey,
    };

    /// <summary>
    /// Pulls every value back inside <see cref="Limits"/>, and replaces anything a JSON file
    /// left null. Without this a hand-edited (or half-written) profile can reach the UI in a
    /// state it cannot display — a repetition count of 0 would "run" and finish instantly,
    /// and a missing delay would be a null reference the moment the editor is filled in.
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = "Untitled";
        }

        Mode = Enum.IsDefined(Mode) ? Mode : ActionMode.KeyAndClick;
        Button = Enum.IsDefined(Button) ? Button : ClickButton.Left;
        Target = Enum.IsDefined(Target) ? Target : ClickTarget.FixedPoint;
        Repeat = Enum.IsDefined(Repeat) ? Repeat : RepeatMode.Count;

        if (Text is null)
        {
            Text = string.Empty;
        }
        else if (Text.Length > Limits.MaxTypedTextLength)
        {
            Text = Text[..Limits.MaxTypedTextLength];
        }

        ClickX = Math.Clamp(ClickX, Limits.MinCoordinate, Limits.MaxCoordinate);
        ClickY = Math.Clamp(ClickY, Limits.MinCoordinate, Limits.MaxCoordinate);
        Scatter = Math.Clamp(Scatter, Limits.MinScatter, Limits.MaxScatter);
        Repetitions = Math.Clamp(Repetitions, Limits.MinRepetitions, Limits.MaxRepetitions);
        DurationMinutes = Math.Clamp(DurationMinutes, Limits.MinDurationMinutes, Limits.MaxDurationMinutes);
        StartDelaySeconds = Math.Clamp(
            StartDelaySeconds, Limits.MinStartDelaySeconds, Limits.MaxStartDelaySeconds);

        KeyDelay ??= new DelaySetting();
        ClickDelay ??= new DelaySetting();
        KeyDelay.Normalize();
        ClickDelay.Normalize();
    }

    public override string ToString() => Name;
}
