namespace ClickerBot;

/// <summary>
/// Edits one <see cref="DelaySetting"/>: a fixed value, or a random min–max range
/// when "Random" is ticked. Used for both the keyboard delay and the click delay.
/// </summary>
internal sealed class DelayEditor : Panel, IThemedControl
{
    private const int MinDelay = 1;
    private const int MaxDelay = 600000;

    private readonly ThemedCheckBox _random = UiFactory.Check("Random", 0, 4, 82);
    private readonly NumberBox _fixed = UiFactory.Numeric(92, 0, 104, MinDelay, MaxDelay, 100);
    private readonly NumberBox _min = UiFactory.Numeric(92, 0, 96, MinDelay, MaxDelay, 80);
    private readonly NumberBox _max = UiFactory.Numeric(214, 0, 96, MinDelay, MaxDelay, 150);
    private readonly ThemedLabel _dash = UiFactory.Label("to", 192, 0, 18, Theme.Small, TextRole.Secondary);
    private readonly ThemedLabel _unit = UiFactory.Label("ms", 0, 0, 26, Theme.Small, TextRole.Secondary);

    private bool _loading;

    public DelayEditor()
    {
        // Left inherited rather than transparent, so children clearing themselves with the
        // backdrop color resolve to the card surface.
        Size = new Size(350, 32);
        Font = Theme.Base;

        _dash.TextAlign = ContentAlignment.MiddleCenter;
        _dash.Height = 32;
        _unit.Height = 32;

        _random.CheckedChanged += (_, _) =>
        {
            ApplyMode();
            RaiseChanged();
        };

        foreach (var numeric in new[] { _fixed, _min, _max })
        {
            numeric.ValueChanged += (_, _) => RaiseChanged();
        }

        Controls.AddRange(new Control[] { _random, _fixed, _min, _dash, _max, _unit });
        ApplyMode();
    }

    public event EventHandler? ValueChanged;

    public DelaySetting Value
    {
        get => new()
        {
            UseRandom = _random.Checked,
            Fixed = _fixed.Value,
            Min = _min.Value,
            Max = _max.Value,
        };

        set
        {
            _loading = true;
            _fixed.Value = Math.Clamp(value.Fixed, MinDelay, MaxDelay);
            _min.Value = Math.Clamp(value.Min, MinDelay, MaxDelay);
            _max.Value = Math.Clamp(value.Max, MinDelay, MaxDelay);
            _random.Checked = value.UseRandom;
            ApplyMode();
            _loading = false;
        }
    }

    // The panel is transparent; children restyle themselves through the tree walk.
    public void ApplyTheme() => Invalidate();

    private void ApplyMode()
    {
        bool random = _random.Checked;
        _fixed.Visible = !random;
        _min.Visible = random;
        _max.Visible = random;
        _dash.Visible = random;
        _unit.Left = (random ? _max.Right : _fixed.Right) + 8;
    }

    private void RaiseChanged()
    {
        if (!_loading)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
