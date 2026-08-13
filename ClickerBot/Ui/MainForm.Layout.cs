namespace ClickerBot;

/// <summary>
/// Where every control is put. Kept apart from <see cref="MainForm"/>'s wiring so that
/// changing the composition never means reading past the behaviour, and vice versa.
///
/// The window is a fixed size on a 4px rhythm: a profile rail on the left, a two-column grid
/// of setting cards, and a run bar pinned across the bottom. Everything that moves while a
/// run is in flight lives in that bar, so the settings above it can hold still.
/// </summary>
internal sealed partial class MainForm
{
    private const int WindowWidth = 1060;
    private const int WindowHeight = 850;

    private const int RailWidth = 252;
    private const int ColumnLeft = 276;
    private const int ColumnRight = 666;
    private const int CardWidth = 374;
    private const int Gutter = 16;

    private void BuildUi()
    {
        Text = "ClickerBot";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppIcon.Idle;

        // Every bound in this file is a pixel at 100% scaling, and the fonts are in points, so
        // on a scaled display the text grows and the boxes around it do not. Naming the design
        // DPI is what turns that ratio into the scale factor — without it the auto-scale
        // factor is a flat 1 and the mode above does nothing at all.
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(WindowWidth, WindowHeight);
        BackColor = Theme.Background;
        Font = Theme.Base;

        BuildRail();
        BuildHeader();
        BuildActionCard();
        BuildTimingCard();
        BuildRepeatCard();
        BuildHotkeyCard();
        BuildWindowCard();
        BuildRemoteCard();
        BuildRunPanel();
    }

    private void BuildRail()
    {
        _sidebar.SetBounds(0, 0, RailWidth, WindowHeight);
        _sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
        };

        _sidebar.Controls.Add(UiFactory.Caption("PROFILES", 20, 24, 140));

        _profileList.SetBounds(12, 50, 228, 550);
        _profileList.DisplayMember = nameof(Profile.Name);
        _profileList.SelectedIndexChanged += ProfileList_SelectedIndexChanged;
        _sidebar.Controls.Add(_profileList);

        Configure(_newButton, "New profile", ButtonKind.Secondary, 12, 612, 228, 34);
        _newButton.Click += (_, _) => CreateProfile();

        Configure(_duplicateButton, "Duplicate", ButtonKind.Secondary, 12, 654, 110, 34);
        _duplicateButton.Click += (_, _) => DuplicateProfile();

        Configure(_deleteButton, "Delete", ButtonKind.Danger, 130, 654, 110, 34);
        _deleteButton.Click += (_, _) => DeleteProfile();

        Configure(_importButton, "Import", ButtonKind.Secondary, 12, 696, 110, 34);
        _importButton.Click += (_, _) => ImportProfiles();

        Configure(_exportButton, "Export", ButtonKind.Secondary, 130, 696, 110, 34);
        _exportButton.Click += (_, _) => ExportProfiles();

        Configure(_historyButton, "History", ButtonKind.Secondary, 12, 738, 228, 34);
        _historyButton.Click += (_, _) => RunHistoryDialog.Show(this, _history);

        _sidebar.Controls.AddRange(new Control[]
        {
            _newButton, _duplicateButton, _deleteButton, _importButton, _exportButton, _historyButton,
        });

