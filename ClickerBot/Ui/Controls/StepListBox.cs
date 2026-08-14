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

        // e.Bounds already reflects the scaled row height, but everything laid out inside it is
        // a 96-DPI design measurement — see Theme.Scale. Left raw, the index column would stay
        // 26px wide while the digits in it grew, clipping a three-digit step number.
        int S(int value) => Theme.Scale(value, this);

        bool selected = (e.State & DrawItemState.Selected) != 0;
        var row = new Rectangle(
            e.Bounds.X + S(4), e.Bounds.Y + S(2), e.Bounds.Width - S(8), e.Bounds.Height - S(4));

        if (selected)
        {
            using var path = Theme.RoundedRect(row, S(7));
            using var fill = new SolidBrush(Theme.AccentSoft);
            g.FillPath(fill, path);
        }

        var indexArea = new Rectangle(row.X + S(12), row.Y, S(26), row.Height);
        TextRenderer.DrawText(g, (e.Index + 1).ToString(), Theme.MonoSmall, indexArea,
            selected ? Theme.Accent : Theme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var textArea = new Rectangle(row.X + S(42), row.Y, row.Width - S(52), row.Height);
        TextRenderer.DrawText(g, Loc.DescribeStep(step), Font, textArea,
            selected ? Theme.Accent : Theme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (e.Index < Items.Count - 1)
        {
            using var pen = new Pen(Theme.Border);
            g.DrawLine(pen, e.Bounds.X + S(12), e.Bounds.Bottom - 1, e.Bounds.Right - S(12), e.Bounds.Bottom - 1);
        }
    }
}
