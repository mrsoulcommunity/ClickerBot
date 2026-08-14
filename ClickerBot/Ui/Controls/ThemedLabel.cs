namespace ClickerBot;

/// <summary>What a piece of text means, which decides its color in either theme.</summary>
internal enum TextRole
{
    Primary,
    Secondary,
    Accent,
    Success,
    Danger,
}

/// <summary>
/// A label that stores its <see cref="Role"/> instead of a literal color, so it can be
/// recolored on a theme switch. Callers set the role and never touch ForeColor.
/// </summary>
internal sealed class ThemedLabel : Label, IThemedControl
{
    private TextRole _role = TextRole.Primary;

    public ThemedLabel()
    {
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleLeft;
        BackColor = Color.Transparent;
        UseMnemonic = false;
        Font = Theme.Base;
        ApplyTheme();
    }

    public TextRole Role
    {
        get => _role;
        set
        {
            if (_role == value)
            {
                return;
            }

            _role = value;
            ApplyTheme();
        }
    }

    // Alignment is deliberately left alone by the theme and language passes. An earlier
    // revision right-aligned every label in Persian, which reads well for a label sitting
    // immediately left of its field but breaks every other kind: a hint describing the control
    // below it, or a card-wide caption, slid to the far right edge and detached from the thing
    // it referred to. The window keeps its left-to-right geometry in both languages — see Loc's
    // class comment — so the labels keep the alignment that geometry was built around, and each
    // label that wants something else (a centered dash, a centered empty state) still just sets
    // it once at construction.

    public void ApplyTheme() => ForeColor = _role switch
    {
        TextRole.Secondary => Theme.TextSecondary,
        TextRole.Accent => Theme.Accent,
        TextRole.Success => Theme.Success,
        TextRole.Danger => Theme.Danger,
        _ => Theme.TextPrimary,
    };
}
