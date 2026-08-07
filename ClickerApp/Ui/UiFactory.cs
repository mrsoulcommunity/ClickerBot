namespace ClickerApp;

/// <summary>Small factory helpers so every plain control is styled the same way.</summary>
internal static class UiFactory
{
    public static Label Label(string text, int x, int y, int width, Font? font = null, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            Font = font ?? Theme.Base,
            ForeColor = color ?? Theme.TextPrimary,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            UseMnemonic = false,
        };
        label.SetBounds(x, y, width, 20);
        return label;
    }

    public static Label Hint(string text, int x, int y, int width) =>
        Label(text, x, y, width, Theme.Small, Theme.TextSecondary);

    public static NumericUpDown Numeric(int x, int y, int width, int min, int max, int value)
    {
        var numeric = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Font = Theme.Base,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            TextAlign = HorizontalAlignment.Center,
        };
        numeric.SetBounds(x, y, width, 24);
        return numeric;
    }

    public static CheckBox Check(string text, int x, int y, int width)
    {
        var check = new CheckBox
        {
            Text = text,
            Font = Theme.Base,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            AutoSize = false,
            Cursor = Cursors.Hand,
        };
        check.FlatAppearance.BorderSize = 0;
        check.SetBounds(x, y, width, 22);
        return check;
    }
}
