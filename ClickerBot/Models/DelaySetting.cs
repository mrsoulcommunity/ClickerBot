namespace ClickerBot;

/// <summary>
/// A delay that is either a fixed number of milliseconds or a random value picked
/// from an inclusive [Min, Max] range on every use.
/// </summary>
internal sealed class DelaySetting
{
    public bool UseRandom { get; set; }

    public int Fixed { get; set; } = 100;

    public int Min { get; set; } = 80;

    public int Max { get; set; } = 150;

    /// <summary>Returns the delay to use for the next action, in milliseconds.</summary>
    public int Next(Random random)
    {
        if (!UseRandom)
        {
            return Fixed;
        }

        int low = Math.Min(Min, Max);
        int high = Math.Max(Min, Max);
        return random.Next(low, high + 1);
    }

    public DelaySetting Clone() => new()
    {
        UseRandom = UseRandom,
        Fixed = Fixed,
        Min = Min,
        Max = Max,
    };

    /// <summary>
    /// Pulls every value back inside <see cref="Limits"/> and puts the range the right way
    /// round. Used when reading the profile file, which nothing stops a person editing by hand.
    /// </summary>
    public void Normalize()
    {
        Fixed = Math.Clamp(Fixed, Limits.MinDelayMs, Limits.MaxDelayMs);
        Min = Math.Clamp(Min, Limits.MinDelayMs, Limits.MaxDelayMs);
        Max = Math.Clamp(Max, Limits.MinDelayMs, Limits.MaxDelayMs);

        if (Min > Max)
        {
            (Min, Max) = (Max, Min);
        }
    }
}
