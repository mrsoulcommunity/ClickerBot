namespace ClickerBot;

/// <summary>
/// The range every numeric setting has to stay inside.
///
/// The inputs and the profile sanitizer both read these, so a value that can be typed is
/// always a value that can be saved and run — the two cannot drift apart.
/// </summary>
internal static class Limits
{
    /// <summary>Click coordinates span the whole virtual desktop, which can start left of zero.</summary>
    public const int MinCoordinate = -32000;

    public const int MaxCoordinate = 32000;

    public const int MinRepetitions = 1;

    public const int MaxRepetitions = 1_000_000;

    /// <summary>Zero would busy-loop, so a delay is at least one millisecond.</summary>
    public const int MinDelayMs = 1;

    /// <summary>Ten minutes: long enough for any sane pacing, short enough to stay a delay.</summary>
    public const int MaxDelayMs = 600_000;

    /// <summary>No scatter at all is the default; the cap keeps the click near what you aimed at.</summary>
    public const int MinScatter = 0;

    public const int MaxScatter = 500;

    /// <summary>A run can start immediately, or wait long enough for you to change windows.</summary>
    public const int MinStartDelaySeconds = 0;

    public const int MaxStartDelaySeconds = 60;

    public const int MinDurationMinutes = 1;

    /// <summary>Twenty-four hours.</summary>
    public const int MaxDurationMinutes = 1_440;

    /// <summary>Generous for a line of chat or form text; not a suggestion of how long to make it.</summary>
    public const int MaxTypedTextLength = 4_000;

    /// <summary>How many finished runs the history keeps before the oldest fall off.</summary>
    public const int MaxHistoryEntries = 50;

    /// <summary>
    /// A generous ceiling on a macro's own length — not a realistic sequence anyone would build
    /// by hand, but a backstop against a runaway recording session producing a list the step
    /// editor cannot reasonably scroll through.
    /// </summary>
    public const int MaxSteps = 500;

    /// <summary>One color channel, 0–255.</summary>
    public const int MinColorChannel = 0;

    public const int MaxColorChannel = 255;

    /// <summary>
    /// How far a sampled pixel may drift from the target color, per channel, and still count
    /// as a match — screens dither and video is never perfectly flat, so an exact-match
    /// tolerance of zero would make this step nearly impossible to satisfy in practice.
    /// </summary>
    public const int MinColorTolerance = 0;

    public const int MaxColorTolerance = 255;

    /// <summary>Zero means wait indefinitely for a pixel-color match.</summary>
    public const int MinPixelTimeoutSeconds = 0;

    /// <summary>One hour: long enough for a real wait, short enough that a stuck step is noticed.</summary>
    public const int MaxPixelTimeoutSeconds = 3_600;

    /// <summary>
    /// Zero drags instantly; some target applications only recognize a drag as real when the
    /// cursor visibly travels over time, which is what a nonzero duration is for.
    /// </summary>
    public const int MinDragDurationMs = 0;

    public const int MaxDragDurationMs = 5_000;
}
