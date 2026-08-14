using System.Text.Json.Serialization;

namespace ClickerBot;

/// <summary>What one step in a macro does.</summary>
internal enum StepKind
{
    KeyPress,
    Click,
    Drag,
    TypeText,
    Wait,
    WaitForPixelColor,
    ClipboardSet,
    ClipboardPaste,
}

/// <summary>
/// One action in a macro's sequence. Every field below belongs to some step kinds and not
/// others — a click's button, a wait's delay — which is the same shape <see cref="Profile"/>
/// used to have for its single action, just multiplied out across a list. <see cref="Kind"/>
/// is fixed once a step is created; building the wrong one is a delete and an add, not an edit.
/// </summary>
internal sealed class MacroStep
{
    public StepKind Kind { get; set; } = StepKind.Click;

    /// <summary>KeyPress: the key pressed and released.</summary>
    public Keys Key { get; set; } = Keys.None;

    /// <summary>Click and Drag: which button.</summary>
    public ClickButton Button { get; set; } = ClickButton.Left;

    /// <summary>Click only.</summary>
    public bool DoubleClick { get; set; }

    /// <summary>Click only: a fixed point, or wherever the cursor already is.</summary>
    public ClickTarget Target { get; set; } = ClickTarget.FixedPoint;

    /// <summary>Click's target point, Drag's "from" point, or WaitForPixelColor's sample point.</summary>
    public int X { get; set; }

    public int Y { get; set; }

    /// <summary>Click only: random pixel radius applied around <see cref="X"/>/<see cref="Y"/>.</summary>
    public int Scatter { get; set; }

    /// <summary>Drag: the point the button is released at. Always a fixed point, unlike Click.</summary>
    public int DragToX { get; set; }

    public int DragToY { get; set; }

    /// <summary>
    /// Drag: milliseconds the cursor spends travelling from the start point to the end point.
    /// Zero jumps straight there; some target applications only recognize a drag as real
    /// motion, which is what a nonzero duration is for.
    /// </summary>
    public int DragDurationMs { get; set; } = 250;

    /// <summary>TypeText and ClipboardSet.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Wait only.</summary>
    public DelaySetting Delay { get; set; } = new();

    /// <summary>WaitForPixelColor: the color being waited for, packed the way <see cref="Color.ToArgb"/> returns it.</summary>
    public int TargetColorArgb { get; set; } = unchecked((int)0xFF000000);

    /// <summary>WaitForPixelColor: per-channel drift still counted as a match.</summary>
    public int ColorTolerance { get; set; } = 10;

    /// <summary>WaitForPixelColor: zero waits indefinitely.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    [JsonIgnore]
    public Point Point => new(X, Y);

    [JsonIgnore]
    public Point DragTo => new(DragToX, DragToY);

    [JsonIgnore]
    public Color TargetColor => Color.FromArgb(TargetColorArgb);

    public MacroStep Clone() => new()
    {
        Kind = Kind,
        Key = Key,
        Button = Button,
        DoubleClick = DoubleClick,
        Target = Target,
        X = X,
        Y = Y,
        Scatter = Scatter,
        DragToX = DragToX,
        DragToY = DragToY,
        DragDurationMs = DragDurationMs,
        Text = Text,
        Delay = Delay.Clone(),
        TargetColorArgb = TargetColorArgb,
        ColorTolerance = ColorTolerance,
        TimeoutSeconds = TimeoutSeconds,
    };

    /// <summary>Pulls every value back inside <see cref="Limits"/>, the same job <see cref="Profile.Normalize"/> does for the macro as a whole.</summary>
    public void Normalize()
    {
        Kind = Enum.IsDefined(Kind) ? Kind : StepKind.Click;
        Button = Enum.IsDefined(Button) ? Button : ClickButton.Left;
        Target = Enum.IsDefined(Target) ? Target : ClickTarget.FixedPoint;

        Text ??= string.Empty;
        if (Text.Length > Limits.MaxTypedTextLength)
        {
            Text = Text[..Limits.MaxTypedTextLength];
        }

        X = Math.Clamp(X, Limits.MinCoordinate, Limits.MaxCoordinate);
        Y = Math.Clamp(Y, Limits.MinCoordinate, Limits.MaxCoordinate);
        Scatter = Math.Clamp(Scatter, Limits.MinScatter, Limits.MaxScatter);
        DragToX = Math.Clamp(DragToX, Limits.MinCoordinate, Limits.MaxCoordinate);
        DragToY = Math.Clamp(DragToY, Limits.MinCoordinate, Limits.MaxCoordinate);
        DragDurationMs = Math.Clamp(DragDurationMs, Limits.MinDragDurationMs, Limits.MaxDragDurationMs);
        ColorTolerance = Math.Clamp(ColorTolerance, Limits.MinColorTolerance, Limits.MaxColorTolerance);
        TimeoutSeconds = Math.Clamp(TimeoutSeconds, Limits.MinPixelTimeoutSeconds, Limits.MaxPixelTimeoutSeconds);

        Delay ??= new DelaySetting();
        Delay.Normalize();
    }

    /// <summary>True for the one kind that carries a key, which is what a hotkey conflict check needs to know.</summary>
    [JsonIgnore]
    public bool UsesKey => Kind == StepKind.KeyPress;
}
