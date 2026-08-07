namespace ClickerApp;

internal sealed class MainForm : Form
{
    private const string StartHotkeyName = "start";
    private const string StopHotkeyName = "stop";
    private const string CaptureHotkeyName = "capture";
    private static readonly Keys CaptureHotkey = Keys.F9;

    // Profiles
    private readonly Panel _sidebar = new();
    private readonly ProfileListBox _profileList = new();
    private readonly FlatButton _newButton = new();
    private readonly FlatButton _duplicateButton = new();
    private readonly FlatButton _deleteButton = new();
    private readonly TextBox _nameBox = new();
    private readonly Label _headerHint = UiFactory.Hint(string.Empty, 0, 0, 0);

    // Settings
    private readonly KeyCaptureBox _keyBox = new();
    private readonly NumericUpDown _xInput = UiFactory.Numeric(0, 0, 80, -32000, 32000, 0);
    private readonly NumericUpDown _yInput = UiFactory.Numeric(0, 0, 80, -32000, 32000, 0);
    private readonly FlatButton _captureButton = new();
    private readonly DelayEditor _keyDelay = new();
    private readonly DelayEditor _clickDelay = new();
    private readonly NumericUpDown _repetitions = UiFactory.Numeric(0, 0, 90, 1, 1000000, 10);
    private readonly CheckBox _infinite = UiFactory.Check("Infinite", 0, 0, 90);
    private readonly KeyCaptureBox _startHotkeyBox = new();
    private readonly KeyCaptureBox _stopHotkeyBox = new();

