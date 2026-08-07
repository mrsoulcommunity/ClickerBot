namespace ClickerBot;

/// <summary>Everything one automation run needs to know, snapshotted from a profile.</summary>
internal sealed record AutomationSettings(
    Keys Key,
    Point ClickPoint,
    DelaySetting KeyDelay,
    DelaySetting ClickDelay,
    int? Repetitions)
{
    public static AutomationSettings FromProfile(Profile profile) => new(
        profile.Key,
        profile.ClickPoint,
        profile.KeyDelay.Clone(),
        profile.ClickDelay.Clone(),
        profile.InfiniteLoop ? null : profile.Repetitions);
}

/// <summary>
/// Runs the key-press / click sequence asynchronously so the UI thread stays free.
/// </summary>
internal static class AutomationRunner
{
    /// <summary>
    /// Repeats "press key, wait, left click, wait" until the repetition count is reached
    /// or <paramref name="cancellationToken"/> is cancelled. A null repetition count loops forever.
    /// Each wait is re-drawn from its <see cref="DelaySetting"/>, so random ranges vary per iteration.
    /// </summary>
    /// <param name="progress">Reports the number of completed iterations.</param>
    public static async Task RunAsync(
        AutomationSettings settings,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        var random = Random.Shared;

        for (int iteration = 1; settings.Repetitions is null || iteration <= settings.Repetitions; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            NativeInput.PressKey(settings.Key);

            await Task.Delay(settings.KeyDelay.Next(random), cancellationToken).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();
            NativeInput.LeftClick(settings.ClickPoint);

            progress.Report(iteration);

            // Pace the gap before the next iteration's key press.
            await Task.Delay(settings.ClickDelay.Next(random), cancellationToken).ConfigureAwait(true);
        }
    }
}
