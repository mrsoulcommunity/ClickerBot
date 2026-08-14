namespace ClickerBot;

/// <summary>
/// Where every control is put. Kept apart from <see cref="MainForm"/>'s wiring so that
/// changing the composition never means reading past the behaviour, and vice versa.
///
/// The window is a fixed size on a 4px rhythm: a profile rail on the left, a two-column grid
/// of setting cards, and a run bar pinned across the bottom. Everything that moves while a
/// run is in flight lives in that bar, so the settings above it can hold still.
///
/// No control here is given its real text: every field is built with an empty string and
/// filled in by <see cref="MainForm.ApplyLanguage"/>, which runs immediately after this and
/// again on every language switch. That keeps the words in exactly one place instead of
/// splitting them between the control that first shows them and the method that updates them
/// later — the same reason none of these bounds is duplicated in <see cref="Loc"/>.
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

        _sidebar.Controls.Add(_profilesCaption);

        _profileList.SetBounds(12, 50, 228, 550);
        _profileList.DisplayMember = nameof(Profile.Name);
        _profileList.SelectedIndexChanged += ProfileList_SelectedIndexChanged;
        _sidebar.Controls.Add(_profileList);

        Configure(_newButton, ButtonKind.Secondary, 12, 612, 228, 34);
        _newButton.Click += (_, _) => CreateProfile();

        Configure(_duplicateButton, ButtonKind.Secondary, 12, 654, 110, 34);
        _duplicateButton.Click += (_, _) => DuplicateProfile();

        Configure(_deleteButton, ButtonKind.Danger, 130, 654, 110, 34);
        _deleteButton.Click += (_, _) => DeleteProfile();

        Configure(_importButton, ButtonKind.Secondary, 12, 696, 110, 34);
        _importButton.Click += (_, _) => ImportProfiles();

        Configure(_exportButton, ButtonKind.Secondary, 130, 696, 110, 34);
        _exportButton.Click += (_, _) => ExportProfiles();

        Configure(_historyButton, ButtonKind.Secondary, 12, 738, 228, 34);
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

        // Right-aligned so it ends against the toggles it sits beside, rather than trailing off
        // into the gap between itself and the name box.
        _renameHint.SetBounds(ColumnLeft + 424, 24, 202, 20);
        _renameHint.TextAlign = ContentAlignment.MiddleRight;
        Controls.Add(_renameHint);

        // Not translated relative to each other, the way no language switch ever translates
        // its own labels — see Loc's class comment.
        _languageToggle.Items = new[] { "EN", "فا" };
        _languageToggle.Location = new Point(910, 21);
        _languageToggle.Size = new Size(64, 30);
        _languageToggle.SelectedIndexChanged += (_, _) => SetLanguage((Language)_languageToggle.SelectedIndex);
        Controls.Add(_languageToggle);

        _themeToggle.Location = new Point(982, 21);
        _themeToggle.ModeRequested += SetTheme;
        Controls.Add(_themeToggle);
    }

    private void BuildActionCard()
    {
        Card card = _actionCard;
        ConfigureCard(card, ColumnLeft, 72, 290);

        card.Controls.Add(_modeLabel);
        // Order matches ActionMode's declaration order exactly, since the index is cast
        // straight to the enum in OnSettingChanged — see Loc.ModeItems.
        _modeSelector.SetBounds(100, 42, 254, 30);
        _modeSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_modeSelector);

        _keyLabel.SetBounds(20, 88, 76, 20);
        card.Controls.Add(_keyLabel);

        _keyBox.SetBounds(100, 82, 174, 32);
        _keyBox.KeyValueChanged += OnSettingChanged;
        card.Controls.Add(_keyBox);

        _keyHint.SetBounds(282, 88, 72, 20);
        card.Controls.Add(_keyHint);

        // Occupies the same row as the key field — only one of the two is ever visible, since
        // a profile either presses a key or types text, never both. See UpdateControlStates.
        _typeText.SetBounds(100, 82, 254, 32);
        _typeText.ValueChanged += OnSettingChanged;
        card.Controls.Add(_typeText);

        card.Controls.Add(_buttonLabel);
        _buttonSelector.SetBounds(100, 122, 174, 30);
        _buttonSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_buttonSelector);

        _doubleClick.SetBounds(284, 125, 78, 24);
        _doubleClick.CheckedChanged += OnSettingChanged;
        card.Controls.Add(_doubleClick);

        card.Controls.Add(_clickAtLabel);
        _targetSelector.SetBounds(100, 162, 210, 30);
        _targetSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_targetSelector);

        card.Controls.Add(_pointLabel);
        _xInput.SetBounds(100, 202, 84, 32);
        _yInput.SetBounds(190, 202, 84, 32);
        _xInput.ValueChanged += OnSettingChanged;
        _yInput.ValueChanged += OnSettingChanged;
        card.Controls.Add(_xInput);
        card.Controls.Add(_yInput);

        Configure(_captureButton, ButtonKind.Secondary, 282, 203, 72, 30);
        _captureButton.Font = Theme.Base;
        _captureButton.Click += (_, _) => CaptureCursorPosition();
        card.Controls.Add(_captureButton);

        card.Controls.Add(_scatterLabel);
        _scatter.SetBounds(100, 242, 84, 32);
        _scatter.ValueChanged += OnSettingChanged;
        card.Controls.Add(_scatter);
        card.Controls.Add(_scatterHint);
    }

    private void BuildTimingCard()
    {
        Card card = _timingCard;
        ConfigureCard(card, ColumnRight, 72, 228);

        card.Controls.Add(_keyDelayHint);
        _keyDelay.Location = new Point(20, 64);
        _keyDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_keyDelay);

        card.Controls.Add(_clickDelayHint);
        _clickDelay.Location = new Point(20, 132);
        _clickDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_clickDelay);

        card.Controls.Add(_startDelayLabel);
        _startDelay.SetBounds(114, 180, 84, 32);
        _startDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_startDelay);
        card.Controls.Add(_startDelayHint);
    }

    private void BuildRepeatCard()
    {
        Card card = _repeatCard;
        ConfigureCard(card, ColumnRight, 310, 170);

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
        Card card = _hotkeyCard;
        ConfigureCard(card, ColumnLeft, 378, 216);

        AddHotkeyRow(card, _startHotkeyLabel, _startHotkeyBox, 44);
        AddHotkeyRow(card, _pauseHotkeyLabel, _pauseHotkeyBox, 84);
        AddHotkeyRow(card, _stopHotkeyLabel, _stopHotkeyBox, 124);
        AddHotkeyRow(card, _pickHotkeyLabel, _pickHotkeyBox, 164);
    }

    private void AddHotkeyRow(Card card, ThemedLabel label, KeyCaptureBox box, int top)
    {
        card.Controls.Add(label);
        box.SetBounds(114, top, 130, 32);
        box.KeyValueChanged += HotkeyBox_Changed;
        card.Controls.Add(box);
    }

    private void BuildWindowCard()
    {
        Card card = _windowCard;
        ConfigureCard(card, ColumnRight, 496, 190);

        _alwaysOnTop.SetBounds(20, 40, 334, 24);
        _alwaysOnTop.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_alwaysOnTop);

        _hideToTray.SetBounds(20, 68, 334, 24);
        _hideToTray.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_hideToTray);

        _autoStart.SetBounds(20, 96, 334, 24);
        _autoStart.CheckedChanged += (_, _) => ApplyAutoStart();
        card.Controls.Add(_autoStart);

        _failsafe.SetBounds(20, 124, 334, 24);
        _failsafe.CheckedChanged += (_, _) => ApplyWindowOptions();
        card.Controls.Add(_failsafe);
    }

    /// <summary>
    /// Not built through <see cref="ConfigureCard"/>: everything else in the left column locks
    /// while a run is active, but this card is exactly how you would stop or check on a run
    /// started from a phone, so it stays live on purpose. Sits in the gap Hotkeys leaves below it.
    /// </summary>
    private void BuildRemoteCard()
    {
        _remoteCard.SetBounds(ColumnLeft, 610, CardWidth, 84);
        Controls.Add(_remoteCard);
        _cards.Add(_remoteCard);

        _remoteEnabled.SetBounds(20, 40, 334, 24);
        _remoteEnabled.CheckedChanged += (_, _) => ApplyRemoteControl();
        _remoteCard.Controls.Add(_remoteEnabled);

        _remoteStatus.SetBounds(20, 64, 334, 18);
        _remoteStatus.Click += (_, _) => CopyRemoteUrl();
        _remoteCard.Controls.Add(_remoteStatus);
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

    private void ConfigureCard(Card card, int x, int top, int height)
    {
        card.SetBounds(x, top, CardWidth, height);
        Controls.Add(card);
        _cards.Add(card);
        _settingCards.Add(card);
    }

    private static void Configure(FlatButton button, ButtonKind kind, int x, int y, int width, int height)
    {
        button.Kind = kind;
        button.SetBounds(x, y, width, height);
    }
}
