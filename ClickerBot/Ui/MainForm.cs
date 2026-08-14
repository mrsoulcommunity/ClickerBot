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

    // Action
    private readonly Card _actionCard = new();
    private readonly ThemedLabel _modeLabel = UiFactory.Label(string.Empty, 20, 48, 76);
    private readonly Segmented _modeSelector = new();
    private readonly ThemedLabel _keyLabel = UiFactory.Label(string.Empty, 0, 0, 76);
    private readonly KeyCaptureBox _keyBox = new();
    private readonly TextField _typeText = new() { MaxLength = Limits.MaxTypedTextLength };
    private readonly ThemedLabel _keyHint = UiFactory.Hint(string.Empty, 0, 0, 72);
    private readonly ThemedLabel _buttonLabel = UiFactory.Label(string.Empty, 20, 128, 76);
    private readonly Segmented _buttonSelector = new();
    private readonly ThemedCheckBox _doubleClick = UiFactory.Check(string.Empty, 0, 0, 78);
    private readonly ThemedLabel _clickAtLabel = UiFactory.Label(string.Empty, 20, 168, 76);
    private readonly Segmented _targetSelector = new();
    private readonly ThemedLabel _pointLabel = UiFactory.Label(string.Empty, 20, 208, 76);
    private readonly NumberBox _xInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly NumberBox _yInput =
        UiFactory.Numeric(0, 0, 84, Limits.MinCoordinate, Limits.MaxCoordinate, 0);
    private readonly FlatButton _captureButton = new();
    private readonly ThemedLabel _scatterLabel = UiFactory.Label(string.Empty, 20, 248, 76);
    private readonly NumberBox _scatter =
        UiFactory.Numeric(0, 0, 84, Limits.MinScatter, Limits.MaxScatter, 0);
    private readonly ThemedLabel _scatterHint = UiFactory.Hint(string.Empty, 192, 248, 170);

    // Timing
    private readonly Card _timingCard = new();
    private readonly ThemedLabel _keyDelayHint = UiFactory.Hint(string.Empty, 20, 44, 320);
    private readonly DelayEditor _keyDelay = new();
    private readonly ThemedLabel _clickDelayHint = UiFactory.Hint(string.Empty, 20, 112, 320);
    private readonly DelayEditor _clickDelay = new();
    private readonly ThemedLabel _startDelayLabel = UiFactory.Label(string.Empty, 20, 186, 88);
    private readonly NumberBox _startDelay = UiFactory.Numeric(
        0, 0, 84, Limits.MinStartDelaySeconds, Limits.MaxStartDelaySeconds, 3);
    private readonly ThemedLabel _startDelayHint = UiFactory.Hint(string.Empty, 206, 186, 156);

    // Repeat
    private readonly Card _repeatCard = new();
    private readonly Segmented _repeatSelector = new();
    private readonly ThemedLabel _repeatLabel = UiFactory.Label(string.Empty, 0, 0, 88);
    private readonly ThemedLabel _repeatHint = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly NumberBox _repetitions =
        UiFactory.Numeric(0, 0, 118, Limits.MinRepetitions, Limits.MaxRepetitions, 100);
    private readonly NumberBox _duration =
        UiFactory.Numeric(0, 0, 118, Limits.MinDurationMinutes, Limits.MaxDurationMinutes, 5);

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
        return new RemoteStatusPayload(
            running,
            _current.Name,
            ActionModeNames.Describe(_current.Mode),
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

        _actionCard.Title = Loc.ActionCardTitle;
        _modeLabel.Text = Loc.ModeLabel;
        _modeSelector.Items = Loc.ModeItems;
        _keyBox.Placeholder = Loc.KeyPlaceholder;
        _keyHint.Text = Loc.EscClears;
        _buttonLabel.Text = Loc.ButtonLabel;
        _buttonSelector.Items = Loc.MouseButtonItems;
        _doubleClick.Text = Loc.DoubleClick;
        _clickAtLabel.Text = Loc.ClickAtLabel;
        _targetSelector.Items = Loc.ClickTargetItems;
        _pointLabel.Text = Loc.PointLabel;
        _captureButton.Text = Loc.Pick;
        _scatterLabel.Text = Loc.ScatterLabel;
        _scatterHint.Text = Loc.ScatterHint;

        _timingCard.Title = Loc.TimingCardTitle;
        _keyDelayHint.Text = Loc.KeyDelayHint;
        _clickDelayHint.Text = Loc.ClickDelayHint;
        _startDelayLabel.Text = Loc.StartDelayLabel;
        _startDelayHint.Text = Loc.StartDelayHint;

        _repeatCard.Title = Loc.RepeatCardTitle;
        _repeatSelector.Items = Loc.RepeatItems;

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

        // State-dependent text (the Key/Text swap, the Iterations/Minutes swap, the remote
        // status line) is re-derived rather than duplicated here.
        UpdateControlStates();
        UpdateRemoteStatusLabel();
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
        _runPanel.SetIdleMessage(Loc.ClickPointSet(position.X, position.Y));
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
            return Loc.BlockerNoText;
        }

        if (_current.UsesKey && _current.Key == Keys.None)
        {
            return Loc.BlockerNoKey;
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
            (_current.StopHotkey, Loc.Stop),
            (_current.StartHotkey, Loc.Start),
            (_current.PauseHotkey, Loc.Pause),
            (_current.PickHotkey, Loc.PickPoint),
        };

        foreach (var (key, name) in conflicts)
        {
            if (key != Keys.None && key == _current.Key)
            {
                return Loc.KeyIsHotkey(KeyNames.Describe(_current.Key), name);
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
                Mode = settings.Mode,
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

        // The run's own click target can legitimately sit on a corner; only a position the
        // automation did not itself choose counts as reaching for the failsafe.
        bool isOwnTarget = _current.UsesMouse && _current.Target == ClickTarget.FixedPoint &&
            IsAtEdge(p.X, _current.ClickX) && IsAtEdge(p.Y, _current.ClickY);

        if (!isOwnTarget)
        {
            StopAutomation(Loc.OutcomeFailsafeStopped);
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
            _runPanel.SetPhase(gate.IsPaused ? RunPhase.Paused : RunPhase.Running);
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
        _keyLabel.Text = _current.UsesText ? Loc.TextWord : Loc.KeyWord;
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
        _repeatLabel.Text = byCount ? Loc.IterationsLabel : Loc.MinutesLabel;

        _repeatHint.Text = _current.Repeat switch
        {
            RepeatMode.Count => Loc.RepeatHintCount,
            RepeatMode.Duration => Loc.RepeatHintDuration,
            _ => Loc.RepeatHintForever,
        };

        _runPanel.SetStartEnabled(!running);
        _runPanel.SetTestEnabled(!running);
    }
}
