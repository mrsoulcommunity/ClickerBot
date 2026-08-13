using System.Media;

namespace ClickerBot;

internal sealed partial class MainForm : Form
{
    private const string StartHotkeyName = "start";
    private const string PauseHotkeyName = "pause";
    private const string StopHotkeyName = "stop";
    private const string PickHotkeyName = "pick";

    // Profiles
    private readonly SurfacePanel _sidebar = new();
    private readonly ProfileListBox _profileList = new();
    private readonly FlatButton _newButton = new();
    private readonly FlatButton _duplicateButton = new();
    private readonly FlatButton _deleteButton = new();
    private readonly FlatButton _importButton = new();
    private readonly FlatButton _exportButton = new();
    private readonly FlatButton _historyButton = new();
    private readonly ThemedTextBox _nameBox = new() { Font = Theme.Title };
    private readonly ThemedLabel _renameHint = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly ThemeToggle _themeToggle = new();

    // Action
    private readonly Segmented _modeSelector = new();
    private readonly ThemedLabel _keyLabel = UiFactory.Label("Key", 0, 0, 76);
    private readonly KeyCaptureBox _keyBox = new();
    private readonly TextField _typeText = new() { MaxLength = Limits.MaxTypedTextLength };
    private readonly ThemedLabel _keyHint = UiFactory.Hint("Esc clears", 0, 0, 72);
    private readonly Segmented _buttonSelector = new();
    private readonly ThemedCheckBox _doubleClick = UiFactory.Check("Double", 0, 0, 78);
    private readonly Segmented _targetSelector = new();
    private readonly NumberBox _xInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly NumberBox _yInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly FlatButton _captureButton = new();
    private readonly NumberBox _scatter =
        UiFactory.Numeric(0, 0, 84, Limits.MinScatter, Limits.MaxScatter, 0);

    // Timing
    private readonly DelayEditor _keyDelay = new();
    private readonly DelayEditor _clickDelay = new();
    private readonly NumberBox _startDelay = UiFactory.Numeric(
        0, 0, 84, Limits.MinStartDelaySeconds, Limits.MaxStartDelaySeconds, 3);

    // Repeat
    private readonly Segmented _repeatSelector = new();
    private readonly ThemedLabel _repeatLabel = UiFactory.Label("Iterations", 0, 0, 88);
    private readonly ThemedLabel _repeatHint = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly NumberBox _repetitions =
        UiFactory.Numeric(0, 0, 118, Limits.MinRepetitions, Limits.MaxRepetitions, 100);
    private readonly NumberBox _duration =
        UiFactory.Numeric(0, 0, 118, Limits.MinDurationMinutes, Limits.MaxDurationMinutes, 5);

    // Hotkeys
    private readonly KeyCaptureBox _startHotkeyBox = new();
    private readonly KeyCaptureBox _pauseHotkeyBox = new();
    private readonly KeyCaptureBox _stopHotkeyBox = new();
    private readonly KeyCaptureBox _pickHotkeyBox = new();

    // Window options
    private readonly ThemedCheckBox _alwaysOnTop = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _hideToTray = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _autoStart = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _failsafe = UiFactory.Check(string.Empty, 0, 0, 320);

