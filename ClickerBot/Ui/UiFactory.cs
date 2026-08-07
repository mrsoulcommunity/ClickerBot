namespace ClickerBot;

/// <summary>
/// Factory helpers so every plain control is built the same way — and, importantly, so
/// every one of them is a themed variant that survives a palette switch.
/// </summary>
internal static class UiFactory
{
    public static ThemedLabel Label(string text, int x, int y, int width, Font? font = null, TextRole role = TextRole.Primary)
    {
        var label = new ThemedLabel
        {
            Text = text,
            Font = font ?? Theme.Base,
            Role = role,
        };
        label.SetBounds(x, y, width, 20);
        return label;
    }

    public static ThemedLabel Hint(string text, int x, int y, int width) =>
        Label(text, x, y, width, Theme.Small, TextRole.Secondary);

    public static ThemedLabel Caption(string text, int x, int y, int width) =>
        Label(text, x, y, width, Theme.Caption, TextRole.Secondary);

    public static NumberBox Numeric(int x, int y, int width, int min, int max, int value)
    {
        var numeric = new NumberBox
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
        };
        // 32px tall so numeric fields line up with the key-capture fields beside them.
        numeric.SetBounds(x, y, width, 32);
        return numeric;
    }

    public static ThemedCheckBox Check(string text, int x, int y, int width)
    {
        var check = new ThemedCheckBox { Text = text };
        check.SetBounds(x, y, width, 24);
        return check;
    }
}
