using System.Diagnostics;

namespace ClickerBot;

/// <summary>Everything one automation run needs to know, snapshotted from a profile.</summary>
internal sealed record AutomationSettings(
    ActionMode Mode,
    Keys Key,
    ClickButton Button,
    bool DoubleClick,
    ClickTarget Target,
    Point ClickPoint,
    int Scatter,
    DelaySetting KeyDelay,
    DelaySetting ClickDelay,
    int StartDelaySeconds,
    int? Repetitions,
    TimeSpan? Duration)
{
    public static AutomationSettings FromProfile(Profile profile) => new(
        profile.Mode,
        profile.Key,
        profile.Button,
        profile.DoubleClick,
        profile.Target,
        profile.ClickPoint,
        profile.Scatter,
        profile.KeyDelay.Clone(),
        profile.ClickDelay.Clone(),
        profile.StartDelaySeconds,
        profile.Repeat == RepeatMode.Count ? Math.Max(1, profile.Repetitions) : null,
        profile.Repeat == RepeatMode.Duration
            ? TimeSpan.FromMinutes(Math.Max(1, profile.DurationMinutes))
            : null);

    /// <summary>The total an iteration counter is working towards, for the progress bar.</summary>
    public bool HasEnd => Repetitions is not null || Duration is not null;
}

/// <summary>
/// Runs the configured sequence asynchronously so the UI thread stays free.
/// </summary>
internal static class AutomationRunner
{
    /// <summary>
    /// Repeats the profile's action until its stop condition is met or
    /// <paramref name="cancellationToken"/> is cancelled.
    ///
    /// Each wait is re-drawn from its <see cref="DelaySetting"/>, so random ranges vary per
    /// iteration. Time spent paused is not counted against a duration limit, and is not
    /// counted in the elapsed figure either — both measure work done, not wall clock.
    /// </summary>
    public static async Task RunAsync(
        AutomationSettings settings,
        PauseGate pause,
        IProgress<RunProgress> progress,
        CancellationToken cancellationToken)
    {
        var random = Random.Shared;

        for (int remaining = settings.StartDelaySeconds; remaining > 0; remaining--)
        {
            progress.Report(new RunProgress(RunPhase.CountingDown, 0, TimeSpan.Zero, remaining));
            await Task.Delay(1000, cancellationToken).ConfigureAwait(true);
        }

        var clock = Stopwatch.StartNew();
        long iteration = 0;
        progress.Report(new RunProgress(RunPhase.Running, 0, TimeSpan.Zero, 0));

        // long, not int: an infinite run counts for as long as it is left on, and an
        // overflow here would wrap the counter negative rather than just keep going.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (settings.Repetitions is { } limit && iteration >= limit)
            {
                break;
            }

            if (settings.Duration is { } window && clock.Elapsed >= window)
            {
                break;
            }

            await WaitWhilePausedAsync(pause, clock, iteration, progress, cancellationToken).ConfigureAwait(true);

            if (settings.Mode != ActionMode.ClickOnly)
            {
                NativeInput.PressKey(settings.Key);

                // Only the full sequence has a gap between the key and the click to pace.
                if (settings.Mode == ActionMode.KeyAndClick)
                {
                    await Task.Delay(settings.KeyDelay.Next(random), cancellationToken).ConfigureAwait(true);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (settings.Mode != ActionMode.KeyOnly)
            {
                Click(settings, random);
            }

            iteration++;
            progress.Report(new RunProgress(RunPhase.Running, iteration, clock.Elapsed, 0));

            // Nothing follows the final action, so end here instead of sitting out one more
            // delay — which can be minutes — before the run reports itself finished.
            if (settings.Repetitions is { } total && iteration >= total)
            {
                break;
            }

            await Task.Delay(settings.ClickDelay.Next(random), cancellationToken).ConfigureAwait(true);
        }

        clock.Stop();
    }

    private static async Task WaitWhilePausedAsync(
        PauseGate pause,
        Stopwatch clock,
        long iteration,
        IProgress<RunProgress> progress,
        CancellationToken cancellationToken)
    {
        if (!pause.IsPaused)
        {
            return;
        }

        // The clock stops with the run: a pause is not time the automation spent working, so
        // it must not eat into a duration limit.
        clock.Stop();
        progress.Report(new RunProgress(RunPhase.Paused, iteration, clock.Elapsed, 0));

        await pause.WaitAsync(cancellationToken).ConfigureAwait(true);

        clock.Start();
        progress.Report(new RunProgress(RunPhase.Running, iteration, clock.Elapsed, 0));
    }

    private static void Click(AutomationSettings settings, Random random)
    {
        Point? destination = settings.Target == ClickTarget.CursorPosition
            ? null
            : Scatter(settings.ClickPoint, settings.Scatter, random);

        NativeInput.Click(settings.Button, destination);

        if (settings.DoubleClick)
        {
            // Same spot as the first press: a double-click that lands two pixels away is two
            // single clicks as far as the target application is concerned.
            NativeInput.Click(settings.Button, destination);
        }
    }

    /// <summary>
    /// Offsets a point by a random amount inside a circle of <paramref name="radius"/> pixels.
    /// The square root keeps the distribution even across the disc instead of bunching it in
    /// the middle, which is what a straight random radius would do.
    /// </summary>
    private static Point Scatter(Point point, int radius, Random random)
    {
        if (radius <= 0)
        {
            return point;
        }

        double angle = random.NextDouble() * Math.Tau;
        double distance = Math.Sqrt(random.NextDouble()) * radius;

        return new Point(
            point.X + (int)Math.Round(Math.Cos(angle) * distance),
            point.Y + (int)Math.Round(Math.Sin(angle) * distance));
    }
}
