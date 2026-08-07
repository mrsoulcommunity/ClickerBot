namespace ClickerBot;

/// <summary>A plain panel that keeps itself painted in the palette's surface color.</summary>
internal sealed class SurfacePanel : Panel, IThemedControl
{
    public SurfacePanel()
    {
        Font = Theme.Base;
        ApplyTheme();
    }

    public void ApplyTheme() => BackColor = Theme.Surface;
}
