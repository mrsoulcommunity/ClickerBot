namespace ClickerBot;

/// <summary>
/// One complete set of UI colors. Every color the interface draws is resolved from here,
/// so a new appearance is a new instance of this class and nothing else.
///
/// The neutrals are a flat zinc ramp and the one chromatic hue is amber — the color of an
/// indicator lamp on a piece of hardware. Warmth appears only where the tool is armed or
/// running, which is the one thing you need to read at a glance from across the desk.
/// </summary>
internal sealed class Palette
{
    /// <summary>The window canvas behind the cards.</summary>
    public required Color Background { get; init; }

    /// <summary>Raised areas: cards and the sidebar.</summary>
    public required Color Surface { get; init; }

    /// <summary>Recessed areas: inputs and key fields.</summary>
    public required Color Field { get; init; }

    public required Color FieldHover { get; init; }

    /// <summary>Deeper than <see cref="Field"/>: meter beds and progress troughs.</summary>
    public required Color Track { get; init; }

    /// <summary>Hairline separators and resting input outlines.</summary>
    public required Color Border { get; init; }

    /// <summary>A heavier outline for elements that must read as interactive.</summary>
    public required Color BorderStrong { get; init; }

    public required Color TextPrimary { get; init; }

    public required Color TextSecondary { get; init; }

    public required Color Accent { get; init; }

    public required Color AccentHover { get; init; }

    /// <summary>Low-saturation accent wash for selected and hovered states.</summary>
    public required Color AccentSoft { get; init; }

    /// <summary>Text drawn on top of <see cref="Accent"/>.</summary>
    public required Color OnAccent { get; init; }

    public required Color Danger { get; init; }

    public required Color DangerSoft { get; init; }

    public required Color Success { get; init; }

    public required Color Disabled { get; init; }

    public required Color DisabledSurface { get; init; }

    /// <summary>True when this palette is dark, which the window chrome needs to know.</summary>
    public required bool IsDark { get; init; }

    public static readonly Palette Light = new()
    {
        IsDark = false,
        Background = Hex("#F4F4F5"),
        Surface = Hex("#FFFFFF"),
        Field = Hex("#F4F4F5"),
        FieldHover = Hex("#E9E9EC"),
        Track = Hex("#E7E7EA"),
        Border = Hex("#E4E4E7"),
        BorderStrong = Hex("#D0D0D6"),
        TextPrimary = Hex("#18181B"),
        TextSecondary = Hex("#71717A"),
        // Amber-700: dark enough to carry white on a fill and to be read as text on white,
        // which is what lets one accent serve both jobs.
        Accent = Hex("#B45309"),
        AccentHover = Hex("#92400E"),
        AccentSoft = Hex("#FDF1DC"),
        OnAccent = Hex("#FFFFFF"),
        Danger = Hex("#B91C1C"),
        DangerSoft = Hex("#FEF2F2"),
        Success = Hex("#15803D"),
        Disabled = Hex("#A1A1AA"),
        DisabledSurface = Hex("#F4F4F5"),
    };

    public static readonly Palette Dark = new()
    {
        IsDark = true,
        Background = Hex("#121214"),
        Surface = Hex("#1A1A1E"),
        Field = Hex("#212126"),
        FieldHover = Hex("#2A2A31"),
        Track = Hex("#0D0D0F"),
        Border = Hex("#2A2A31"),
        BorderStrong = Hex("#3F3F46"),
        TextPrimary = Hex("#F4F4F5"),
        TextSecondary = Hex("#A1A1AA"),
        // Amber-400. Lifted well clear of the mid-tones so it still reads as a lamp rather
        // than a brown smear on a near-black canvas.
        Accent = Hex("#FBBF24"),
        AccentHover = Hex("#FCD34D"),
        AccentSoft = Hex("#2C2313"),
        // Near-black on the light accent fill: white would sit at roughly 1.6:1.
        OnAccent = Hex("#1A1200"),
        Danger = Hex("#F87171"),
        DangerSoft = Hex("#2C1A1C"),
        Success = Hex("#4ADE80"),
        Disabled = Hex("#52525B"),
        DisabledSurface = Hex("#1E1E22"),
    };

    public static Palette For(ThemeMode mode) => mode == ThemeMode.Dark ? Dark : Light;

    private static Color Hex(string value) => ColorTranslator.FromHtml(value);
}
