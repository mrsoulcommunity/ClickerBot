namespace ClickerBot;

/// <summary>Where a run currently is, which is what the run panel renders.</summary>
internal enum RunPhase
{
    /// <summary>Waiting out the start delay so you can bring the target window forward.</summary>
    CountingDown,

    Running,

    /// <summary>Held by the user's own Pause, not by anything the macro itself is waiting on.</summary>
    Paused,

    /// <summary>
    /// Held because the macro is restricted to a window that is not currently in front — see
    /// <see cref="Profile.RequireTargetWindow"/>. Resumes on its own once that window regains
    /// focus, unlike <see cref="Paused"/>.
    /// </summary>
    WaitingForWindow,
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
///
/// Two independent things can each want the run held — the user's own Pause, and a target
/// window not being in front — and either one holding is enough to pause. They are tracked
/// separately rather than as one flag so that the window-focus check can let go on its own
/// the moment focus returns, without ever resuming a run the user paused by hand: resuming
/// requires every reason that is currently holding the gate to clear, not just one of them.
/// </summary>
internal sealed class PauseGate
{
    private TaskCompletionSource _resume = AlreadyOpen();
    private bool _userPaused;
    private bool _windowUnfocused;

    public bool IsPaused => _userPaused || _windowUnfocused;

    /// <summary>True when the only thing holding the gate is the target window losing focus.</summary>
    public bool IsWaitingForWindow => _windowUnfocused && !_userPaused;

    public void Pause() => SetUserPaused(true);

    public void Resume() => SetUserPaused(false);

    public void Toggle() => SetUserPaused(!_userPaused);

    /// <summary>Called from MainForm's own focus poll — see CheckWindowFocus — never by the run itself.</summary>
    public void SetWindowFocused(bool hasFocus)
    {
        bool unfocused = !hasFocus;
        if (_windowUnfocused == unfocused)
        {
            return;
        }

        _windowUnfocused = unfocused;
        Resync();
    }

    /// <summary>Completes when the gate is open again, or throws if the run is cancelled first.</summary>
    public Task WaitAsync(CancellationToken cancellationToken) => _resume.Task.WaitAsync(cancellationToken);

    private void SetUserPaused(bool value)
    {
        if (_userPaused == value)
        {
            return;
        }

        _userPaused = value;
        Resync();
    }

    private void Resync()
    {
        bool shouldBeOpen = !IsPaused;
        bool isOpen = _resume.Task.IsCompleted;

        if (shouldBeOpen && !isOpen)
        {
            _resume.TrySetResult();
        }
        else if (!shouldBeOpen && isOpen)
        {
            _resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private static TaskCompletionSource AlreadyOpen()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
