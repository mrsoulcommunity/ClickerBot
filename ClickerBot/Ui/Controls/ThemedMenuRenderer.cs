namespace ClickerBot;

/// <summary>
/// Palette colors for a <see cref="ToolStrip"/>-based popup. Every member is read at paint
/// time rather than captured once, so a theme switch reaches an already-built menu.
/// </summary>
internal sealed class ThemedMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.Surface;

    public override Color MenuBorder => Theme.BorderStrong;

    public override Color MenuItemBorder => Theme.AccentSoft;

    public override Color MenuItemSelected => Theme.AccentSoft;

    public override Color MenuItemSelectedGradientBegin => Theme.AccentSoft;

    public override Color MenuItemSelectedGradientEnd => Theme.AccentSoft;

    public override Color MenuItemPressedGradientBegin => Theme.AccentSoft;

    public override Color MenuItemPressedGradientEnd => Theme.AccentSoft;

    // The strip down the left of a menu, where icons would go. Filled with the surface color
    // so it disappears: nothing in this app's menus has an icon.
    public override Color ImageMarginGradientBegin => Theme.Surface;

    public override Color ImageMarginGradientMiddle => Theme.Surface;

    public override Color ImageMarginGradientEnd => Theme.Surface;

    public override Color SeparatorDark => Theme.Border;

    public override Color SeparatorLight => Theme.Border;
}

/// <summary>
/// Draws a menu from the active <see cref="Theme"/> instead of the system palette.
///
/// The same reasoning that owner-draws the checkbox glyph and the numeric field's steppers
/// applies here: left to itself, Windows renders a menu in system colors, which stay light on
/// a dark palette no matter what BackColor is assigned. Shared by the step-kind
/// <see cref="Dropdown"/> and the notification-area menu, so both read as part of the app.
/// </summary>
internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
{
    public ThemedMenuRenderer()
        : base(new ThemedMenuColors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = !e.Item.Enabled ? Theme.Disabled
            : e.Item.Selected ? Theme.Accent
            : Theme.TextPrimary;

        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var fill = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(fill, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.BorderStrong);
        e.Graphics.DrawRectangle(pen, new Rectangle(
            0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
    }
}
