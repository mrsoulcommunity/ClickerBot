namespace ClickerApp;

internal sealed class MainForm : Form
{
    private const string StartHotkeyName = "start";
    private const string StopHotkeyName = "stop";
    private const string CaptureHotkeyName = "capture";
    private static readonly Keys CaptureHotkey = Keys.F9;

    // Profiles
    private readonly SurfacePanel _sidebar = new();
    private readonly ProfileListBox _profileList = new();
    private readonly FlatButton _newButton = new();
    private readonly FlatButton _duplicateButton = new();
    private readonly FlatButton _deleteButton = new();
    private readonly ThemedTextBox _nameBox = new() { Font = Theme.Title };
    private readonly ThemedLabel _headerHint = UiFactory.Hint(string.Empty, 0, 0, 0);
    private readonly ThemeToggle _themeToggle = new();

    // Settings
    private readonly KeyCaptureBox _keyBox = new();
    private readonly NumberBox _xInput = UiFactory.Numeric(0, 0, 96, -32000, 32000, 0);
    private readonly NumberBox _yInput = UiFactory.Numeric(0, 0, 96, -32000, 32000, 0);
    private readonly FlatButton _captureButton = new();
    private readonly DelayEditor _keyDelay = new();
    private readonly DelayEditor _clickDelay = new();
    private readonly NumberBox _repetitions = UiFactory.Numeric(0, 0, 108, 1, 1000000, 10);
    private readonly ThemedCheckBox _infinite = UiFactory.Check("Infinite", 0, 0, 92);
    private readonly KeyCaptureBox _startHotkeyBox = new();
    private readonly KeyCaptureBox _stopHotkeyBox = new();

