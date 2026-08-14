namespace ClickerBot;

/// <summary>
/// A named macro: an ordered list of steps, how many times it repeats, its hotkeys, and
/// optionally the one window it is allowed to run against. Everything the user can configure
/// lives here, which is what makes profiles a straight save/load of this object.
/// </summary>
internal sealed class Profile
{
    public string Name { get; set; } = Loc.NewProfile;

    /// <summary>The macro itself: what happens, in order, on every iteration.</summary>
    public List<MacroStep> Steps { get; set; } = new();

    /// <summary>Countdown before the first iteration, to give you time to change windows.</summary>
    public int StartDelaySeconds { get; set; } = 3;

    public RepeatMode Repeat { get; set; } = RepeatMode.Count;

    public int Repetitions { get; set; } = 100;

    public int DurationMinutes { get; set; } = 5;

    /// <summary>
    /// Restricts the macro to running only while a particular window is in front. Off by
    /// default — most profiles are meant to be aimed by hand the way they always were.
    /// </summary>
    public bool RequireTargetWindow { get; set; }

    /// <summary>A case-insensitive substring match against the foreground window's title.</summary>
    public string TargetWindowTitle { get; set; } = string.Empty;

    public Keys StartHotkey { get; set; } = Keys.F7;

    public Keys PauseHotkey { get; set; } = Keys.F8;

    public Keys StopHotkey { get; set; } = Keys.F9;

    public Keys PickHotkey { get; set; } = Keys.F10;

    // --- Legacy single-action fields ------------------------------------
    //
    // Read once by Normalize to build an equivalent Steps list for a profile saved before
    // macros existed, then never read again. Kept only so that file — and the "New profile"
    // default, which is really just an empty profile going through the same migration path —
    // still deserializes and still produces a first step to look at instead of a blank list.

    public ActionMode Mode { get; set; } = ActionMode.KeyAndClick;
    public Keys Key { get; set; } = Keys.None;
    public ClickButton Button { get; set; } = ClickButton.Left;
    public bool DoubleClick { get; set; }
    public ClickTarget Target { get; set; } = ClickTarget.FixedPoint;
    public string Text { get; set; } = string.Empty;
    public int ClickX { get; set; }
    public int ClickY { get; set; }
    public int Scatter { get; set; }
    public DelaySetting KeyDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };
    public DelaySetting ClickDelay { get; set; } = new() { Fixed = 100, Min = 80, Max = 150 };

    public Profile Clone() => new()
    {
        Name = Name,
        Steps = Steps.Select(s => s.Clone()).ToList(),
        StartDelaySeconds = StartDelaySeconds,
        Repeat = Repeat,
        Repetitions = Repetitions,
        DurationMinutes = DurationMinutes,
        RequireTargetWindow = RequireTargetWindow,
        TargetWindowTitle = TargetWindowTitle,
        StartHotkey = StartHotkey,
        PauseHotkey = PauseHotkey,
        StopHotkey = StopHotkey,
        PickHotkey = PickHotkey,
    };

    /// <summary>
    /// Pulls every value back inside <see cref="Limits"/>, replaces anything a JSON file left
    /// null, and — the one migration this app will ever need to do — turns a pre-macro
    /// profile's single action into the equivalent Steps list the first time it is loaded.
    /// Without this a hand-edited (or half-written) profile can reach the UI in a state it
    /// cannot display, and an old profiles.json would open to an empty, confusing macro instead
    /// of the one action it always ran.
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = Loc.UntitledProfileName;
        }

        Repeat = Enum.IsDefined(Repeat) ? Repeat : RepeatMode.Count;
        Repetitions = Math.Clamp(Repetitions, Limits.MinRepetitions, Limits.MaxRepetitions);
        DurationMinutes = Math.Clamp(DurationMinutes, Limits.MinDurationMinutes, Limits.MaxDurationMinutes);
        StartDelaySeconds = Math.Clamp(
            StartDelaySeconds, Limits.MinStartDelaySeconds, Limits.MaxStartDelaySeconds);
        TargetWindowTitle ??= string.Empty;

        if (Steps is null || Steps.Count == 0)
        {
            Steps = BuildLegacyMigrationSteps();
        }

        Steps.RemoveAll(step => step is null);
        if (Steps.Count > Limits.MaxSteps)
        {
            Steps.RemoveRange(Limits.MaxSteps, Steps.Count - Limits.MaxSteps);
        }

        foreach (var step in Steps)
        {
            step.Normalize();
        }
    }

    /// <summary>
    /// Reconstructs the equivalent step sequence for whatever this profile's legacy single
    /// action used to be: the key (if any), the pace between the key and the click, the click
    /// (if any) or the typed text, then the pace between one iteration and the next — the exact
    /// order <c>AutomationRunner</c> used to run them in, now expressed as steps instead of a
    /// mode switch. A brand new profile goes through this same path with its untouched
    /// property-initializer defaults, which is what gives it a sensible first step to look at
    /// rather than an empty list.
    /// </summary>
    private List<MacroStep> BuildLegacyMigrationSteps()
    {
        var steps = new List<MacroStep>();

        switch (Mode)
        {
            case ActionMode.KeyAndClick:
                steps.Add(new MacroStep { Kind = StepKind.KeyPress, Key = Key });
                steps.Add(new MacroStep { Kind = StepKind.Wait, Delay = (KeyDelay ?? new DelaySetting()).Clone() });
                steps.Add(BuildLegacyClickStep());
                break;

            case ActionMode.KeyOnly:
                steps.Add(new MacroStep { Kind = StepKind.KeyPress, Key = Key });
                break;

            case ActionMode.ClickOnly:
                steps.Add(BuildLegacyClickStep());
                break;

            case ActionMode.TypeText:
                steps.Add(new MacroStep { Kind = StepKind.TypeText, Text = Text ?? string.Empty });
                break;
        }

        steps.Add(new MacroStep { Kind = StepKind.Wait, Delay = (ClickDelay ?? new DelaySetting()).Clone() });
        return steps;
    }

    private MacroStep BuildLegacyClickStep() => new()
    {
        Kind = StepKind.Click,
        Button = Button,
        DoubleClick = DoubleClick,
        Target = Target,
        X = ClickX,
        Y = ClickY,
        Scatter = Scatter,
    };

    public override string ToString() => Name;
}
