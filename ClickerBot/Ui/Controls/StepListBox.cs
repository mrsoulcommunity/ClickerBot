using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>Owner-drawn list of a macro's steps: an index, a one-line summary, newest at the bottom.</summary>
internal sealed class StepListBox : ListBox, IThemedControl
{
    public StepListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        Font = Theme.Base;
        ItemHeight = 34;
        IntegralHeight = false;
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        BackColor = Theme.Field;
        ForeColor = Theme.TextPrimary;
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var background = new SolidBrush(Theme.Field))
        {
            g.FillRectangle(background, e.Bounds);
        }

        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not MacroStep step)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) != 0;
        var row = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 2, e.Bounds.Width - 8, e.Bounds.Height - 4);

        if (selected)
        {
            using var path = Theme.RoundedRect(row, 7);
            using var fill = new SolidBrush(Theme.AccentSoft);
            g.FillPath(fill, path);
        }

        var indexArea = new Rectangle(row.X + 12, row.Y, 26, row.Height);
        TextRenderer.DrawText(g, (e.Index + 1).ToString(), Theme.MonoSmall, indexArea,
            selected ? Theme.Accent : Theme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var textArea = new Rectangle(row.X + 42, row.Y, row.Width - 52, row.Height);
        TextRenderer.DrawText(g, Loc.DescribeStep(step), Font, textArea,
            selected ? Theme.Accent : Theme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (e.Index < Items.Count - 1)
        {
            using var pen = new Pen(Theme.Border);
            g.DrawLine(pen, e.Bounds.X + 12, e.Bounds.Bottom - 1, e.Bounds.Right - 12, e.Bounds.Bottom - 1);
        }
    }
}
