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

    public void ApplyTheme() => ForeColor = _role switch
    {
        TextRole.Secondary => Theme.TextSecondary,
        TextRole.Accent => Theme.Accent,
        TextRole.Success => Theme.Success,
        TextRole.Danger => Theme.Danger,
        _ => Theme.TextPrimary,
    };
}
