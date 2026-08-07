using System.Drawing.Drawing2D;

namespace ClickerApp;

/// <summary>Owner-drawn profile list: flat rows, soft accent pill on the selected one.</summary>
internal sealed class ProfileListBox : ListBox, IThemedControl
{
    public ProfileListBox()
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
        BackColor = Theme.Surface;
        ForeColor = Theme.TextPrimary;
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var background = new SolidBrush(Theme.Surface))
        {
            g.FillRectangle(background, e.Bounds);
        }

        if (e.Index < 0 || e.Index >= Items.Count)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) != 0;
        var row = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 2, e.Bounds.Width - 8, e.Bounds.Height - 4);

        if (selected)
        {
            using var path = Theme.RoundedRect(row, 8);
            using var fill = new SolidBrush(Theme.AccentSoft);
            g.FillPath(fill, path);

            using var bar = new SolidBrush(Theme.Accent);
            using var barPath = Theme.RoundedRect(new Rectangle(row.X + 5, row.Y + 7, 3, row.Height - 14), 2);
            g.FillPath(bar, barPath);
        }

        var text = new Rectangle(row.X + 16, row.Y, row.Width - 22, row.Height);
        TextRenderer.DrawText(g, GetItemText(Items[e.Index]), Font, text,
            selected ? Theme.Accent : Theme.TextPrimary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
