namespace ClickerBot;

/// <summary>
/// A drop-down-only combo box themed to match the rest of the window. Used where a step's kind
/// has too many values for a <see cref="Segmented"/> pill to hold comfortably.
/// </summary>
internal sealed class ThemedComboBox : ComboBox, IThemedControl
{
    public ThemedComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        Font = Theme.Base;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 20;
        DrawItem += OnDrawItem;
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        BackColor = Theme.Field;
        ForeColor = Theme.TextPrimary;
        Invalidate();
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
        {
            e.DrawBackground();
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color back = selected ? Theme.AccentSoft : Theme.Field;
        using (var brush = new SolidBrush(back))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        Color fore = selected ? Theme.Accent : Theme.TextPrimary;
        TextRenderer.DrawText(e.Graphics, Items[e.Index]?.ToString() ?? string.Empty, Font, e.Bounds, fore,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);

        e.DrawFocusRectangle();
    }
}
