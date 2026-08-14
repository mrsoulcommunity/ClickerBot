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

    /// <summary>
    /// Whether this label's alignment should follow the reading direction of the current
    /// language. On by default. A label given its own fixed alignment — a centered dash, a
    /// centered empty-state message — sets this false once, so <see cref="ApplyTheme"/> never
    /// overwrites a choice that had nothing to do with reading direction in the first place.
    /// </summary>
    public bool FollowsReadingDirection { get; set; } = true;

    public void ApplyTheme()
    {
        ForeColor = _role switch
        {
            TextRole.Secondary => Theme.TextSecondary,
            TextRole.Accent => Theme.Accent,
            TextRole.Success => Theme.Success,
            TextRole.Danger => Theme.Danger,
            _ => Theme.TextPrimary,
        };

        // The rest of the window keeps its left-to-right geometry in Persian too — see Loc's
        // class comment — but a plain label is stock-rendered, not owner-drawn, so nudging its
        // text to the edge its language actually reads from costs nothing and is worth doing.
        if (FollowsReadingDirection)
        {
            TextAlign = Loc.IsPersian ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        }
    }
}