    // Run controls
    private readonly FlatButton _startButton = new();
    private readonly FlatButton _stopButton = new();
    private readonly ThemedLabel _statusLabel = UiFactory.Label(string.Empty, 0, 0, 0);
    private readonly List<Card> _cards = new();

    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 400 };

    private ProfileStore _store = new();
    private Profile _current = new();
    private HotkeyManager? _hotkeys;
    private CancellationTokenSource? _cancellation;
    private bool _loading;
    private bool _suspendSelection;

    public MainForm()
    {
        BuildUi();
        _saveTimer.Tick += (_, _) => SaveNow();
    }

    private bool IsRunning => _cancellation is not null;

    // --- Lifecycle ------------------------------------------------------

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        _hotkeys = new HotkeyManager(this);
        _hotkeys.HotkeyPressed += OnHotkeyPressed;

        _store = ProfileStore.Load();

        // Apply before the first paint, then let ThemeManager keep the tree in sync.
        Theme.Apply(_store.Appearance);
        ThemeManager.Attach(this);

        ReloadProfileList(_store.SelectedIndex);
        SetStatus("Ready.");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopAutomation();
        SaveNow();
        _hotkeys?.Dispose();
        base.OnFormClosing(e);
    }

    // --- UI -------------------------------------------------------------

    private void BuildUi()
    {
        Text = "Auto Key & Click";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 664);
        BackColor = Theme.Background;
        Font = Theme.Base;

        BuildSidebar();
        BuildHeader();
        BuildActionCard();
        BuildDelayCard();
        BuildRepeatCard();
        BuildFooter();
    }

    private void BuildSidebar()
    {
        _sidebar.SetBounds(0, 0, 250, ClientSize.Height);
        _sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
        };

        _sidebar.Controls.Add(UiFactory.Caption("PROFILES", 20, 22, 140));

        _profileList.SetBounds(10, 50, 230, 486);
        _profileList.DisplayMember = nameof(Profile.Name);
        _profileList.SelectedIndexChanged += ProfileList_SelectedIndexChanged;
        _sidebar.Controls.Add(_profileList);

        ConfigureButton(_newButton, "New profile", ButtonKind.Secondary, 12, 546, 226, 38);
        _newButton.Click += (_, _) => CreateProfile();

        ConfigureButton(_duplicateButton, "Duplicate", ButtonKind.Secondary, 12, 592, 108, 38);
        _duplicateButton.Click += (_, _) => DuplicateProfile();

        ConfigureButton(_deleteButton, "Delete", ButtonKind.Danger, 130, 592, 108, 38);
        _deleteButton.Click += (_, _) => DeleteProfile();

        _sidebar.Controls.AddRange(new Control[] { _newButton, _duplicateButton, _deleteButton });
        Controls.Add(_sidebar);
    }

    private void BuildHeader()
    {
        _nameBox.SetBounds(272, 20, 340, 32);
        _nameBox.TextChanged += NameBox_TextChanged;
        _nameBox.Leave += NameBox_Leave;
        Controls.Add(_nameBox);

        _headerHint.SetBounds(618, 26, 190, 20);
        _headerHint.TextAlign = ContentAlignment.MiddleRight;
        _headerHint.Text = "Edit the name to rename";
        Controls.Add(_headerHint);

        _themeToggle.Location = new Point(818, 21);
        _themeToggle.ModeRequested += SetTheme;
        Controls.Add(_themeToggle);
    }

    private void BuildActionCard()
    {
        var card = AddCard("ACTION", 66, 156);

        card.Controls.Add(UiFactory.Label("Key", 20, 52, 90));
        _keyBox.SetBounds(116, 46, 170, 32);
        _keyBox.Placeholder = "Click, then press a key";
        _keyBox.KeyValueChanged += OnSettingChanged;
        card.Controls.Add(_keyBox);
        card.Controls.Add(UiFactory.Hint("Any key works — Tab, Enter, Space, \\ … Esc clears it", 296, 52, 290));

        card.Controls.Add(UiFactory.Label("Position", 20, 102, 90));
        _xInput.Location = new Point(116, 96);
        _yInput.Location = new Point(220, 96);
        _xInput.ValueChanged += OnSettingChanged;
        _yInput.ValueChanged += OnSettingChanged;
        card.Controls.Add(_xInput);
        card.Controls.Add(_yInput);

        ConfigureButton(_captureButton, "Capture", ButtonKind.Secondary, 328, 97, 108, 30);
        _captureButton.Font = Theme.Base;
        _captureButton.Click += (_, _) => CaptureCursorPosition();
        card.Controls.Add(_captureButton);
        card.Controls.Add(UiFactory.Hint("or press F9 anywhere", 446, 102, 150));
    }

    private void BuildDelayCard()
    {
        var card = AddCard("DELAYS", 238, 156);

        card.Controls.Add(UiFactory.Label("After key press", 20, 52, 110));
        _keyDelay.Location = new Point(136, 46);
        _keyDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_keyDelay);

        card.Controls.Add(UiFactory.Label("Between clicks", 20, 102, 110));
        _clickDelay.Location = new Point(136, 96);
        _clickDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_clickDelay);
    }

    private void BuildRepeatCard()
    {
        var card = AddCard("REPEAT & HOTKEYS", 410, 156);

        card.Controls.Add(UiFactory.Label("Repetitions", 20, 52, 110));
        _repetitions.Location = new Point(136, 46);
        _repetitions.ValueChanged += OnSettingChanged;
        card.Controls.Add(_repetitions);

        _infinite.SetBounds(256, 50, 92, 24);
        _infinite.CheckedChanged += OnSettingChanged;
        card.Controls.Add(_infinite);
        card.Controls.Add(UiFactory.Hint("runs until you stop it", 356, 52, 200));

        card.Controls.Add(UiFactory.Label("Start hotkey", 20, 102, 110));
        _startHotkeyBox.SetBounds(136, 96, 130, 32);
        _startHotkeyBox.Placeholder = "Not set";
        _startHotkeyBox.KeyValueChanged += StartHotkey_Changed;
        card.Controls.Add(_startHotkeyBox);

        card.Controls.Add(UiFactory.Label("Stop hotkey", 286, 102, 90));
        _stopHotkeyBox.SetBounds(382, 96, 130, 32);
        _stopHotkeyBox.Placeholder = "Not set";
        _stopHotkeyBox.KeyValueChanged += StopHotkey_Changed;
        card.Controls.Add(_stopHotkeyBox);
    }

    private void BuildFooter()
    {
        ConfigureButton(_startButton, "Start", ButtonKind.Primary, 274, 582, 290, 44);
        _startButton.Click += (_, _) => StartAutomation();

        ConfigureButton(_stopButton, "Stop", ButtonKind.Secondary, 586, 582, 290, 44);
        _stopButton.Enabled = false;
        _stopButton.Click += (_, _) => StopAutomation();

        _statusLabel.SetBounds(274, 636, 602, 20);
        _statusLabel.Role = TextRole.Secondary;

        Controls.AddRange(new Control[] { _startButton, _stopButton, _statusLabel });
    }

    private Card AddCard(string title, int top, int height)
    {
        var card = new Card { Title = title };
        card.SetBounds(274, top, 602, height);
        Controls.Add(card);
        _cards.Add(card);
        return card;
    }

    private static void ConfigureButton(FlatButton button, string text, ButtonKind kind, int x, int y, int width, int height)
    {
        button.Text = text;
        button.Kind = kind;
        button.SetBounds(x, y, width, height);
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
        _keyBox.Key = _current.Key;
        _xInput.Value = _current.ClickX;
        _yInput.Value = _current.ClickY;
        _keyDelay.Value = _current.KeyDelay;
        _clickDelay.Value = _current.ClickDelay;
        _repetitions.Value = _current.Repetitions;
        _infinite.Checked = _current.InfiniteLoop;
        _startHotkeyBox.Key = _current.StartHotkey;
        _stopHotkeyBox.Key = _current.StopHotkey;
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
        SetStatus($"Created \"{profile.Name}\".");
    }

    private void DuplicateProfile()
    {
        var copy = _current.Clone();
        copy.Name = _store.CreateUniqueName(_current.Name + " copy");
        _store.Profiles.Add(copy);
        ReloadProfileList(_store.Profiles.Count - 1);
        SetStatus($"Duplicated to \"{copy.Name}\".");
    }

    private void DeleteProfile()
    {
        if (_store.Profiles.Count == 1)
        {
            SetStatus("At least one profile is required.", TextRole.Danger);
            return;
        }

        bool confirmed = ConfirmDialog.Ask(this, "Delete profile",
            $"\"{_current.Name}\" will be removed. This cannot be undone.", "Delete", destructive: true);
        if (!confirmed)
        {
            return;
        }

        string name = _current.Name;
        int index = _store.Profiles.IndexOf(_current);
        _store.Profiles.RemoveAt(index);
        ReloadProfileList(Math.Max(0, index - 1));
        SetStatus($"Deleted \"{name}\".");
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

        _current.Key = _keyBox.Key;
        _current.ClickX = _xInput.Value;
        _current.ClickY = _yInput.Value;
        _current.KeyDelay = _keyDelay.Value;
        _current.ClickDelay = _clickDelay.Value;
        _current.Repetitions = _repetitions.Value;
        _current.InfiniteLoop = _infinite.Checked;

        UpdateControlStates();
        ScheduleSave();
    }

    private void StartHotkey_Changed(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (_startHotkeyBox.Key != Keys.None && _startHotkeyBox.Key == _stopHotkeyBox.Key)
        {
            SetStatus("Start and Stop need different keys.", TextRole.Danger);
            _startHotkeyBox.Key = _current.StartHotkey;
            return;
        }

        _current.StartHotkey = _startHotkeyBox.Key;
        RegisterHotkeys();
        ScheduleSave();
    }

    private void StopHotkey_Changed(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (_stopHotkeyBox.Key != Keys.None && _stopHotkeyBox.Key == _startHotkeyBox.Key)
        {
            SetStatus("Start and Stop need different keys.", TextRole.Danger);
            _stopHotkeyBox.Key = _current.StopHotkey;
            return;
        }

        _current.StopHotkey = _stopHotkeyBox.Key;
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
            SetStatus("Could not save profiles: " + ex.Message, TextRole.Danger);
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

        if (!_hotkeys.Assign(StartHotkeyName, _current.StartHotkey))
        {
            unavailable.Add(KeyNames.Describe(_current.StartHotkey) + " (start)");
        }

        if (!_hotkeys.Assign(StopHotkeyName, _current.StopHotkey))
        {
            unavailable.Add(KeyNames.Describe(_current.StopHotkey) + " (stop)");
        }

        bool captureTaken = _current.StartHotkey == CaptureHotkey || _current.StopHotkey == CaptureHotkey;
        if (captureTaken)
        {
            _hotkeys.Release(CaptureHotkeyName);
        }
        else if (!_hotkeys.Assign(CaptureHotkeyName, CaptureHotkey))
        {
            unavailable.Add("F9 (capture position)");
        }

        if (unavailable.Count > 0)
        {
            SetStatus("Already used by another app: " + string.Join(", ", unavailable), TextRole.Danger);
        }
    }

    private void OnHotkeyPressed(string name)
    {
        switch (name)
        {
            case StartHotkeyName when !IsRunning:
                StartAutomation();
                break;
            case StopHotkeyName:
                StopAutomation();
                break;
            case CaptureHotkeyName when !IsRunning:
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
        SetStatus($"Click position saved: {position.X}, {position.Y}");
    }

    private async void StartAutomation()
    {
        if (IsRunning)
        {
            return;
        }

        if (_current.Key == Keys.None)
        {
            SetStatus("Pick a key first: click the key field, then press any key.", TextRole.Danger);
            return;
        }

        // A synthesized key still triggers registered hotkeys, so this pair would stop itself.
        if (_current.Key == _current.StopHotkey)
        {
            SetStatus($"{KeyNames.Describe(_current.Key)} is also the Stop hotkey — choose another key.", TextRole.Danger);
            return;
        }

        var settings = AutomationSettings.FromProfile(_current);
        _cancellation = new CancellationTokenSource();
        SaveNow();
        UpdateControlStates();

        string total = settings.Repetitions?.ToString() ?? "∞";
        var progress = new Progress<int>(iteration =>
            SetStatus($"Running \"{_current.Name}\" — iteration {iteration} of {total}", TextRole.Success));

        SetStatus($"Running \"{_current.Name}\" — 0 of {total}", TextRole.Success);

        try
        {
            await AutomationRunner.RunAsync(settings, progress, _cancellation.Token);
            SetStatus("Finished.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Stopped.");
        }
        catch (Exception ex)
        {
            SetStatus("Error: " + ex.Message, TextRole.Danger);
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            UpdateControlStates();
        }
    }

    private void StopAutomation() => _cancellation?.Cancel();

    // --- State -----------------------------------------------------------

    private void UpdateControlStates()
    {
        bool running = IsRunning;

        _startButton.Enabled = !running;
        _stopButton.Enabled = running;
        _stopButton.Kind = running ? ButtonKind.Danger : ButtonKind.Secondary;
        _sidebar.Enabled = !running;
        _nameBox.Enabled = !running;

        foreach (var card in _cards)
        {
            card.Enabled = !running;
        }

        _repetitions.Enabled = !running && !_infinite.Checked;
    }

    private void SetStatus(string text, TextRole role = TextRole.Secondary)
    {
        _statusLabel.Text = text;

        // Stored as a role rather than a color so the status survives a theme switch.
        _statusLabel.Role = role;
    }
}