    // Run controls
    private readonly FlatButton _startButton = new();
    private readonly FlatButton _stopButton = new();
    private readonly Label _statusLabel = UiFactory.Label(string.Empty, 0, 0, 0);
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
        ClientSize = new Size(900, 648);
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
        _sidebar.BackColor = Theme.Surface;
        _sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
        };

        _sidebar.Controls.Add(UiFactory.Label("PROFILES", 20, 22, 140, Theme.Caption, Theme.TextSecondary));

        _profileList.SetBounds(10, 50, 230, 470);
        _profileList.DisplayMember = nameof(Profile.Name);
        _profileList.SelectedIndexChanged += ProfileList_SelectedIndexChanged;
        _sidebar.Controls.Add(_profileList);

        ConfigureButton(_newButton, "New profile", ButtonKind.Secondary, 12, 530, 226, 36);
        _newButton.Click += (_, _) => CreateProfile();

        ConfigureButton(_duplicateButton, "Duplicate", ButtonKind.Secondary, 12, 574, 108, 36);
        _duplicateButton.Click += (_, _) => DuplicateProfile();

        ConfigureButton(_deleteButton, "Delete", ButtonKind.Danger, 130, 574, 108, 36);
        _deleteButton.Click += (_, _) => DeleteProfile();

        _sidebar.Controls.AddRange(new Control[] { _newButton, _duplicateButton, _deleteButton });
        Controls.Add(_sidebar);
    }

    private void BuildHeader()
    {
        _nameBox.SetBounds(272, 18, 400, 32);
        _nameBox.BorderStyle = BorderStyle.None;
        _nameBox.BackColor = Theme.Background;
        _nameBox.ForeColor = Theme.TextPrimary;
        _nameBox.Font = Theme.Title;
        _nameBox.TextChanged += NameBox_TextChanged;
        _nameBox.Leave += NameBox_Leave;
        Controls.Add(_nameBox);

        _headerHint.SetBounds(676, 24, 200, 20);
        _headerHint.TextAlign = ContentAlignment.MiddleRight;
        _headerHint.Text = "Edit the name to rename";
        Controls.Add(_headerHint);
    }

    private void BuildActionCard()
    {
        var card = AddCard("ACTION", 62, 152);

        card.Controls.Add(UiFactory.Label("Key", 20, 52, 90));
        _keyBox.SetBounds(116, 46, 170, 32);
        _keyBox.Placeholder = "Click, then press a key";
        _keyBox.KeyValueChanged += OnSettingChanged;
        card.Controls.Add(_keyBox);
        card.Controls.Add(UiFactory.Hint("Any key works — Tab, Enter, Space, \\ … Esc clears it", 296, 52, 290));

        card.Controls.Add(UiFactory.Label("Position", 20, 100, 90));
        _xInput.SetBounds(116, 98, 80, 24);
        _yInput.SetBounds(204, 98, 80, 24);
        _xInput.ValueChanged += OnSettingChanged;
        _yInput.ValueChanged += OnSettingChanged;
        card.Controls.Add(_xInput);
        card.Controls.Add(_yInput);

        ConfigureButton(_captureButton, "Capture", ButtonKind.Secondary, 292, 95, 110, 30);
        _captureButton.Font = Theme.Base;
        _captureButton.Click += (_, _) => CaptureCursorPosition();
        card.Controls.Add(_captureButton);
        card.Controls.Add(UiFactory.Hint("or press F9 anywhere", 412, 100, 170));
    }

    private void BuildDelayCard()
    {
        var card = AddCard("DELAYS", 230, 152);

        card.Controls.Add(UiFactory.Label("After key press", 20, 52, 110));
        _keyDelay.Location = new Point(136, 48);
        _keyDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_keyDelay);

        card.Controls.Add(UiFactory.Label("Between clicks", 20, 100, 110));
        _clickDelay.Location = new Point(136, 96);
        _clickDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_clickDelay);
    }

    private void BuildRepeatCard()
    {
        var card = AddCard("REPEAT & HOTKEYS", 398, 152);

        card.Controls.Add(UiFactory.Label("Repetitions", 20, 52, 110));
        _repetitions.SetBounds(136, 50, 90, 24);
        _repetitions.ValueChanged += OnSettingChanged;
        card.Controls.Add(_repetitions);

        _infinite.SetBounds(238, 51, 90, 22);
        _infinite.CheckedChanged += OnSettingChanged;
        card.Controls.Add(_infinite);
        card.Controls.Add(UiFactory.Hint("runs until you stop it", 336, 52, 200));

        card.Controls.Add(UiFactory.Label("Start hotkey", 20, 100, 110));
        _startHotkeyBox.SetBounds(136, 94, 130, 32);
        _startHotkeyBox.Placeholder = "Not set";
        _startHotkeyBox.KeyValueChanged += StartHotkey_Changed;
        card.Controls.Add(_startHotkeyBox);

        card.Controls.Add(UiFactory.Label("Stop hotkey", 286, 100, 90));
        _stopHotkeyBox.SetBounds(382, 94, 130, 32);
        _stopHotkeyBox.Placeholder = "Not set";
        _stopHotkeyBox.KeyValueChanged += StopHotkey_Changed;
        card.Controls.Add(_stopHotkeyBox);
    }

    private void BuildFooter()
    {
        ConfigureButton(_startButton, "Start", ButtonKind.Primary, 274, 566, 290, 44);
        _startButton.Click += (_, _) => StartAutomation();

        ConfigureButton(_stopButton, "Stop", ButtonKind.Secondary, 586, 566, 290, 44);
        _stopButton.Enabled = false;
        _stopButton.Click += (_, _) => StopAutomation();

        _statusLabel.SetBounds(274, 620, 602, 20);
        _statusLabel.ForeColor = Theme.TextSecondary;

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
        _xInput.Value = Math.Clamp(_current.ClickX, _xInput.Minimum, _xInput.Maximum);
        _yInput.Value = Math.Clamp(_current.ClickY, _yInput.Minimum, _yInput.Maximum);
        _keyDelay.Value = _current.KeyDelay;
        _clickDelay.Value = _current.ClickDelay;
        _repetitions.Value = Math.Clamp(_current.Repetitions, _repetitions.Minimum, _repetitions.Maximum);
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
            SetStatus("At least one profile is required.", Theme.Danger);
            return;
        }

        var result = MessageBox.Show(this, $"Delete \"{_current.Name}\"?", "Delete profile",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
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
        _current.ClickX = (int)_xInput.Value;
        _current.ClickY = (int)_yInput.Value;
        _current.KeyDelay = _keyDelay.Value;
        _current.ClickDelay = _clickDelay.Value;
        _current.Repetitions = (int)_repetitions.Value;
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
            SetStatus("Start and Stop need different keys.", Theme.Danger);
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
            SetStatus("Start and Stop need different keys.", Theme.Danger);
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
            SetStatus("Could not save profiles: " + ex.Message, Theme.Danger);
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
            SetStatus("Already used by another app: " + string.Join(", ", unavailable), Theme.Danger);
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
        _xInput.Value = Math.Clamp(position.X, _xInput.Minimum, _xInput.Maximum);
        _yInput.Value = Math.Clamp(position.Y, _yInput.Minimum, _yInput.Maximum);
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
            SetStatus("Pick a key first: click the key field, then press any key.", Theme.Danger);
            return;
        }

        // A synthesized key still triggers registered hotkeys, so this pair would stop itself.
        if (_current.Key == _current.StopHotkey)
        {
            SetStatus($"{KeyNames.Describe(_current.Key)} is also the Stop hotkey — choose another key.", Theme.Danger);
            return;
        }

        var settings = AutomationSettings.FromProfile(_current);
        _cancellation = new CancellationTokenSource();
        SaveNow();
        UpdateControlStates();

        string total = settings.Repetitions?.ToString() ?? "∞";
        var progress = new Progress<int>(iteration =>
            SetStatus($"Running \"{_current.Name}\" — iteration {iteration} of {total}", Theme.Success));

        SetStatus($"Running \"{_current.Name}\" — 0 of {total}", Theme.Success);

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
            SetStatus("Error: " + ex.Message, Theme.Danger);
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

    private void SetStatus(string text, Color? color = null)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color ?? Theme.TextSecondary;
    }
}