        Controls.Add(_sidebar);
    }

    private void BuildHeader()
    {
        _nameBox.SetBounds(ColumnLeft, 22, 420, 30);
        _nameBox.TextChanged += NameBox_TextChanged;
        _nameBox.Leave += NameBox_Leave;
        Controls.Add(_nameBox);

        _renameHint.SetBounds(ColumnLeft + 424, 24, 274, 20);
        _renameHint.TextAlign = ContentAlignment.MiddleRight;
        _renameHint.Text = "Type in the name above to rename";
        Controls.Add(_renameHint);

        _themeToggle.Location = new Point(982, 21);
        _themeToggle.ModeRequested += SetTheme;
        Controls.Add(_themeToggle);
    }

    private void BuildActionCard()
    {
        var card = AddCard("ACTION", ColumnLeft, 72, 290);

        card.Controls.Add(UiFactory.Label("Mode", 20, 48, 76));
        // Short enough to survive four-way division of the row's width without ellipsizing;
        // order matches ActionMode's declaration order exactly, since the index is cast
        // straight to the enum in OnSettingChanged.
        _modeSelector.Items = new[] { "Both", "Key", "Click", "Text" };
        _modeSelector.SetBounds(100, 42, 254, 30);
        _modeSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_modeSelector);

        _keyLabel.SetBounds(20, 88, 76, 20);
        card.Controls.Add(_keyLabel);

        _keyBox.SetBounds(100, 82, 174, 32);
        _keyBox.Placeholder = "Click, then press a key";
        _keyBox.KeyValueChanged += OnSettingChanged;
        card.Controls.Add(_keyBox);

        _keyHint.SetBounds(282, 88, 72, 20);
        card.Controls.Add(_keyHint);

        // Occupies the same row as the key field — only one of the two is ever visible, since
        // a profile either presses a key or types text, never both. See UpdateControlStates.
        _typeText.SetBounds(100, 82, 254, 32);
        _typeText.ValueChanged += OnSettingChanged;
        card.Controls.Add(_typeText);

        card.Controls.Add(UiFactory.Label("Button", 20, 128, 76));
        _buttonSelector.Items = new[] { "Left", "Right", "Middle" };
        _buttonSelector.SetBounds(100, 122, 174, 30);
        _buttonSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_buttonSelector);

        _doubleClick.SetBounds(284, 125, 78, 24);
        _doubleClick.Text = "Double";
        _doubleClick.CheckedChanged += OnSettingChanged;
        card.Controls.Add(_doubleClick);

        card.Controls.Add(UiFactory.Label("Click at", 20, 168, 76));
        _targetSelector.Items = new[] { "A fixed point", "The cursor" };
        _targetSelector.SetBounds(100, 162, 210, 30);
        _targetSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_targetSelector);

        card.Controls.Add(UiFactory.Label("Point", 20, 208, 76));
        _xInput.SetBounds(100, 202, 84, 32);
        _yInput.SetBounds(190, 202, 84, 32);
        _xInput.ValueChanged += OnSettingChanged;
        _yInput.ValueChanged += OnSettingChanged;
        card.Controls.Add(_xInput);
        card.Controls.Add(_yInput);

        Configure(_captureButton, "Pick", ButtonKind.Secondary, 282, 203, 72, 30);
        _captureButton.Font = Theme.Base;
        _captureButton.Click += (_, _) => CaptureCursorPosition();
        card.Controls.Add(_captureButton);

        card.Controls.Add(UiFactory.Label("Scatter", 20, 248, 76));
        _scatter.SetBounds(100, 242, 84, 32);
        _scatter.ValueChanged += OnSettingChanged;
        card.Controls.Add(_scatter);
        card.Controls.Add(UiFactory.Hint("px of random spread per click", 192, 248, 170));
    }

    private void BuildTimingCard()
    {
        var card = AddCard("TIMING", ColumnRight, 72, 228);

        card.Controls.Add(UiFactory.Hint("After the key press, before the click", 20, 44, 320));
        _keyDelay.Location = new Point(20, 64);
        _keyDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_keyDelay);

        card.Controls.Add(UiFactory.Hint("Between one iteration and the next", 20, 112, 320));
        _clickDelay.Location = new Point(20, 132);
        _clickDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_clickDelay);

        card.Controls.Add(UiFactory.Label("Start delay", 20, 186, 88));
        _startDelay.SetBounds(114, 180, 84, 32);
        _startDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_startDelay);
        card.Controls.Add(UiFactory.Hint("seconds before the first action", 206, 186, 156));
    }

    private void BuildRepeatCard()
    {
        var card = AddCard("REPEAT", ColumnRight, 310, 170);

        _repeatSelector.Items = new[] { "Count", "Duration", "Until stopped" };
        _repeatSelector.SetBounds(20, 42, 334, 30);
        _repeatSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_repeatSelector);

        _repeatLabel.SetBounds(20, 88, 88, 20);
        card.Controls.Add(_repeatLabel);

        _repetitions.SetBounds(114, 82, 118, 32);
        _repetitions.ValueChanged += OnSettingChanged;
        card.Controls.Add(_repetitions);

        _duration.SetBounds(114, 82, 118, 32);
        _duration.ValueChanged += OnSettingChanged;
        card.Controls.Add(_duration);

        _repeatHint.SetBounds(20, 126, 334, 20);
        card.Controls.Add(_repeatHint);
    }

    private void BuildHotkeyCard()
    {
        var card = AddCard("HOTKEYS", ColumnLeft, 378, 216);

        AddHotkeyRow(card, "Start", _startHotkeyBox, 44);
        AddHotkeyRow(card, "Pause", _pauseHotkeyBox, 84);
        AddHotkeyRow(card, "Stop", _stopHotkeyBox, 124);
        AddHotkeyRow(card, "Pick point", _pickHotkeyBox, 164);
    }

    private void AddHotkeyRow(Card card, string label, KeyCaptureBox box, int top)
    {
        card.Controls.Add(UiFactory.Label(label, 20, top + 6, 88));
        box.SetBounds(114, top, 130, 32);
        box.Placeholder = "Not set";
        box.KeyValueChanged += HotkeyBox_Changed;
        card.Controls.Add(box);
    }

    private void BuildWindowCard()
    {
        var card = AddCard("WINDOW", ColumnRight, 496, 190);

        _alwaysOnTop.SetBounds(20, 40, 334, 24);
        _alwaysOnTop.Text = "Keep above other windows";
        _alwaysOnTop.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_alwaysOnTop);

        _hideToTray.SetBounds(20, 68, 334, 24);
        _hideToTray.Text = "Hide to the notification area while running";
        _hideToTray.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_hideToTray);

        _autoStart.SetBounds(20, 96, 334, 24);
        _autoStart.Text = "Start ClickerBot when Windows starts";
        _autoStart.CheckedChanged += (_, _) => ApplyAutoStart();
        card.Controls.Add(_autoStart);

        _failsafe.SetBounds(20, 124, 334, 24);
        _failsafe.Text = "Abort if the mouse touches a screen corner";
        _failsafe.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_failsafe);
    }

    /// <summary>
    /// Not built through <see cref="AddCard"/>: everything else in the left column locks while
    /// a run is active, but this card is exactly how you would stop or check on a run started
    /// from a phone, so it stays live on purpose. Sits in the gap Hotkeys leaves below it.
    /// </summary>
    private void BuildRemoteCard()
    {
        var card = new Card { Title = "REMOTE" };
        card.SetBounds(ColumnLeft, 610, CardWidth, 84);
        Controls.Add(card);
        _cards.Add(card);

        _remoteEnabled.SetBounds(20, 40, 334, 24);
        _remoteEnabled.Text = "Enable mobile control";
        _remoteEnabled.CheckedChanged += (_, _) => ApplyRemoteControl();
        card.Controls.Add(_remoteEnabled);

        _remoteStatus.SetBounds(20, 64, 334, 18);
        _remoteStatus.Click += (_, _) => CopyRemoteUrl();
        card.Controls.Add(_remoteStatus);
    }

    private void BuildRunPanel()
    {
        _runPanel.SetBounds(ColumnLeft, 710, (CardWidth * 2) + Gutter, 120);
        _runPanel.TestRequested += (_, _) => TestAction();
        _runPanel.StartRequested += (_, _) => StartAutomation();
        _runPanel.PauseRequested += (_, _) => TogglePause();
        _runPanel.StopRequested += (_, _) => StopAutomation();
        Controls.Add(_runPanel);
        _cards.Add(_runPanel);
    }

    private Card AddCard(string title, int x, int top, int height)
    {
        var card = new Card { Title = title };
        card.SetBounds(x, top, CardWidth, height);
        Controls.Add(card);
        _cards.Add(card);
        _settingCards.Add(card);
        return card;
    }

    private static void Configure(
        FlatButton button, string text, ButtonKind kind, int x, int y, int width, int height)
    {
        button.Text = text;
        button.Kind = kind;
        button.SetBounds(x, y, width, height);
    }
}
