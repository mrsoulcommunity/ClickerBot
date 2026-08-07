namespace ClickerApp;

/// <summary>
/// One complete set of UI colors. Every color the interface draws is resolved from here,
/// so a new appearance is a new instance of this class and nothing else.
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
        Background = Hex("#F5F6F8"),
        Surface = Hex("#FFFFFF"),
        Field = Hex("#F3F4F7"),
        FieldHover = Hex("#ECEEF3"),
        Border = Hex("#E2E5EB"),
        BorderStrong = Hex("#CFD4DE"),
        TextPrimary = Hex("#171A21"),
        TextSecondary = Hex("#6B7382"),
        Accent = Hex("#4F46E5"),
        AccentHover = Hex("#4338CA"),
        AccentSoft = Hex("#EEEFFE"),
        OnAccent = Hex("#FFFFFF"),
        Danger = Hex("#DC2626"),
        DangerSoft = Hex("#FEF2F2"),
        Success = Hex("#15803D"),
        Disabled = Hex("#B9BFCA"),
        DisabledSurface = Hex("#F3F4F7"),
    };

    public static readonly Palette Dark = new()
    {
        IsDark = true,
        Background = Hex("#0E1014"),
        Surface = Hex("#16191F"),
        Field = Hex("#1D2129"),
        FieldHover = Hex("#232833"),
        Border = Hex("#282D37"),
        BorderStrong = Hex("#3A4150"),
        TextPrimary = Hex("#E8EAEF"),
        TextSecondary = Hex("#949CAC"),
        // Indigo is lifted well above the mid-tones so it stays legible on a dark canvas.
        Accent = Hex("#818CF8"),
        AccentHover = Hex("#A5B4FC"),
        AccentSoft = Hex("#232741"),
        // Dark text on the light accent fill: white would sit at roughly 2.6:1 contrast.
        OnAccent = Hex("#10131A"),
        Danger = Hex("#F87171"),
        DangerSoft = Hex("#2C1B1E"),
        Success = Hex("#4ADE80"),
        Disabled = Hex("#4E5666"),
        DisabledSurface = Hex("#1A1E25"),
    };

    public static Palette For(ThemeMode mode) => mode == ThemeMode.Dark ? Dark : Light;

    private static Color Hex(string value) => ColorTranslator.FromHtml(value);
}
