namespace ClickerApp;

/// <summary>
/// Edits one <see cref="DelaySetting"/>: a fixed value, or a random min–max range
/// when "Random" is ticked. Used for both the keyboard delay and the click delay.
/// </summary>
internal sealed class DelayEditor : Panel
{
    private const int MinDelay = 1;
    private const int MaxDelay = 600000;

    private readonly CheckBox _random = UiFactory.Check("Random", 0, 3, 78);
    private readonly NumericUpDown _fixed = UiFactory.Numeric(86, 2, 88, MinDelay, MaxDelay, 100);
    private readonly NumericUpDown _min = UiFactory.Numeric(86, 2, 80, MinDelay, MaxDelay, 80);
    private readonly NumericUpDown _max = UiFactory.Numeric(188, 2, 80, MinDelay, MaxDelay, 150);
    private readonly Label _dash = UiFactory.Label("to", 170, 2, 18, Theme.Small, Theme.TextSecondary);
    private readonly Label _unit = UiFactory.Label("ms", 0, 2, 24, Theme.Small, Theme.TextSecondary);

    private bool _loading;

    public DelayEditor()
    {
        Size = new Size(310, 28);
        Font = Theme.Base;
        _dash.TextAlign = ContentAlignment.MiddleCenter;

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
            Fixed = (int)_fixed.Value,
            Min = (int)_min.Value,
            Max = (int)_max.Value,
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

    private void ApplyMode()
    {
        bool random = _random.Checked;
        _fixed.Visible = !random;
        _min.Visible = random;
        _max.Visible = random;
        _dash.Visible = random;
        _unit.Left = random ? _max.Right + 6 : _fixed.Right + 6;
    }

    private void RaiseChanged()
    {
        if (!_loading)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
