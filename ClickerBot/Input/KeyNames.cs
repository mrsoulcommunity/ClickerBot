namespace ClickerBot;

/// <summary>Human friendly names for <see cref="Keys"/> values shown in the UI.</summary>
internal static class KeyNames
{
    public static string Describe(Keys key) => key switch
    {
        Keys.None => "—",
        Keys.Back => "Backspace",
        Keys.Return => "Enter",
        Keys.Space => "Space",
        Keys.Escape => "Esc",
        Keys.Capital => "Caps Lock",
        Keys.OemPipe => "Backslash  \\",
        Keys.OemQuestion => "Slash  /",
        Keys.Oemtilde => "Tilde  ~",
        Keys.Oemcomma => "Comma  ,",
        Keys.OemPeriod => "Period  .",
        Keys.OemMinus => "Minus  -",
        Keys.Oemplus => "Equals  =",
        Keys.OemSemicolon => "Semicolon  ;",
        Keys.OemQuotes => "Quote  '",
        Keys.OemOpenBrackets => "Bracket  [",
        Keys.OemCloseBrackets => "Bracket  ]",
        >= Keys.D0 and <= Keys.D9 => key.ToString()[1..],
        >= Keys.NumPad0 and <= Keys.NumPad9 => "Numpad " + key.ToString()[6..],
        _ => key.ToString(),
    };
}
