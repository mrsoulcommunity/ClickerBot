namespace ClickerBot;

/// <summary>A borderless text box that follows the palette.</summary>
internal sealed class ThemedTextBox : TextBox, IThemedControl
{
    public ThemedTextBox()
    {
        BorderStyle = BorderStyle.None;
        Font = Theme.Base;
        ApplyTheme();
    }

    /// <summary>When set, the box paints on <see cref="Palette.Surface"/> instead of the canvas.</summary>
    public bool OnSurface { get; set; }

    public void ApplyTheme()
    {
        BackColor = OnSurface ? Theme.Surface : Theme.Background;
        ForeColor = Enabled ? Theme.TextPrimary : Theme.Disabled;
    }

    // Being locked during a run has to look locked, and the palette owns what that looks like.
    protected override void OnEnabledChanged(EventArgs e)
    {
        ApplyTheme();
        base.OnEnabledChanged(e);
    }
}
