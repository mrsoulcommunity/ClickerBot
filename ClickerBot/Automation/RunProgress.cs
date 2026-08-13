namespace ClickerBot;

/// <summary>Where a run currently is, which is what the run panel renders.</summary>
internal enum RunPhase
{
    /// <summary>Waiting out the start delay so you can bring the target window forward.</summary>
    CountingDown,

    Running,

    Paused,
}

/// <summary>
/// One snapshot of a run in flight. Sent after every iteration, and again whenever the phase
/// changes, so the panel never has to guess what is happening between reports.
/// </summary>
/// <param name="Iteration">Completed iterations so far.</param>
/// <param name="Elapsed">Time spent running, with paused time excluded.</param>
/// <param name="CountdownSeconds">Seconds left on the start delay, during that phase only.</param>
internal readonly record struct RunProgress(
    RunPhase Phase,
    long Iteration,
    TimeSpan Elapsed,
    int CountdownSeconds);

/// <summary>
/// Lets the UI hold a run still without tearing it down.
///
/// Pausing has to be a gate rather than a cancellation: the run keeps its iteration count,
/// its elapsed clock and its place in the loop, and carries on from there when resumed.
/// </summary>
internal sealed class PauseGate
{
    private TaskCompletionSource _resume = AlreadyOpen();

    public bool IsPaused { get; private set; }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        _resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }

        IsPaused = false;
        _resume.TrySetResult();
    }

    public void Toggle()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>Completes when the gate is open again, or throws if the run is cancelled first.</summary>
    public Task WaitAsync(CancellationToken cancellationToken) => _resume.Task.WaitAsync(cancellationToken);

    private static TaskCompletionSource AlreadyOpen()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
