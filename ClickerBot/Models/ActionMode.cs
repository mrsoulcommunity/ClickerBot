namespace ClickerBot;

/// <summary>What one iteration actually does.</summary>
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

/// <summary>Human-friendly names for <see cref="ActionMode"/>, shared by the history list and the remote status feed.</summary>
internal static class ActionModeNames
{
    public static string Describe(ActionMode mode) => mode switch
    {
        ActionMode.KeyAndClick => Loc.IsPersian ? "کلید + کلیک" : "Key + click",
        ActionMode.KeyOnly => Loc.IsPersian ? "فقط کلید" : "Key only",
        ActionMode.ClickOnly => Loc.IsPersian ? "فقط کلیک" : "Click only",
        ActionMode.TypeText => Loc.IsPersian ? "تایپ متن" : "Type text",
        _ => mode.ToString(),
    };
}

/// <summary>Which mouse button an iteration clicks with.</summary>
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
