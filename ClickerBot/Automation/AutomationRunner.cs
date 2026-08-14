using System.Diagnostics;

namespace ClickerBot;

/// <summary>Everything one automation run needs to know, snapshotted from a profile.</summary>
internal sealed record AutomationSettings(
    IReadOnlyList<MacroStep> Steps,
    int StartDelaySeconds,
    int? Repetitions,
    TimeSpan? Duration,
    bool RequireTargetWindow,
    string TargetWindowTitle)
{
    public static AutomationSettings FromProfile(Profile profile) => new(
        profile.Steps.Select(s => s.Clone()).ToList(),
        profile.StartDelaySeconds,
        profile.Repeat == RepeatMode.Count ? Math.Max(1, profile.Repetitions) : null,
        profile.Repeat == RepeatMode.Duration
            ? TimeSpan.FromMinutes(Math.Max(1, profile.DurationMinutes))
            : null,
        profile.RequireTargetWindow,
        profile.TargetWindowTitle);
}

/// <summary>
/// Runs a macro's step sequence asynchronously so the UI thread stays free.
/// </summary>
internal static class AutomationRunner
{
    /// <summary>
    /// Repeats <paramref name="settings"/>'s steps until its stop condition is met or
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

            // A trailing Wait step exists to pace one iteration against the next, which the
            // last iteration of a count-limited run has none of — running it anyway would mean
            // sitting out a delay that can be minutes long for no reason before the run reports
            // itself finished. Only count-limited runs know in advance that this iteration is
            // the last one; a duration limit can end after any iteration, so it always runs the
            // full sequence including its trailing wait, same as before.
            bool isFinalIteration = settings.Repetitions is { } total && iteration + 1 >= total;
            await PerformIterationAsync(settings.Steps, random, isFinalIteration, cancellationToken).ConfigureAwait(true);

            iteration++;
            progress.Report(new RunProgress(RunPhase.Running, iteration, clock.Elapsed, 0));
        }

        clock.Stop();
    }

    /// <summary>
    /// Runs every step once, in order — the same loop body <see cref="RunAsync"/> repeats, so
    /// the run bar's <b>Test</b> button can fire one full pass without any of the repeat count,
    /// pausing, or stop-condition machinery around it.
    /// </summary>
    public static async Task RunOnceAsync(IReadOnlyList<MacroStep> steps, CancellationToken cancellationToken) =>
        await PerformIterationAsync(steps, Random.Shared, skipTrailingWaits: false, cancellationToken).ConfigureAwait(true);

    /// <summary>
    /// Runs exactly one step, for the step editor's own "try this step" button — the same
    /// per-step code the real loop calls, so testing one step and running it for real can never
    /// behave differently.
    /// </summary>
    public static Task RunStepOnceAsync(MacroStep step, CancellationToken cancellationToken) =>
        PerformStepAsync(step, Random.Shared, cancellationToken);

    private static async Task PerformIterationAsync(
        IReadOnlyList<MacroStep> steps, Random random, bool skipTrailingWaits, CancellationToken cancellationToken)
    {
        int count = steps.Count;
        if (skipTrailingWaits)
        {
            while (count > 0 && steps[count - 1].Kind == StepKind.Wait)
            {
                count--;
            }
        }

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PerformStepAsync(steps[i], random, cancellationToken).ConfigureAwait(true);
        }
    }

    private static async Task PerformStepAsync(MacroStep step, Random random, CancellationToken cancellationToken)
    {
        switch (step.Kind)
        {
            case StepKind.KeyPress:
                NativeInput.PressKey(step.Key);
                break;

            case StepKind.Click:
                Click(step, random);
                break;

            case StepKind.Drag:
                await NativeInput.DragAsync(step.Button, step.Point, step.DragTo, step.DragDurationMs, cancellationToken)
                    .ConfigureAwait(true);
                break;

            case StepKind.TypeText:
                NativeInput.TypeText(step.Text);
                break;

            case StepKind.Wait:
                await Task.Delay(step.Delay.Next(random), cancellationToken).ConfigureAwait(true);
                break;

            case StepKind.WaitForPixelColor:
                await WaitForPixelColorAsync(step, cancellationToken).ConfigureAwait(true);
                break;

            case StepKind.ClipboardSet:
                NativeInput.SetClipboardText(step.Text);
                break;

            case StepKind.ClipboardPaste:
                NativeInput.PasteFromClipboard();
                break;
        }
    }

    private static void Click(MacroStep step, Random random)
    {
        Point? destination = step.Target == ClickTarget.CursorPosition
            ? null
            : Scatter(step.Point, step.Scatter, random);

        NativeInput.Click(step.Button, destination);

        if (step.DoubleClick)
        {
            // Same spot as the first press: a double-click that lands two pixels away is two
            // single clicks as far as the target application is concerned.
            NativeInput.Click(step.Button, destination);
        }
    }

    /// <summary>
    /// Polls the target pixel every 100ms until it matches within tolerance or the step's own
    /// timeout elapses. A timeout is not a failure: it lets the step go and the macro moves on
    /// to whatever comes next, on the theory that a macro stuck waiting forever on a color that
    /// will never come is worse than one that proceeds slightly wrong. Zero timeout waits with
    /// no limit at all, for when that risk is acceptable.
    /// </summary>
    private static async Task WaitForPixelColorAsync(MacroStep step, CancellationToken cancellationToken)
    {
        DateTimeOffset? deadline = step.TimeoutSeconds > 0
            ? DateTimeOffset.UtcNow.AddSeconds(step.TimeoutSeconds)
            : null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ColorsMatch(NativeInput.SamplePixel(step.Point), step.TargetColor, step.ColorTolerance))
            {
                return;
            }

            if (deadline is { } d && DateTimeOffset.UtcNow >= d)
            {
                return;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(true);
        }
    }

    private static bool ColorsMatch(Color sampled, Color target, int tolerance) =>
        Math.Abs(sampled.R - target.R) <= tolerance &&
        Math.Abs(sampled.G - target.G) <= tolerance &&
        Math.Abs(sampled.B - target.B) <= tolerance;

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
        RunPhase phase = pause.IsWaitingForWindow ? RunPhase.WaitingForWindow : RunPhase.Paused;
        progress.Report(new RunProgress(phase, iteration, clock.Elapsed, 0));

        await pause.WaitAsync(cancellationToken).ConfigureAwait(true);

        clock.Start();
        progress.Report(new RunProgress(RunPhase.Running, iteration, clock.Elapsed, 0));
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
