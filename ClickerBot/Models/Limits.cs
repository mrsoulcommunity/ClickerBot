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
}
