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
    private readonly ThemedLabel _profilesCaption = UiFactory.Caption(string.Empty, 20, 24, 140);
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
    private readonly Segmented _languageToggle = new();

    // Steps
    private readonly Card _stepsCard = new();
    private readonly StepListBox _stepList = new();
    private readonly ThemedLabel _noStepsLabel = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly FlatButton _moveUpButton = new();
    private readonly FlatButton _moveDownButton = new();
    private readonly FlatButton _deleteStepButton = new();
    private readonly FlatButton _recordButton = new();
    private readonly FlatButton _addStepButton = new();

    // Step detail
    private readonly Card _stepDetailCard = new();
    private readonly ThemedLabel _noStepSelectedLabel = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly ThemedComboBox _kindSelector = new();
    private readonly FlatButton _testStepButton = new();

    // Step detail — shared row controls, repositioned per StepKind by ShowStepFields.
    private readonly ThemedLabel _stepKeyLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly KeyCaptureBox _stepKeyBox = new();
    private readonly ThemedLabel _stepKeyHint = UiFactory.Hint(string.Empty, 0, 0, 72);

    private readonly ThemedLabel _stepButtonLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly Segmented _stepButtonSelector = new();
    private readonly ThemedCheckBox _stepDoubleClick = UiFactory.Check(string.Empty, 0, 0, 78);

    private readonly ThemedLabel _stepTargetLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly Segmented _stepTargetSelector = new();

    private readonly ThemedLabel _stepPointLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepXInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly NumberBox _stepYInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly FlatButton _stepPickButton = new();

    private readonly ThemedLabel _stepScatterLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepScatter =
        UiFactory.Numeric(0, 0, 84, Limits.MinScatter, Limits.MaxScatter, 0);
    private readonly ThemedLabel _stepScatterHint = UiFactory.Hint(string.Empty, 0, 0, 170);

    private readonly ThemedLabel _stepToLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepToXInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly NumberBox _stepToYInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly FlatButton _stepPickToButton = new();

    private readonly ThemedLabel _stepDragDurationLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepDragDuration =
        UiFactory.Numeric(0, 0, 84, Limits.MinDragDurationMs, Limits.MaxDragDurationMs, 250);
    private readonly ThemedLabel _stepDragDurationHint = UiFactory.Hint(string.Empty, 0, 0, 220);

    private readonly ThemedLabel _stepTextLabel = UiFactory.Label(string.Empty, 0, 0, 88);
    private readonly TextField _stepTextField = new() { MaxLength = Limits.MaxTypedTextLength };

    private readonly DelayEditor _stepDelayEditor = new();

    private readonly ThemedLabel _stepColorLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly Panel _stepColorSwatch = new();
    private readonly FlatButton _stepCaptureColorButton = new();

    private readonly ThemedLabel _stepToleranceLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepTolerance =
        UiFactory.Numeric(0, 0, 84, Limits.MinColorTolerance, Limits.MaxColorTolerance, 10);
    private readonly ThemedLabel _stepToleranceHint = UiFactory.Hint(string.Empty, 0, 0, 220);

    private readonly ThemedLabel _stepTimeoutLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly NumberBox _stepTimeout =
        UiFactory.Numeric(0, 0, 84, Limits.MinPixelTimeoutSeconds, Limits.MaxPixelTimeoutSeconds, 10);
    private readonly ThemedLabel _stepTimeoutHint = UiFactory.Hint(string.Empty, 0, 0, 220);

    private readonly ThemedLabel _stepClipboardHint = UiFactory.Hint(string.Empty, 0, 0, 380);

    // Repeat (also carries the start delay and window-targeting fields)
    private readonly Card _repeatCard = new();
    private readonly Segmented _repeatSelector = new();
    private readonly ThemedLabel _repeatLabel = UiFactory.Label(string.Empty, 0, 0, 88);
    private readonly ThemedLabel _repeatHint = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly NumberBox _repetitions =
        UiFactory.Numeric(0, 0, 118, Limits.MinRepetitions, Limits.MaxRepetitions, 100);
    private readonly NumberBox _duration =
        UiFactory.Numeric(0, 0, 118, Limits.MinDurationMinutes, Limits.MaxDurationMinutes, 5);
    private readonly ThemedLabel _startDelayLabel = UiFactory.Label(string.Empty, 0, 0, 88);
    private readonly NumberBox _startDelay = UiFactory.Numeric(
        0, 0, 84, Limits.MinStartDelaySeconds, Limits.MaxStartDelaySeconds, 3);
    private readonly ThemedLabel _startDelayHint = UiFactory.Hint(string.Empty, 0, 0, 218);
    private readonly ThemedCheckBox _restrictWindowCheck = UiFactory.Check(string.Empty, 0, 0, 404);
    private readonly TextField _windowTitleField = new() { MaxLength = 200 };
    private readonly FlatButton _useCurrentWindowButton = new();

    // Hotkeys
    private readonly Card _hotkeyCard = new();
    private readonly ThemedLabel _startHotkeyLabel = UiFactory.Label(string.Empty, 20, 50, 88);
    private readonly KeyCaptureBox _startHotkeyBox = new();
    private readonly ThemedLabel _pauseHotkeyLabel = UiFactory.Label(string.Empty, 20, 90, 88);
    private readonly KeyCaptureBox _pauseHotkeyBox = new();
    private readonly ThemedLabel _stopHotkeyLabel = UiFactory.Label(string.Empty, 20, 130, 88);
    private readonly KeyCaptureBox _stopHotkeyBox = new();
    private readonly ThemedLabel _pickHotkeyLabel = UiFactory.Label(string.Empty, 20, 170, 88);
    private readonly KeyCaptureBox _pickHotkeyBox = new();

    // Window options
    private readonly Card _windowCard = new();
    private readonly ThemedCheckBox _alwaysOnTop = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _hideToTray = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _autoStart = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedCheckBox _failsafe = UiFactory.Check(string.Empty, 0, 0, 320);

    // Remote (phone) control
    private readonly Card _remoteCard = new();
    private readonly ThemedCheckBox _remoteEnabled = UiFactory.Check(string.Empty, 0, 0, 320);
    private readonly ThemedLabel _remoteStatus = UiFactory.Label(string.Empty, 0, 0, 0, Theme.MonoSmall, TextRole.Secondary);
    private readonly RemoteControlServer _remote = new();

    // Run
    private readonly RunPanel _runPanel = new();
    private readonly List<Card> _cards = new();
    private readonly List<Card> _settingCards = new();
    private readonly NotifyIcon _tray = new();

    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 400 };
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _captureWindowTimer = new() { Interval = 1000 };

    private readonly MacroRecorder _recorder = new();

    private ProfileStore _store = new();
    private Profile _current = new();
    private RunHistoryStore _history = new();
    private HotkeyManager? _hotkeys;
    private CancellationTokenSource? _cancellation;
    private PauseGate? _pause;
    private Task? _run;
    private string? _pendingStopReason;
    private RunProgress? _lastProgress;
    private long? _remoteTarget;
    private string? _remoteMessage;
    private readonly bool _startMinimized;
    private bool _shownOnce;
    private bool _initialized;
    private bool _loading;
    private bool _suspendSelection;
    private bool _suspendStepSelection;
    private MacroStep? _selectedStep;
    private int _captureWindowCountdown;

    public MainForm(bool startMinimized = false)
    {
        _startMinimized = startMinimized;

        BuildUi();
        BuildTray();
        _saveTimer.Tick += (_, _) => SaveNow();
        _captureWindowTimer.Tick += CaptureWindowTimer_Tick;
        _recorder.StepCaptured += OnStepCaptured;

        // Repaints the cadence strip and the elapsed clock between progress reports, so a slow
        // run still shows a moving second hand rather than looking frozen. The failsafe and
        // window-focus checks ride along on the same clock rather than their own timers, since
        // all three only matter while a run is in flight.
        _tickTimer.Tick += (_, _) =>
        {
            _runPanel.Invalidate(invalidateChildren: true);
            CheckFailsafe();
            CheckWindowFocus();
        };

        // These three are invoked from the HTTP listener's own background thread, so every
        // path through them has to reach the UI thread before touching anything here.
        _remote.GetStatus = BuildRemoteStatus;
        _remote.RequestStart = () => BeginInvoke(new Action(StartAutomation));
        _remote.RequestStop = () => BeginInvoke(new Action(() => StopAutomation()));
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

            // Suppressing that first Show also means the window is never created, and OnLoad
            // only runs once it is — so without this, a sign-in launch would sit in the tray
            // with no profiles loaded, no hotkeys registered and the phone server switched off
            // until the window was opened by hand, which is the one thing this launch mode is
            // meant to avoid. Setup does not actually need the window on screen, only a handle,
            // and asking for one here is what creates it.
            EnsureInitialized();
            return;
        }

        _shownOnce = true;
        base.SetVisibleCore(value);
    }

    private bool IsRunning => _cancellation is not null;

    /// <summary>Either a run or a recording is in progress — the two moments Start/Test have to stay locked out for.</summary>
    private bool IsBusy => IsRunning || _recorder.IsRecording;

    // --- Lifecycle ------------------------------------------------------

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        EnsureInitialized();
    }

    /// <summary>
    /// Loads the saved state and brings up everything that outlives a single window: hotkeys,
    /// the theme and language, and the phone server. Idempotent, because the two paths that
    /// need it — an ordinary launch reaching OnLoad, and a <c>--minimized</c> one that never
    /// does — can both run first, and on an ordinary launch both run.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        _hotkeys = new HotkeyManager(this);
        _hotkeys.HotkeyPressed += OnHotkeyPressed;

        _store = ProfileStore.Load();
        _history = RunHistoryStore.Load();

        // Apply before the first paint, then let ThemeManager keep the tree in sync.
        Theme.Apply(_store.Appearance);
        Loc.Apply(_store.Language);
        ThemeManager.Attach(this, ApplyLanguage);

        _loading = true;
        _alwaysOnTop.Checked = _store.AlwaysOnTop;
        _hideToTray.Checked = _store.HideToTrayWhileRunning;
        _failsafe.Checked = _store.FailsafeEnabled;
        // The registry, not a stored flag, is the source of truth — see StartupManager.
        _autoStart.Checked = StartupManager.IsEnabled();
        _remoteEnabled.Checked = _store.RemoteControlEnabled;
        _languageToggle.SelectedIndex = (int)_store.Language;
        _loading = false;
        TopMost = _store.AlwaysOnTop;

        if (_store.RemoteControlEnabled)
        {
            StartRemoteControl();
        }

        UpdateRemoteStatusLabel();

        ReloadProfileList(_store.SelectedIndex);
        _runPanel.SetIdleMessage(Loc.Ready);
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

        _recorder.Stop();
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
            _captureWindowTimer.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _hotkeys?.Dispose();
            _hotkeys = null;
            _cancellation?.Dispose();
            _cancellation = null;
            _recorder.Dispose();
            _remote.Dispose();
            AppIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    // --- Tray and window options ------------------------------------------

    private void BuildTray()
    {
        _tray.Icon = AppIcon.Idle;
        _tray.Text = Loc.TrayIdle;
        _tray.DoubleClick += (_, _) => RestoreFromTray();

        // Built here as well as from ApplyLanguage, so the icon is never live without a menu
        // behind it: a --minimized launch shows the tray icon from SetVisibleCore, which runs
        // ahead of the form's load.
        RebuildTrayMenu();
    }

    /// <summary>
    /// Rebuilt fresh rather than kept as named fields: a tray context menu is only ever
    /// opened, never referenced, so there is nothing to gain by holding onto the individual
    /// items the way every visible control's field is held. Called from
    /// <see cref="ApplyLanguage"/>, which already runs once at startup and again on every
    /// switch — see <see cref="ThemeManager.Attach"/> — so this needs no subscription of
    /// its own to keep in sync, and nothing extra to unhook on close either.
    /// </summary>
    private void RebuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(Loc.TrayShow, null, (_, _) => RestoreFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Loc.TrayStopRun, null, (_, _) => StopAutomation());
        menu.Items.Add(Loc.TrayQuit, null, (_, _) => Close());

        var old = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = menu;
        old?.Dispose();
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

            _runPanel.SetIdleMessage(Loc.CouldNotChangeStartup(ex.Message), TextRole.Danger);
        }
    }

    // --- Remote (phone) control -------------------------------------------

    private void ApplyRemoteControl()
    {
        if (_loading)
        {
            return;
        }

        _store.RemoteControlEnabled = _remoteEnabled.Checked;
        ScheduleSave();

        if (_remoteEnabled.Checked)
        {
            StartRemoteControl();
        }
        else
        {
            _remote.Stop();
        }

        UpdateRemoteStatusLabel();
    }

    /// <summary>
    /// Starts the server, reverting the checkbox and the saved setting if it fails — a checkbox
    /// that stays checked after the thing it turns on failed to turn on is worse than one that
    /// visibly refuses. Shared by the checkbox handler and the auto-start-on-launch path.
    /// </summary>
    private void StartRemoteControl()
    {
        string? error = _remote.Start();
        if (error is null)
        {
            return;
        }

        _loading = true;
        _remoteEnabled.Checked = false;
        _loading = false;

        _store.RemoteControlEnabled = false;
        ScheduleSave();

        _runPanel.SetIdleMessage(error, TextRole.Danger);
    }

    private void UpdateRemoteStatusLabel()
    {
        if (!_remote.IsRunning)
        {
            _remoteStatus.Text = Loc.RemoteOff;
            _remoteStatus.Cursor = Cursors.Default;
            return;
        }

        string url = _remote.Urls.FirstOrDefault() ?? $"http://127.0.0.1:{RemoteControlServer.Port}/";
        _remoteStatus.Text = $"{url}   PIN {_remote.Pin}";
        _remoteStatus.Cursor = Cursors.Hand;
    }

    private void CopyRemoteUrl()
    {
        if (!_remote.IsRunning)
        {
            return;
        }

        string url = _remote.Urls.FirstOrDefault() ?? $"http://127.0.0.1:{RemoteControlServer.Port}/";
        Clipboard.SetText(url);
        _runPanel.SetIdleMessage(Loc.RemoteCopiedMessage);
    }

    /// <summary>
    /// Built on demand for the phone's status poll. Called from the HTTP listener's background
    /// thread, so every field it touches is read on the UI thread first.
    /// </summary>
    private RemoteStatusPayload BuildRemoteStatus()
    {
        if (InvokeRequired)
        {
            return (RemoteStatusPayload)Invoke(new Func<RemoteStatusPayload>(BuildRemoteStatus));
        }

        bool running = IsRunning;
        int count = _current.Steps.Count;
        // Kept bare, not through Loc.T: this rides inside the phone page's own JSON, the same
        // reasoning that keeps its inline labels un-wrapped — see RemoteControlServer.
        string stepSummary = Loc.IsPersian ? $"{count} مرحله" : $"{count} step{(count == 1 ? "" : "s")}";

        return new RemoteStatusPayload(
            running,
            _current.Name,
            stepSummary,
            _lastProgress?.Iteration ?? 0,
            running ? _remoteTarget : null,
            _lastProgress?.Elapsed.TotalSeconds ?? 0,
            running ? string.Empty : _remoteMessage ?? Loc.Ready);
    }

    private void UpdateTray(bool running)
    {
        _tray.Icon = AppIcon.For(running);
        _tray.Text = running ? Loc.TrayRunning(_current.Name) : Loc.TrayIdle;
        Icon = _tray.Icon;
    }

    // --- Theme -----------------------------------------------------------

    private void SetTheme(ThemeMode mode)
    {
        Theme.Apply(mode);
        _store.Appearance = mode;
        ScheduleSave();
    }

    // --- Language ----------------------------------------------------------

    private void SetLanguage(Language language)
    {
        if (_loading)
        {
            return;
        }

        Loc.Apply(language);
        _store.Language = language;
        ScheduleSave();
    }

    /// <summary>
    /// Every static string this window shows, freshly read from <see cref="Loc"/>. Run once
    /// at startup and again on every language switch — see <see cref="ThemeManager.Attach"/>,
    /// which calls it and then walks the tree the same way a theme switch does, so this can
    /// set text without worrying about repaint timing or flicker.
    ///
    /// Text that is computed straight from state on every paint — the tray tooltip, a card's
    /// live run status, a history row's outcome — reads <see cref="Loc"/> directly at the
    /// point it is drawn instead, and needs no entry here.
    /// </summary>
    private void ApplyLanguage()
    {
        _renameHint.Text = Loc.RenameHint;

        _profilesCaption.Text = Loc.ProfilesCaption;
        _newButton.Text = Loc.NewProfile;
        _duplicateButton.Text = Loc.Duplicate;
        _deleteButton.Text = Loc.Delete;
        _importButton.Text = Loc.Import;
        _exportButton.Text = Loc.Export;
        _historyButton.Text = Loc.History;

        _stepsCard.Title = Loc.StepsCardTitle;
        _noStepsLabel.Text = Loc.NoStepsYet;
        _moveUpButton.Text = "▲";
        _moveDownButton.Text = "▼";
        _deleteStepButton.Text = Loc.DeleteStep;
        _recordButton.Text = _recorder.IsRecording ? Loc.StopRecording : Loc.Record;
        _addStepButton.Text = Loc.AddStep;

        _stepDetailCard.Title = Loc.StepDetailCardTitle;
        _noStepSelectedLabel.Text = Loc.NoStepSelected;
        _testStepButton.Text = Loc.TestStep;

        ApplyKindSelectorItems();

        _stepKeyBox.Placeholder = Loc.KeyPlaceholder;
        _stepKeyHint.Text = Loc.EscClears;
        _stepButtonLabel.Text = Loc.ButtonLabel;
        _stepButtonSelector.Items = Loc.MouseButtonItems;
        _stepDoubleClick.Text = Loc.DoubleClick;
        _stepTargetLabel.Text = Loc.ClickAtLabel;
        _stepTargetSelector.Items = Loc.ClickTargetItems;
        _stepPointLabel.Text = Loc.PointLabel;
        _stepPickButton.Text = Loc.Pick;
        _stepScatterLabel.Text = Loc.ScatterLabel;
        _stepScatterHint.Text = Loc.ScatterHint;
        _stepToLabel.Text = Loc.ToLabel;
        _stepPickToButton.Text = Loc.Pick;
        _stepDragDurationLabel.Text = Loc.DragDurationLabel;
        _stepDragDurationHint.Text = Loc.DragDurationHint;
        _stepColorLabel.Text = Loc.ColorLabel;
        _stepCaptureColorButton.Text = Loc.CaptureColor;
        _stepToleranceLabel.Text = Loc.ToleranceLabel;
        _stepToleranceHint.Text = Loc.ToleranceHint;
        _stepTimeoutLabel.Text = Loc.TimeoutLabel;
        _stepTimeoutHint.Text = Loc.TimeoutHint;
        _stepClipboardHint.Text = Loc.ClipboardPasteHint;

        _repeatCard.Title = Loc.RepeatCardTitle;
        _repeatSelector.Items = Loc.RepeatItems;
        _startDelayLabel.Text = Loc.StartDelayLabel;
        _startDelayHint.Text = Loc.StartDelayHint;
        _restrictWindowCheck.Text = Loc.RestrictToWindow;
        _windowTitleField.Placeholder = Loc.WindowTitlePlaceholder;
        if (!_captureWindowTimer.Enabled)
        {
            _useCurrentWindowButton.Text = Loc.UseCurrentWindow;
        }

        _hotkeyCard.Title = Loc.HotkeysCardTitle;
        _startHotkeyLabel.Text = Loc.Start;
        _pauseHotkeyLabel.Text = Loc.Pause;
        _stopHotkeyLabel.Text = Loc.Stop;
        _pickHotkeyLabel.Text = Loc.PickPoint;
        _startHotkeyBox.Placeholder = Loc.NotSet;
        _pauseHotkeyBox.Placeholder = Loc.NotSet;
        _stopHotkeyBox.Placeholder = Loc.NotSet;
        _pickHotkeyBox.Placeholder = Loc.NotSet;

        _windowCard.Title = Loc.WindowCardTitle;
        _alwaysOnTop.Text = Loc.AlwaysOnTop;
        _hideToTray.Text = Loc.HideToTray;
        _autoStart.Text = Loc.AutoStart;
        _failsafe.Text = Loc.Failsafe;

        _remoteCard.Title = Loc.RemoteCardTitle;
        _remoteEnabled.Text = Loc.RemoteEnabledCheck;

        RebuildTrayMenu();
        UpdateTray(IsRunning);

        // State-dependent text (the step-kind field layout, the Iterations/Minutes swap, the
        // remote status line) is re-derived rather than duplicated here.
        UpdateControlStates();
        UpdateRemoteStatusLabel();
        _stepList.Invalidate();
    }

    /// <summary>
    /// Rebuilds the kind combo's items from <see cref="Loc.StepKindName"/> without disturbing
    /// which one is selected: the enum's declaration order never changes, so the index a
    /// language switch would otherwise clobber is exactly the one to restore afterwards.
    /// </summary>
    private void ApplyKindSelectorItems()
    {
        _loading = true;
        int previous = _kindSelector.SelectedIndex;
        _kindSelector.Items.Clear();
        _kindSelector.Items.AddRange(Enum.GetValues<StepKind>().Select(k => (object)Loc.StepKindName(k)).ToArray());
        _kindSelector.SelectedIndex = previous >= 0 && previous < _kindSelector.Items.Count ? previous : -1;
        _loading = false;
    }

    // --- Profile handling -----------------------------------------------

    private void ReloadProfileList(int selectIndex)
    {
        // Everything below indexes into the list, and the clamp itself throws when there is
        // nothing to clamp to. The app is never without a profile, so restore that first.
        if (_store.Profiles.Count == 0)
        {
            _store.Profiles.Add(new Profile { Name = Loc.DefaultProfileName });
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
        if (_recorder.IsRecording)
        {
            ToggleRecording();
        }

        _current = _store.Profiles[index];
        _store.SelectedIndex = index;

        _loading = true;
        _nameBox.Text = _current.Name;
        _startDelay.Value = _current.StartDelaySeconds;
        _repeatSelector.SelectedIndex = (int)_current.Repeat;
        _repetitions.Value = _current.Repetitions;
        _duration.Value = _current.DurationMinutes;
        _restrictWindowCheck.Checked = _current.RequireTargetWindow;
        _windowTitleField.Value = _current.TargetWindowTitle;
        _startHotkeyBox.Key = _current.StartHotkey;
        _pauseHotkeyBox.Key = _current.PauseHotkey;
        _stopHotkeyBox.Key = _current.StopHotkey;
        _pickHotkeyBox.Key = _current.PickHotkey;
        _loading = false;

        RefreshStepList(0);
        UpdateControlStates();
        RegisterHotkeys();
        ScheduleSave();
    }

    private void CreateProfile()
    {
        var profile = new Profile { Name = _store.CreateUniqueName(Loc.NewProfile) };
        _store.Profiles.Add(profile);
        ReloadProfileList(_store.Profiles.Count - 1);
        _nameBox.Focus();
        _nameBox.SelectAll();
        _runPanel.SetIdleMessage(Loc.CreatedProfile(profile.Name));
    }

    private void DuplicateProfile()
    {
        var copy = _current.Clone();
        copy.Name = _store.CreateUniqueName(_current.Name + Loc.CopySuffix);
        _store.Profiles.Add(copy);
        ReloadProfileList(_store.Profiles.Count - 1);
        _runPanel.SetIdleMessage(Loc.DuplicatedProfile(copy.Name));
    }

    private void DeleteProfile()
    {
        if (_store.Profiles.Count == 1)
        {
            _runPanel.SetIdleMessage(Loc.AtLeastOneProfileRequired, TextRole.Danger);
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

        bool confirmed = ConfirmDialog.Ask(this, Loc.DeleteProfileTitle,
            Loc.DeleteProfileMessage(_current.Name), Loc.Delete, destructive: true);
        if (!confirmed)
        {
            return;
        }

        string name = _current.Name;
        _store.Profiles.RemoveAt(index);
        ReloadProfileList(Math.Max(0, index - 1));
        _runPanel.SetIdleMessage(Loc.DeletedProfile(name));
    }

    private void ImportProfiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Loc.ImportDialogTitle,
            Filter = $"{Loc.ProfilesFilterName} (*.json)|*.json|{Loc.AllFilesFilterName} (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var imported = ProfileStore.ReadProfiles(dialog.FileName);
        if (imported is null)
        {
            _runPanel.SetIdleMessage(Loc.NoProfilesInFile, TextRole.Danger);
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
        _runPanel.SetIdleMessage(Loc.ImportedProfiles(imported.Count));
    }

    private void ExportProfiles()
    {
        using var dialog = new SaveFileDialog
        {
            Title = Loc.ExportDialogTitle,
            Filter = $"{Loc.ProfilesFilterName} (*.json)|*.json",
            FileName = "clickerbot-profiles.json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _store.ExportTo(dialog.FileName);
            _runPanel.SetIdleMessage(Loc.ExportedProfiles(_store.Profiles.Count));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _runPanel.SetIdleMessage(Loc.CouldNotWriteFile(ex.Message), TextRole.Danger);
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

        _current.Name = _store.CreateUniqueName(Loc.UntitledProfileName);
        _nameBox.Text = _current.Name;
        _profileList.Invalidate();
    }

    // --- Steps list -------------------------------------------------------

    /// <summary>Rebuilds the step list from <see cref="_current"/>'s steps and selects <paramref name="selectIndex"/>.</summary>
    private void RefreshStepList(int selectIndex)
    {
        _suspendStepSelection = true;
        _stepList.BeginUpdate();
        _stepList.Items.Clear();
        foreach (var step in _current.Steps)
        {
            _stepList.Items.Add(step);
        }

        _stepList.EndUpdate();

        bool empty = _current.Steps.Count == 0;
        _stepList.Visible = !empty;
        _noStepsLabel.Visible = empty;

        int index = empty ? -1 : Math.Clamp(selectIndex, 0, _current.Steps.Count - 1);
        _stepList.SelectedIndex = index;
        _suspendStepSelection = false;

        ApplySelectedStep(index);
    }

    private void StepList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suspendStepSelection)
        {
            return;
        }

        ApplySelectedStep(_stepList.SelectedIndex);
    }

    /// <summary>Loads the step at <paramref name="index"/> (or none) into the detail editor.</summary>
    private void ApplySelectedStep(int index)
    {
        _selectedStep = index >= 0 && index < _current.Steps.Count ? _current.Steps[index] : null;

        bool running = IsBusy;
        bool hasSelection = _selectedStep is not null;
        bool hasSteps = _current.Steps.Count > 0;

        _moveUpButton.Enabled = !running && hasSelection && index > 0;
        _moveDownButton.Enabled = !running && hasSelection && index < _current.Steps.Count - 1;
        _deleteStepButton.Enabled = !running && hasSelection;

        _noStepSelectedLabel.Visible = !hasSelection;
        _kindSelector.Visible = hasSelection;
        _testStepButton.Visible = hasSelection;
        _testStepButton.Enabled = !running && hasSelection;

        if (_selectedStep is not { } step)
        {
            HideAllStepFields();
            return;
        }

        _loading = true;
        _kindSelector.SelectedIndex = (int)step.Kind;
        _stepKeyBox.Key = step.Key;
        _stepButtonSelector.SelectedIndex = (int)step.Button;
        _stepDoubleClick.Checked = step.DoubleClick;
        _stepTargetSelector.SelectedIndex = (int)step.Target;
        _stepXInput.Value = step.X;
        _stepYInput.Value = step.Y;
        _stepScatter.Value = step.Scatter;
        _stepToXInput.Value = step.DragToX;
        _stepToYInput.Value = step.DragToY;
        _stepDragDuration.Value = step.DragDurationMs;
        _stepTextField.Value = step.Text;
        _stepDelayEditor.Value = step.Delay;
        _stepColorSwatch.BackColor = step.TargetColor;
        _stepTolerance.Value = step.ColorTolerance;
        _stepTimeout.Value = step.TimeoutSeconds;
        _loading = false;

        ShowStepFields(step.Kind);
        _ = hasSteps;
    }

    private void AddStep()
    {
        if (_current.Steps.Count >= Limits.MaxSteps)
        {
            return;
        }

        _current.Steps.Add(new MacroStep());
        RefreshStepList(_current.Steps.Count - 1);
        ScheduleSave();
    }

    private void MoveStepUp()
    {
        int index = _stepList.SelectedIndex;
        if (index <= 0)
        {
            return;
        }

        (_current.Steps[index - 1], _current.Steps[index]) = (_current.Steps[index], _current.Steps[index - 1]);
        RefreshStepList(index - 1);
        ScheduleSave();
    }

    private void MoveStepDown()
    {
        int index = _stepList.SelectedIndex;
        if (index < 0 || index >= _current.Steps.Count - 1)
        {
            return;
        }

        (_current.Steps[index + 1], _current.Steps[index]) = (_current.Steps[index], _current.Steps[index + 1]);
        RefreshStepList(index + 1);
        ScheduleSave();
    }

    private void DeleteStep()
    {
        int index = _stepList.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        _current.Steps.RemoveAt(index);
        RefreshStepList(Math.Min(index, _current.Steps.Count - 1));
        ScheduleSave();
    }

    private void ToggleRecording()
    {
        if (_recorder.IsRecording)
        {
            _recorder.Stop();
            _recordButton.Text = Loc.Record;
            _recordButton.Kind = ButtonKind.Secondary;
            _runPanel.SetIdleMessage(Loc.Ready);
        }
        else
        {
            if (IsRunning)
            {
                return;
            }

            _recorder.Start();
            _recordButton.Text = Loc.StopRecording;
            _recordButton.Kind = ButtonKind.Danger;
            _runPanel.SetIdleMessage(Loc.RecordingStatus, TextRole.Accent);
        }

        UpdateControlStates();
    }

    /// <summary>Appends a step the recorder just captured live. See <see cref="MacroRecorder"/>.</summary>
    private void OnStepCaptured(MacroStep step)
    {
        if (_current.Steps.Count >= Limits.MaxSteps)
        {
            ToggleRecording();
            return;
        }

        _current.Steps.Add(step);
        RefreshStepList(_current.Steps.Count - 1);
        ScheduleSave();
    }

    /// <summary>Writes every shared field back into the selected step. Kind is changed separately — see <see cref="KindSelector_SelectedIndexChanged"/>.</summary>
    private void SyncStepFieldsToModel()
    {
        if (_loading || _selectedStep is not { } step)
        {
            return;
        }

        step.Key = _stepKeyBox.Key;
        step.Button = (ClickButton)_stepButtonSelector.SelectedIndex;
        step.DoubleClick = _stepDoubleClick.Checked;
        step.Target = (ClickTarget)_stepTargetSelector.SelectedIndex;
        step.X = _stepXInput.Value;
        step.Y = _stepYInput.Value;
        step.Scatter = _stepScatter.Value;
        step.DragToX = _stepToXInput.Value;
        step.DragToY = _stepToYInput.Value;
        step.DragDurationMs = _stepDragDuration.Value;
        step.Text = _stepTextField.Value;
        step.Delay = _stepDelayEditor.Value;
        step.ColorTolerance = _stepTolerance.Value;
        step.TimeoutSeconds = _stepTimeout.Value;

        _stepList.Invalidate();
        ScheduleSave();
    }

    private void KindSelector_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loading || _selectedStep is not { } step)
        {
            return;
        }

        step.Kind = (StepKind)_kindSelector.SelectedIndex;
        ShowStepFields(step.Kind);
        _stepList.Invalidate();
        ScheduleSave();
    }

    private void PickStepPoint()
    {
        if (_selectedStep is not { } step)
        {
            return;
        }

        Point position = Cursor.Position;
        _stepXInput.Value = position.X;
        _stepYInput.Value = position.Y;

        if (step.Kind == StepKind.WaitForPixelColor)
        {
            Color color = NativeInput.SamplePixel(position);
            step.TargetColorArgb = color.ToArgb();
            _stepColorSwatch.BackColor = color;
        }

        _runPanel.SetIdleMessage(Loc.ClickPointSet(position.X, position.Y));
    }

    private void PickStepDragTo()
    {
        if (_selectedStep is null)
        {
            return;
        }

        Point position = Cursor.Position;
        _stepToXInput.Value = position.X;
        _stepToYInput.Value = position.Y;
        _runPanel.SetIdleMessage(Loc.ClickPointSet(position.X, position.Y));
    }

    private void CaptureStepColor()
    {
        if (_selectedStep is not { } step)
        {
            return;
        }

        Color color = NativeInput.SamplePixel(Cursor.Position);
        step.TargetColorArgb = color.ToArgb();
        _stepColorSwatch.BackColor = color;
        ScheduleSave();
    }

    private void TestStep()
    {
        if (IsBusy || _selectedStep is not { } step)
        {
            return;
        }

        CommitPendingEdits();
        _ = RunStepOnceAsync(step.Clone());
    }

    private async Task RunStepOnceAsync(MacroStep step)
    {
        _testStepButton.Enabled = false;

        try
        {
            await AutomationRunner.RunStepOnceAsync(step, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _runPanel.SetIdleMessage(ex.Message, TextRole.Danger);
        }
        finally
        {
            _testStepButton.Enabled = !IsBusy && _selectedStep is not null;
        }
    }

    // --- Settings <-> profile -------------------------------------------

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _current.StartDelaySeconds = _startDelay.Value;
        _current.Repeat = (RepeatMode)_repeatSelector.SelectedIndex;
        _current.Repetitions = _repetitions.Value;
        _current.DurationMinutes = _duration.Value;

        UpdateControlStates();
        ScheduleSave();
    }

    private void RestrictWindowCheck_CheckedChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _current.RequireTargetWindow = _restrictWindowCheck.Checked;
        UpdateControlStates();
        ScheduleSave();
    }

    private void WindowTitleField_ValueChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _current.TargetWindowTitle = _windowTitleField.Value;
        ScheduleSave();
    }

    /// <summary>
    /// Starts a short countdown, then captures whatever window is in front once it ends.
    ///
    /// Clicking this button makes ClickerBot itself the foreground window before the click
    /// handler ever runs, so there is no "previous window" left to read at the moment of the
    /// click — a countdown, giving time to switch to the intended window by hand, is what
    /// makes the button able to do anything at all.
    /// </summary>
    private void BeginCaptureWindowTitle()
    {
        if (_captureWindowTimer.Enabled)
        {
            return;
        }

        _captureWindowCountdown = 3;
        _useCurrentWindowButton.Enabled = false;
        _useCurrentWindowButton.Text = Loc.SwitchNowCountdown(_captureWindowCountdown);
        _captureWindowTimer.Start();
    }

    private void CaptureWindowTimer_Tick(object? sender, EventArgs e)
    {
        _captureWindowCountdown--;
        if (_captureWindowCountdown > 0)
        {
            _useCurrentWindowButton.Text = Loc.SwitchNowCountdown(_captureWindowCountdown);
            return;
        }

        _captureWindowTimer.Stop();
        _useCurrentWindowButton.Enabled = _restrictWindowCheck.Checked && !IsBusy;
        _useCurrentWindowButton.Text = Loc.UseCurrentWindow;

        string title = ForegroundWindow.Title();
        if (!string.IsNullOrWhiteSpace(title))
        {
            _windowTitleField.Value = title;
        }
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
            (Box: _startHotkeyBox, Name: Loc.Start, Current: _current.StartHotkey),
            (Box: _pauseHotkeyBox, Name: Loc.Pause, Current: _current.PauseHotkey),
            (Box: _stopHotkeyBox, Name: Loc.Stop, Current: _current.StopHotkey),
            (Box: _pickHotkeyBox, Name: Loc.PickPoint, Current: _current.PickHotkey),
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
                _runPanel.SetIdleMessage(Loc.HotkeyClash(KeyNames.Describe(box.Key), clash.Name), TextRole.Danger);

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
            _runPanel.SetIdleMessage(Loc.CouldNotSaveProfiles(ex.Message), TextRole.Danger);
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

        Assign(StartHotkeyName, _current.StartHotkey, Loc.Start);
        Assign(PauseHotkeyName, _current.PauseHotkey, Loc.Pause);
        Assign(StopHotkeyName, _current.StopHotkey, Loc.Stop);
        Assign(PickHotkeyName, _current.PickHotkey, Loc.PickPoint);

        if (unavailable.Count > 0)
        {
            _runPanel.SetIdleMessage(Loc.HotkeyUnavailable(string.Join(", ", unavailable)), TextRole.Danger);
        }
    }

    private void OnHotkeyPressed(string name)
    {
        switch (name)
        {
            case StartHotkeyName when !IsBusy:
                StartAutomation();
                break;
            case PauseHotkeyName when IsRunning:
                TogglePause();
                break;
            case StopHotkeyName:
                StopAutomation();
                break;
            case PickHotkeyName when !IsBusy:
                CaptureCursorPosition();
                break;
        }
    }

    // --- Actions ---------------------------------------------------------

    /// <summary>
    /// The Pick hotkey's target: whichever point field the selected step is currently showing.
    /// A Drag step has two points — this always captures the "from" one, since that is the
    /// point every other kind has just one of; the "to" point has its own button in the editor.
    /// </summary>
    private void CaptureCursorPosition()
    {
        if (_selectedStep is { } step && step.Kind is StepKind.Click or StepKind.Drag or StepKind.WaitForPixelColor)
        {
            PickStepPoint();
        }
    }

    private void StartAutomation()
    {
        if (IsBusy)
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
            _remoteMessage = problem;
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
        if (IsBusy)
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
        if (_current.Steps.Count == 0)
        {
            return Loc.BlockerNoSteps;
        }

        foreach (var step in _current.Steps)
        {
            if (step.Kind == StepKind.TypeText && string.IsNullOrEmpty(step.Text))
            {
                return Loc.BlockerNoText;
            }

            if (step.Kind != StepKind.KeyPress)
            {
                continue;
            }

            if (step.Key == Keys.None)
            {
                return Loc.BlockerNoKey;
            }

            // A synthesized key press reaches registered hotkeys just like a real one, so a key
            // that doubles as one turns every iteration into a Stop, a Start, or a silent
            // rewrite of whatever the run is aiming at.
            var conflicts = new (Keys Key, string Name)[]
            {
                (_current.StopHotkey, Loc.Stop),
                (_current.StartHotkey, Loc.Start),
                (_current.PauseHotkey, Loc.Pause),
                (_current.PickHotkey, Loc.PickPoint),
            };

            foreach (var (key, name) in conflicts)
            {
                if (key != Keys.None && key == step.Key)
                {
                    return Loc.KeyIsHotkey(KeyNames.Describe(step.Key), name);
                }
            }
        }

        return null;
    }

    private void BeginRun(AutomationSettings settings, bool isTest)
    {
        _cancellation = new CancellationTokenSource();
        _pause = new PauseGate();
        SaveNow();

        _lastProgress = null;
        _remoteTarget = settings.Repetitions;

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

        var progress = new Progress<RunProgress>(report =>
        {
            _lastProgress = report;
            _runPanel.Update(report);

            // One tick per completed iteration, which is what gives the strip its rhythm.
            if (report.Phase == RunPhase.Running && report.Iteration > 0)
            {
                _runPanel.MarkIteration();
            }
        });

        // Kept in canonical English regardless of the active language: this is what lands in
        // history.json and the phone API's raw message, and HistoryListBox matches its color
        // against these same constants. Loc.DescribeOutcome renders it for display, below.
        string outcome;
        TextRole role;

        try
        {
            await AutomationRunner.RunAsync(settings, pause, progress, token);
            outcome = isTest ? Loc.OutcomeTestComplete : Loc.OutcomeFinished;
            role = TextRole.Success;
        }
        catch (OperationCanceledException)
        {
            outcome = _pendingStopReason ?? Loc.OutcomeStopped;
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

        _remoteMessage = Loc.DescribeOutcome(outcome);

        if (!isTest)
        {
            _history.Record(new RunHistoryEntry
            {
                ProfileName = _current.Name,
                StepCount = settings.Steps.Count,
                StartedAt = startedAt,
                Elapsed = _lastProgress?.Elapsed ?? TimeSpan.Zero,
                Iterations = _lastProgress?.Iteration ?? 0,
                Outcome = outcome,
            });
            SaveHistory();
        }

        _runPanel.EndRun(Loc.DescribeOutcome(outcome), role);
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

        // A run's own click or drag target can legitimately sit on a corner; only a position
        // none of its steps chose counts as reaching for the failsafe.
        bool isOwnTarget = _current.Steps.Any(step =>
            (step.Kind == StepKind.Click && step.Target == ClickTarget.FixedPoint &&
                IsAtEdge(p.X, step.X) && IsAtEdge(p.Y, step.Y)) ||
            (step.Kind == StepKind.Drag &&
                ((IsAtEdge(p.X, step.X) && IsAtEdge(p.Y, step.Y)) ||
                 (IsAtEdge(p.X, step.DragToX) && IsAtEdge(p.Y, step.DragToY)))));

        if (!isOwnTarget)
        {
            StopAutomation(Loc.OutcomeFailsafeStopped);
        }
    }

    /// <summary>
    /// Keeps the run's <see cref="PauseGate"/> in sync with whether the target window is
    /// actually in front, for profiles with <see cref="Profile.RequireTargetWindow"/> set.
    /// Polled from the same tick timer as <see cref="CheckFailsafe"/>, never touched by the
    /// run itself — see <see cref="PauseGate.SetWindowFocused"/>.
    /// </summary>
    private void CheckWindowFocus()
    {
        if (!IsRunning || _pause is not { } gate)
        {
            return;
        }

        if (!_current.RequireTargetWindow)
        {
            gate.SetWindowFocused(true);
            return;
        }

        gate.SetWindowFocused(ForegroundWindow.Matches(_current.TargetWindowTitle));

        if (gate.IsWaitingForWindow)
        {
            _runPanel.SetPhase(RunPhase.WaitingForWindow);
        }
        else if (!gate.IsPaused)
        {
            _runPanel.SetPhase(RunPhase.Running);
        }
    }

    private static bool IsAtEdge(int value, int edge) => Math.Abs(value - edge) <= 1;

    private void TogglePause()
    {
        _pause?.Toggle();

        // The gate reports itself through the runner, but a run sitting inside a long delay
        // will not reach that report for a while. Reflect it now so the button never lies —
        // phase only, since the iteration count and elapsed time are the run's to report and
        // this side does not know them.
        if (_pause is { } gate)
        {
            _runPanel.SetPhase(gate.IsWaitingForWindow ? RunPhase.WaitingForWindow
                : gate.IsPaused ? RunPhase.Paused
                : RunPhase.Running);
        }
    }

    private void CommitPendingEdits()
    {
        _startDelay.Commit();
        _repetitions.Commit();
        _duration.Commit();
        _stepXInput.Commit();
        _stepYInput.Commit();
        _stepScatter.Commit();
        _stepToXInput.Commit();
        _stepToYInput.Commit();
        _stepDragDuration.Commit();
        _stepTolerance.Commit();
        _stepTimeout.Commit();
        _stepDelayEditor.Commit();
    }

    // --- State -----------------------------------------------------------

    /// <summary>Hides every per-kind row control. <see cref="ShowStepFields"/> reveals only what the selected kind needs.</summary>
    private void HideAllStepFields()
    {
        foreach (Control control in new Control[]
        {
            _stepKeyLabel, _stepKeyBox, _stepKeyHint,
            _stepButtonLabel, _stepButtonSelector, _stepDoubleClick,
            _stepTargetLabel, _stepTargetSelector,
            _stepPointLabel, _stepXInput, _stepYInput, _stepPickButton,
            _stepScatterLabel, _stepScatter, _stepScatterHint,
            _stepToLabel, _stepToXInput, _stepToYInput, _stepPickToButton,
            _stepDragDurationLabel, _stepDragDuration, _stepDragDurationHint,
            _stepTextLabel, _stepTextField,
            _stepDelayEditor,
            _stepColorLabel, _stepColorSwatch, _stepCaptureColorButton,
            _stepToleranceLabel, _stepTolerance, _stepToleranceHint,
            _stepTimeoutLabel, _stepTimeout, _stepTimeoutHint,
            _stepClipboardHint,
        })
        {
            control.Visible = false;
        }
    }

    /// <summary>
    /// Shows and positions exactly the controls <paramref name="kind"/> needs, reusing the same
    /// instance of each shared control (the point fields, the Pick button, the button selector…)
    /// across every kind that needs one rather than keeping a separate copy per kind — the same
    /// approach the pre-macro Action card used for its Key/Text swap, extended to more rows.
    /// </summary>
    private void ShowStepFields(StepKind kind)
    {
        HideAllStepFields();

        const int Row1 = 76;
        const int Row2 = 120;
        const int Row3 = 164;
        const int Row4 = 208;

        void ShowButtonRow(int y)
        {
            _stepButtonLabel.SetBounds(20, y + 6, 76, 20);
            _stepButtonSelector.SetBounds(100, y, 174, 30);
            _stepButtonLabel.Visible = true;
            _stepButtonSelector.Visible = true;
        }

        void ShowPointRow(int y, FlatButton pickButton, NumberBox xInput, NumberBox yInput, ThemedLabel label)
        {
            label.SetBounds(20, y + 6, 76, 20);
            xInput.SetBounds(100, y, 84, 32);
            yInput.SetBounds(190, y, 84, 32);
            pickButton.SetBounds(282, y - 1, 72, 30);
            label.Visible = true;
            xInput.Visible = true;
            yInput.Visible = true;
            pickButton.Visible = true;
        }

        switch (kind)
        {
            case StepKind.KeyPress:
                _stepKeyLabel.SetBounds(20, Row1 + 6, 76, 20);
                _stepKeyBox.SetBounds(100, Row1, 174, 32);
                _stepKeyHint.SetBounds(282, Row1 + 6, 72, 20);
                _stepKeyLabel.Visible = true;
                _stepKeyBox.Visible = true;
                _stepKeyHint.Visible = true;
                break;

            case StepKind.Click:
                ShowButtonRow(Row1);
                _stepDoubleClick.SetBounds(284, Row1 + 3, 78, 24);
                _stepDoubleClick.Visible = true;

                _stepTargetLabel.SetBounds(20, Row2 + 6, 76, 20);
                _stepTargetSelector.SetBounds(100, Row2, 210, 30);
                _stepTargetLabel.Visible = true;
                _stepTargetSelector.Visible = true;

                ShowPointRow(Row3, _stepPickButton, _stepXInput, _stepYInput, _stepPointLabel);

                _stepScatterLabel.SetBounds(20, Row4 + 6, 76, 20);
                _stepScatter.SetBounds(100, Row4, 84, 32);
                _stepScatterHint.SetBounds(192, Row4 + 6, 170, 20);
                _stepScatterLabel.Visible = true;
                _stepScatter.Visible = true;
                _stepScatterHint.Visible = true;
                break;

            case StepKind.Drag:
                ShowButtonRow(Row1);
                ShowPointRow(Row2, _stepPickButton, _stepXInput, _stepYInput, _stepPointLabel);
                ShowPointRow(Row3, _stepPickToButton, _stepToXInput, _stepToYInput, _stepToLabel);

                _stepDragDurationLabel.SetBounds(20, Row4 + 6, 76, 20);
                _stepDragDuration.SetBounds(100, Row4, 84, 32);
                _stepDragDurationHint.SetBounds(192, Row4 + 6, 220, 20);
                _stepDragDurationLabel.Visible = true;
                _stepDragDuration.Visible = true;
                _stepDragDurationHint.Visible = true;
                break;

            case StepKind.TypeText:
                _stepTextLabel.SetBounds(20, Row1 + 6, 88, 20);
                _stepTextField.SetBounds(100, Row1, 304, 32);
                _stepTextLabel.Text = Loc.TextWord;
                _stepTextLabel.Visible = true;
                _stepTextField.Visible = true;
                break;

            case StepKind.Wait:
                _stepDelayEditor.SetBounds(20, Row1, 320, 32);
                _stepDelayEditor.Visible = true;
                break;

            case StepKind.WaitForPixelColor:
                ShowPointRow(Row1, _stepPickButton, _stepXInput, _stepYInput, _stepPointLabel);

                _stepColorLabel.SetBounds(20, Row2 + 6, 76, 20);
                _stepColorSwatch.SetBounds(100, Row2, 32, 32);
                _stepCaptureColorButton.SetBounds(140, Row2, 90, 32);
                _stepColorLabel.Visible = true;
                _stepColorSwatch.Visible = true;
                _stepCaptureColorButton.Visible = true;

                _stepToleranceLabel.SetBounds(20, Row3 + 6, 76, 20);
                _stepTolerance.SetBounds(100, Row3, 84, 32);
                _stepToleranceHint.SetBounds(192, Row3 + 6, 220, 20);
                _stepToleranceLabel.Visible = true;
                _stepTolerance.Visible = true;
                _stepToleranceHint.Visible = true;

                _stepTimeoutLabel.SetBounds(20, Row4 + 6, 76, 20);
                _stepTimeout.SetBounds(100, Row4, 84, 32);
                _stepTimeoutHint.SetBounds(192, Row4 + 6, 220, 20);
                _stepTimeoutLabel.Visible = true;
                _stepTimeout.Visible = true;
                _stepTimeoutHint.Visible = true;
                break;

            case StepKind.ClipboardSet:
                _stepTextLabel.SetBounds(20, Row1 + 6, 88, 20);
                _stepTextField.SetBounds(100, Row1, 304, 32);
                _stepTextLabel.Text = Loc.ClipboardTextLabel;
                _stepTextLabel.Visible = true;
                _stepTextField.Visible = true;
                break;

            case StepKind.ClipboardPaste:
                _stepClipboardHint.SetBounds(20, Row1 + 6, 380, 32);
                _stepClipboardHint.Visible = true;
                break;
        }
    }

    private void UpdateControlStates()
    {
        bool running = IsRunning;
        bool busy = IsBusy;

        _sidebar.Enabled = !running;
        _nameBox.Enabled = !running;

        foreach (var card in _settingCards)
        {
            card.Enabled = !running;
        }

        _addStepButton.Enabled = !busy && _current.Steps.Count < Limits.MaxSteps;
        _recordButton.Enabled = !running;
        ApplySelectedStep(_stepList.SelectedIndex);

        bool byCount = _current.Repeat == RepeatMode.Count;
        bool byDuration = _current.Repeat == RepeatMode.Duration;
        _repetitions.Visible = byCount;
        _duration.Visible = byDuration;
        _repeatLabel.Visible = byCount || byDuration;
        _repeatLabel.Text = byCount ? Loc.IterationsLabel : Loc.MinutesLabel;

        _repeatHint.Text = _current.Repeat switch
        {
            RepeatMode.Count => Loc.RepeatHintCount,
            RepeatMode.Duration => Loc.RepeatHintDuration,
            _ => Loc.RepeatHintForever,
        };

        _windowTitleField.Enabled = _restrictWindowCheck.Checked;
        _useCurrentWindowButton.Enabled = _restrictWindowCheck.Checked && !busy && !_captureWindowTimer.Enabled;

        _runPanel.SetStartEnabled(!busy);
        _runPanel.SetTestEnabled(!busy);
    }
}