    // Run
    private readonly RunPanel _runPanel = new();
    private readonly List<Card> _cards = new();
    private readonly List<Card> _settingCards = new();
    private readonly NotifyIcon _tray = new();

    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 400 };
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 100 };

    private ProfileStore _store = new();
    private Profile _current = new();
    private RunHistoryStore _history = new();
    private HotkeyManager? _hotkeys;
    private CancellationTokenSource? _cancellation;
    private PauseGate? _pause;
    private Task? _run;
    private string? _pendingStopReason;
    private readonly bool _startMinimized;
    private bool _shownOnce;
    private bool _loading;
    private bool _suspendSelection;

    public MainForm(bool startMinimized = false)
    {
        _startMinimized = startMinimized;

        BuildUi();
        BuildTray();
        _saveTimer.Tick += (_, _) => SaveNow();

        // Repaints the cadence strip and the elapsed clock between progress reports, so a slow
        // run still shows a moving second hand rather than looking frozen. The failsafe check
        // rides along on the same clock rather than its own timer, since both only matter
        // while a run is in flight.
        _tickTimer.Tick += (_, _) =>
        {
            _runPanel.Invalidate(invalidateChildren: true);
            CheckFailsafe();
        };
    }

    /// <summary>
    /// Suppresses the very first Show the WinForms message loop performs when launched with
    /// <c>--minimized</c>, so a Windows-startup launch goes straight to the tray instead of
    /// flashing the window open and immediately hiding it again.
    /// </summary>
    protected override void SetVisibleCore(bool value)
    {
        if (!_shownOnce && _startMinimized)
        {
            _shownOnce = true;
            base.SetVisibleCore(false);
            _tray.Visible = true;
            return;
        }

        _shownOnce = true;
        base.SetVisibleCore(value);
    }

    private bool IsRunning => _cancellation is not null;

    // --- Lifecycle ------------------------------------------------------

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        _hotkeys = new HotkeyManager(this);
        _hotkeys.HotkeyPressed += OnHotkeyPressed;

        _store = ProfileStore.Load();
        _history = RunHistoryStore.Load();

        // Apply before the first paint, then let ThemeManager keep the tree in sync.
        Theme.Apply(_store.Appearance);
        ThemeManager.Attach(this);

        _loading = true;
        _alwaysOnTop.Checked = _store.AlwaysOnTop;
        _hideToTray.Checked = _store.HideToTrayWhileRunning;
        _failsafe.Checked = _store.FailsafeEnabled;
        // The registry, not a stored flag, is the source of truth — see StartupManager.
        _autoStart.Checked = StartupManager.IsEnabled();
        _loading = false;
        TopMost = _store.AlwaysOnTop;

        ReloadProfileList(_store.SelectedIndex);
        _runPanel.SetIdleMessage("Ready");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (e.Cancel)
        {
            return;
        }

        // A run resumes on the UI thread after each of its delays. Closing out from under it
        // leaves that continuation to land on disposed controls, so the close is held back
        // for the moment it takes cancellation to unwind, then repeated.
        if (_run is { IsCompleted: false })
        {
            e.Cancel = true;
            StopAutomation();
            _run.ContinueWith(_ => Close(), TaskScheduler.FromCurrentSynchronizationContext());
            return;
        }

        SaveNow();
        _tray.Visible = false;
        _hotkeys?.Dispose();
        _hotkeys = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // None of these are in the form's component container, so nothing else releases
            // them: the timers keep callbacks alive and the hotkeys stay owned by this app.
            _saveTimer.Dispose();
            _tickTimer.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _hotkeys?.Dispose();
            _hotkeys = null;
            _cancellation?.Dispose();
            _cancellation = null;
            AppIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    // --- Tray and window options ------------------------------------------

    private void BuildTray()
    {
        _tray.Icon = AppIcon.Idle;
        _tray.Text = "ClickerBot";
        _tray.DoubleClick += (_, _) => RestoreFromTray();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show ClickerBot", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Stop run", null, (_, _) => StopAutomation());
        menu.Items.Add("Quit", null, (_, _) => Close());
        _tray.ContextMenuStrip = menu;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        _tray.Visible = false;
        Activate();
    }

    private void ApplyWindowOptions()
    {
        if (_loading)
        {
            return;
        }

        _store.AlwaysOnTop = _alwaysOnTop.Checked;
        _store.HideToTrayWhileRunning = _hideToTray.Checked;
        _store.FailsafeEnabled = _failsafe.Checked;
        TopMost = _store.AlwaysOnTop;
        ScheduleSave();
    }

    /// <summary>
    /// Separate from <see cref="ApplyWindowOptions"/>: this one is not a profiles.json field
    /// at all, it is a registry write, and a write that fails has to un-check the box rather
    /// than silently claim a state that was never actually saved.
    /// </summary>
    private void ApplyAutoStart()
    {
        if (_loading)
        {
            return;
        }

        bool requested = _autoStart.Checked;

        try
        {
            StartupManager.SetEnabled(requested);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            _loading = true;
            _autoStart.Checked = !requested;
            _loading = false;

            _runPanel.SetIdleMessage("Could not change startup setting: " + ex.Message, TextRole.Danger);
        }
    }

    private void UpdateTray(bool running)
    {
        _tray.Icon = AppIcon.For(running);
        _tray.Text = running ? $"ClickerBot — running \"{_current.Name}\"" : "ClickerBot";
        Icon = _tray.Icon;
    }

    // --- Theme -----------------------------------------------------------

    private void SetTheme(ThemeMode mode)
    {
        Theme.Apply(mode);
        _store.Appearance = mode;
        ScheduleSave();
    }

    // --- Profile handling -----------------------------------------------

    private void ReloadProfileList(int selectIndex)
    {
        // Everything below indexes into the list, and the clamp itself throws when there is
        // nothing to clamp to. The app is never without a profile, so restore that first.
        if (_store.Profiles.Count == 0)
        {
            _store.Profiles.Add(new Profile { Name = "Default" });
        }

        _suspendSelection = true;
        _profileList.BeginUpdate();
        _profileList.Items.Clear();
        foreach (var profile in _store.Profiles)
        {
            _profileList.Items.Add(profile);
        }

        _profileList.EndUpdate();

        int index = Math.Clamp(selectIndex, 0, _store.Profiles.Count - 1);
        _profileList.SelectedIndex = index;
        _suspendSelection = false;

        ApplyProfile(index);
    }

    private void ProfileList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suspendSelection || _profileList.SelectedIndex < 0)
        {
            return;
        }

        ApplyProfile(_profileList.SelectedIndex);
    }

    /// <summary>Makes the profile at <paramref name="index"/> current and pushes it into the UI.</summary>
    private void ApplyProfile(int index)
    {
        _current = _store.Profiles[index];
        _store.SelectedIndex = index;

        _loading = true;
        _nameBox.Text = _current.Name;
        _modeSelector.SelectedIndex = (int)_current.Mode;
        _keyBox.Key = _current.Key;
        _typeText.Value = _current.Text;
        _buttonSelector.SelectedIndex = (int)_current.Button;
        _doubleClick.Checked = _current.DoubleClick;
        _targetSelector.SelectedIndex = (int)_current.Target;
        _xInput.Value = _current.ClickX;
        _yInput.Value = _current.ClickY;
        _scatter.Value = _current.Scatter;
        _keyDelay.Value = _current.KeyDelay;
        _clickDelay.Value = _current.ClickDelay;
        _startDelay.Value = _current.StartDelaySeconds;
        _repeatSelector.SelectedIndex = (int)_current.Repeat;
        _repetitions.Value = _current.Repetitions;
        _duration.Value = _current.DurationMinutes;
        _startHotkeyBox.Key = _current.StartHotkey;
        _pauseHotkeyBox.Key = _current.PauseHotkey;
        _stopHotkeyBox.Key = _current.StopHotkey;
        _pickHotkeyBox.Key = _current.PickHotkey;
        _loading = false;

        UpdateControlStates();
        RegisterHotkeys();
        ScheduleSave();
    }

    private void CreateProfile()
    {
        var profile = new Profile { Name = _store.CreateUniqueName("New profile") };
        _store.Profiles.Add(profile);
        ReloadProfileList(_store.Profiles.Count - 1);
        _nameBox.Focus();
        _nameBox.SelectAll();
        _runPanel.SetIdleMessage($"Created \"{profile.Name}\"");
    }

    private void DuplicateProfile()
    {
        var copy = _current.Clone();
        copy.Name = _store.CreateUniqueName(_current.Name + " copy");
        _store.Profiles.Add(copy);
        ReloadProfileList(_store.Profiles.Count - 1);
        _runPanel.SetIdleMessage($"Duplicated to \"{copy.Name}\"");
    }

    private void DeleteProfile()
    {
        if (_store.Profiles.Count == 1)
        {
            _runPanel.SetIdleMessage("At least one profile is required", TextRole.Danger);
            return;
        }

        // Resolved before the prompt: RemoveAt(-1) throws, and there is no sense asking the
        // user to confirm a deletion that cannot happen.
        int index = _store.Profiles.IndexOf(_current);
        if (index < 0)
        {
            ReloadProfileList(_store.SelectedIndex);
            return;
        }

        bool confirmed = ConfirmDialog.Ask(this, "Delete profile",
            $"\"{_current.Name}\" will be removed. This cannot be undone.", "Delete", destructive: true);
        if (!confirmed)
        {
            return;
        }

        string name = _current.Name;
        _store.Profiles.RemoveAt(index);
        ReloadProfileList(Math.Max(0, index - 1));
        _runPanel.SetIdleMessage($"Deleted \"{name}\"");
    }

    private void ImportProfiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import profiles",
            Filter = "ClickerBot profiles (*.json)|*.json|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var imported = ProfileStore.ReadProfiles(dialog.FileName);
        if (imported is null)
        {
            _runPanel.SetIdleMessage("That file does not contain any profiles", TextRole.Danger);
            return;
        }

        // Added rather than replacing: an import that silently wiped the existing profiles
        // would be one undo the app does not have.
        foreach (var profile in imported)
        {
            profile.Name = _store.CreateUniqueName(profile.Name);
            _store.Profiles.Add(profile);
        }

        ReloadProfileList(_store.Profiles.Count - 1);
        SaveNow();
        _runPanel.SetIdleMessage(
            imported.Count == 1 ? "Imported 1 profile" : $"Imported {imported.Count} profiles");
    }

    private void ExportProfiles()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export profiles",
            Filter = "ClickerBot profiles (*.json)|*.json",
            FileName = "clickerbot-profiles.json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _store.ExportTo(dialog.FileName);
            _runPanel.SetIdleMessage($"Exported {_store.Profiles.Count} profiles");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _runPanel.SetIdleMessage("Could not write that file: " + ex.Message, TextRole.Danger);
        }
    }

    private void NameBox_TextChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _current.Name = _nameBox.Text;
        _profileList.Invalidate();
        ScheduleSave();
    }

    private void NameBox_Leave(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            return;
        }

        _current.Name = _store.CreateUniqueName("Untitled");
        _nameBox.Text = _current.Name;
        _profileList.Invalidate();
    }

    // --- Settings <-> profile -------------------------------------------

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _current.Mode = (ActionMode)_modeSelector.SelectedIndex;
        _current.Key = _keyBox.Key;
        _current.Text = _typeText.Value;
        _current.Button = (ClickButton)_buttonSelector.SelectedIndex;
        _current.DoubleClick = _doubleClick.Checked;
        _current.Target = (ClickTarget)_targetSelector.SelectedIndex;
        _current.ClickX = _xInput.Value;
        _current.ClickY = _yInput.Value;
        _current.Scatter = _scatter.Value;
        _current.KeyDelay = _keyDelay.Value;
        _current.ClickDelay = _clickDelay.Value;
        _current.StartDelaySeconds = _startDelay.Value;
        _current.Repeat = (RepeatMode)_repeatSelector.SelectedIndex;
        _current.Repetitions = _repetitions.Value;
        _current.DurationMinutes = _duration.Value;

        UpdateControlStates();
        ScheduleSave();
    }

    private void HotkeyBox_Changed(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        // Read as a set: every hotkey has to be distinct from every other, so the check is
        // over the whole group rather than pairwise between the two that happen to be next
        // to each other.
        var assignments = new[]
        {
            (Box: _startHotkeyBox, Name: "Start", Current: _current.StartHotkey),
            (Box: _pauseHotkeyBox, Name: "Pause", Current: _current.PauseHotkey),
            (Box: _stopHotkeyBox, Name: "Stop", Current: _current.StopHotkey),
            (Box: _pickHotkeyBox, Name: "Pick point", Current: _current.PickHotkey),
        };

        foreach (var (box, _, previous) in assignments)
        {
            if (box.Key == Keys.None)
            {
                continue;
            }

            var clash = assignments.FirstOrDefault(
                other => !ReferenceEquals(other.Box, box) && other.Box.Key == box.Key);

            if (clash.Box is not null)
            {
                _runPanel.SetIdleMessage(
                    $"{KeyNames.Describe(box.Key)} is already the {clash.Name} hotkey", TextRole.Danger);

                _loading = true;
                box.Key = previous;
                _loading = false;
                return;
            }
        }

        _current.StartHotkey = _startHotkeyBox.Key;
        _current.PauseHotkey = _pauseHotkeyBox.Key;
        _current.StopHotkey = _stopHotkeyBox.Key;
        _current.PickHotkey = _pickHotkeyBox.Key;

        RegisterHotkeys();
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _saveTimer.Stop();

        try
        {
            _store.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _runPanel.SetIdleMessage("Could not save profiles: " + ex.Message, TextRole.Danger);
        }
    }

    // --- Hotkeys ---------------------------------------------------------

    private void RegisterHotkeys()
    {
        if (_hotkeys is null)
        {
            return;
        }

        var unavailable = new List<string>();

        void Assign(string slot, Keys key, string label)
        {
            if (!_hotkeys.Assign(slot, key))
            {
                unavailable.Add($"{KeyNames.Describe(key)} ({label})");
            }
        }

        Assign(StartHotkeyName, _current.StartHotkey, "start");
        Assign(PauseHotkeyName, _current.PauseHotkey, "pause");
        Assign(StopHotkeyName, _current.StopHotkey, "stop");
        Assign(PickHotkeyName, _current.PickHotkey, "pick point");

        if (unavailable.Count > 0)
        {
            _runPanel.SetIdleMessage(
                "Already used by another app: " + string.Join(", ", unavailable), TextRole.Danger);
        }
    }

    private void OnHotkeyPressed(string name)
    {
        switch (name)
        {
            case StartHotkeyName when !IsRunning:
                StartAutomation();
                break;
            case PauseHotkeyName when IsRunning:
                TogglePause();
                break;
            case StopHotkeyName:
                StopAutomation();
                break;
            case PickHotkeyName when !IsRunning:
                CaptureCursorPosition();
                break;
        }
    }

    // --- Actions ---------------------------------------------------------

    private void CaptureCursorPosition()
    {
        Point position = Cursor.Position;
        _xInput.Value = position.X;
        _yInput.Value = position.Y;
        _runPanel.SetIdleMessage($"Click point set to {position.X}, {position.Y}");
    }

    private void StartAutomation()
    {
        if (IsRunning)
        {
            return;
        }

        // A field the user is still typing in has not raised its change event yet, so the
        // profile would be run with the previous value. Reaching Start by hotkey never takes
        // focus off that field, which is exactly when this matters.
        CommitPendingEdits();

        if (Blocker() is string problem)
        {
            _runPanel.SetIdleMessage(problem, TextRole.Danger);
            return;
        }

        BeginRun(AutomationSettings.FromProfile(_current), isTest: false);
    }

    /// <summary>
    /// Fires exactly one iteration right now — no start delay, no repeat count, no history
    /// entry — so a setup can be checked before committing to a real run.
    /// </summary>
    private void TestAction()
    {
        if (IsRunning)
        {
            return;
        }

        CommitPendingEdits();

        if (Blocker() is string problem)
        {
            _runPanel.SetIdleMessage(problem, TextRole.Danger);
            return;
        }

        var settings = AutomationSettings.FromProfile(_current) with
        {
            Repetitions = 1,
            Duration = null,
            StartDelaySeconds = 0,
        };

        BeginRun(settings, isTest: true);
    }

    /// <summary>The reason this profile cannot run right now, or null when it can.</summary>
    private string? Blocker()
    {
        if (_current.UsesText && string.IsNullOrEmpty(_current.Text))
        {
            return "Type something first: click the text field and enter what to type";
        }

        if (_current.UsesKey && _current.Key == Keys.None)
        {
            return "Pick a key first: click the key field, then press any key";
        }

        if (!_current.UsesKey)
        {
            return null;
        }

        // A synthesized key press reaches registered hotkeys just like a real one, so a key
        // that doubles as one turns every iteration into a Stop, a Start, or a silent rewrite
        // of the click target the run is aiming at.
        var conflicts = new (Keys Key, string Name)[]
        {
            (_current.StopHotkey, "Stop"),
            (_current.StartHotkey, "Start"),
            (_current.PauseHotkey, "Pause"),
            (_current.PickHotkey, "Pick point"),
        };

        foreach (var (key, name) in conflicts)
        {
            if (key != Keys.None && key == _current.Key)
            {
                return $"{KeyNames.Describe(_current.Key)} is also the {name} hotkey — choose another key";
            }
        }

        return null;
    }

    private void BeginRun(AutomationSettings settings, bool isTest)
    {
        _cancellation = new CancellationTokenSource();
        _pause = new PauseGate();
        SaveNow();

        _runPanel.BeginRun(settings.Repetitions, settings.Duration);
        UpdateControlStates();
        UpdateTray(running: true);
        _tickTimer.Start();

        // A one-shot test hiding the window for a fraction of a second would just be a flicker.
        if (!isTest && _hideToTray.Checked)
        {
            _tray.Visible = true;
            Hide();
        }

        _run = RunAsync(settings, _pause, _cancellation.Token, isTest);
    }

    private async Task RunAsync(AutomationSettings settings, PauseGate pause, CancellationToken token, bool isTest)
    {
        DateTimeOffset startedAt = DateTimeOffset.Now;
        RunProgress? lastReport = null;

        var progress = new Progress<RunProgress>(report =>
        {
            lastReport = report;
            _runPanel.Update(report);

            // One tick per completed iteration, which is what gives the strip its rhythm.
            if (report.Phase == RunPhase.Running && report.Iteration > 0)
            {
                _runPanel.MarkIteration();
            }
        });

        string outcome;
        TextRole role;

        try
        {
            await AutomationRunner.RunAsync(settings, pause, progress, token);
            outcome = isTest ? "Test complete" : "Finished";
            role = TextRole.Success;
        }
        catch (OperationCanceledException)
        {
            outcome = _pendingStopReason ?? "Stopped";
            role = TextRole.Secondary;
        }
        catch (Exception ex)
        {
            outcome = ex.Message;
            role = TextRole.Danger;
        }
        finally
        {
            _pendingStopReason = null;
            _cancellation?.Dispose();
            _cancellation = null;
            _pause = null;
            _tickTimer.Stop();
            UpdateTray(running: false);
        }

        // Captured before RestoreFromTray potentially activates the window: a run that finished
        // while hidden should still count as "you were not watching", even though the very
        // next line is about to bring the window back.
        bool wasAway = !Visible || !ContainsFocus;

        if (!Visible)
        {
            RestoreFromTray();
        }

        if (wasAway)
        {
            SystemSounds.Asterisk.Play();
        }

        if (!isTest)
        {
            _history.Record(new RunHistoryEntry
            {
                ProfileName = _current.Name,
                Mode = settings.Mode,
                StartedAt = startedAt,
                Elapsed = lastReport?.Elapsed ?? TimeSpan.Zero,
                Iterations = lastReport?.Iteration ?? 0,
                Outcome = outcome,
            });
            SaveHistory();
        }

        _runPanel.EndRun(outcome, role);
        UpdateControlStates();
    }

    private void SaveHistory()
    {
        try
        {
            _history.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort: losing a history entry is not worth interrupting the user over.
        }
    }

    private void StopAutomation(string? reason = null)
    {
        _pendingStopReason = reason;
        _cancellation?.Cancel();
    }

    /// <summary>
    /// Aborts a run the moment the real cursor touches a screen corner — a backstop for when
    /// the configured Stop hotkey could not be registered. Slamming the mouse into a corner is
    /// a deliberate gesture (both axes clamp at once only exactly there), so it will not fire
    /// from ordinary mouse movement near an edge.
    /// </summary>
    private void CheckFailsafe()
    {
        if (!IsRunning || !_store.FailsafeEnabled)
        {
            return;
        }

        var bounds = SystemInformation.VirtualScreen;
        Point p = Cursor.Position;

        bool atCorner =
            (IsAtEdge(p.X, bounds.Left) || IsAtEdge(p.X, bounds.Right - 1)) &&
            (IsAtEdge(p.Y, bounds.Top) || IsAtEdge(p.Y, bounds.Bottom - 1));

        if (!atCorner)
        {
            return;
        }

        // The run's own click target can legitimately sit on a corner; only a position the
        // automation did not itself choose counts as reaching for the failsafe.
        bool isOwnTarget = _current.UsesMouse && _current.Target == ClickTarget.FixedPoint &&
            IsAtEdge(p.X, _current.ClickX) && IsAtEdge(p.Y, _current.ClickY);

        if (!isOwnTarget)
        {
            StopAutomation("Stopped — the mouse touched a screen corner");
        }
    }

    private static bool IsAtEdge(int value, int edge) => Math.Abs(value - edge) <= 1;

    private void TogglePause()
    {
        _pause?.Toggle();

        // The gate reports itself through the runner, but a run sitting inside a long delay
        // will not reach that report for a while. Reflect it now so the button never lies.
        if (_pause is { } gate)
        {
            _runPanel.Update(new RunProgress(
                gate.IsPaused ? RunPhase.Paused : RunPhase.Running, 0, TimeSpan.Zero, 0));
        }
    }

    private void CommitPendingEdits()
    {
        _xInput.Commit();
        _yInput.Commit();
        _scatter.Commit();
        _startDelay.Commit();
        _repetitions.Commit();
        _duration.Commit();
        _keyDelay.Commit();
        _clickDelay.Commit();
    }

    // --- State -----------------------------------------------------------

    private void UpdateControlStates()
    {
        bool running = IsRunning;

        _sidebar.Enabled = !running;
        _nameBox.Enabled = !running;

        foreach (var card in _settingCards)
        {
            card.Enabled = !running;
        }

        // Fields that do not apply to the chosen mode are disabled rather than hidden: the
        // card keeps its shape, so switching modes never makes the layout jump. The key field
        // and the text field are the one exception — TypeText needs a whole line box rather
        // than a key-capture field, so that row swaps which control occupies it instead.
        _keyLabel.Text = _current.UsesText ? "Text" : "Key";
        _keyBox.Visible = !_current.UsesText;
        _keyBox.Enabled = _current.UsesKey;
        _keyHint.Visible = !_current.UsesText;
        _typeText.Visible = _current.UsesText;
        _typeText.Enabled = _current.UsesText;

        _buttonSelector.Enabled = _current.UsesMouse;
        _doubleClick.Enabled = _current.UsesMouse;
        _targetSelector.Enabled = _current.UsesMouse;

        bool fixedPoint = _current.UsesMouse && _current.Target == ClickTarget.FixedPoint;
        _xInput.Enabled = fixedPoint;
        _yInput.Enabled = fixedPoint;
        _captureButton.Enabled = fixedPoint;
        _scatter.Enabled = fixedPoint;

        // The key delay only paces the gap between a key and a click, which only the full
        // sequence has.
        _keyDelay.Enabled = _current.Mode == ActionMode.KeyAndClick;

        bool byCount = _current.Repeat == RepeatMode.Count;
        bool byDuration = _current.Repeat == RepeatMode.Duration;
        _repetitions.Visible = byCount;
        _duration.Visible = byDuration;
        _repeatLabel.Visible = byCount || byDuration;
        _repeatLabel.Text = byCount ? "Iterations" : "Minutes";

        _repeatHint.Text = _current.Repeat switch
        {
            RepeatMode.Count => "The run ends once it has done this many iterations.",
            RepeatMode.Duration => "The run ends after this long. Paused time does not count.",
            _ => "The run keeps going until you stop it.",
        };

        _runPanel.SetStartEnabled(!running);
        _runPanel.SetTestEnabled(!running);
    }
}
