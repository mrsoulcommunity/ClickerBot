namespace ClickerBot;

/// <summary>
/// What a pre-macro profile's single action used to be. Read once by
/// <see cref="Profile.Normalize"/> to build the equivalent <see cref="MacroStep"/> sequence for
/// a profile saved before macros existed, then never read again — see <see cref="Profile"/>'s
/// legacy fields.
/// </summary>
internal enum ActionMode
{
    /// <summary>Press the key, wait, then click. The original behaviour.</summary>
    KeyAndClick,

    /// <summary>Press the key only — no mouse involved, so the cursor is left alone.</summary>
    KeyOnly,

    /// <summary>Click only — a plain auto-clicker, with no key to choose.</summary>
    ClickOnly,

    /// <summary>Types a fixed line of text — no single key, no click.</summary>
    TypeText,
}

/// <summary>Which mouse button a step clicks or drags with.</summary>
internal enum ClickButton
{
    Left,
    Right,
    Middle,
}

/// <summary>How a run decides it is finished.</summary>
internal enum RepeatMode
{
    /// <summary>Stop after a set number of iterations.</summary>
    Count,

    /// <summary>Stop after a set amount of time.</summary>
    Duration,

    /// <summary>Run until stopped by hand.</summary>
    Forever,
}

/// <summary>Where the click lands.</summary>
internal enum ClickTarget
{
    /// <summary>A fixed screen coordinate; the cursor is moved there for every click.</summary>
    FixedPoint,

    /// <summary>Wherever the cursor already is, so you can steer the run by hand.</summary>
    CursorPosition,
}
