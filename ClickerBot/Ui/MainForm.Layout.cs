namespace ClickerBot;

/// <summary>
/// Where every control is put. Kept apart from <see cref="MainForm"/>'s wiring so that
/// changing the composition never means reading past the behaviour, and vice versa.
///
/// The window is a fixed size on a 4px rhythm: a profile rail on the left, a two-column grid
/// of setting cards, and a run bar pinned across the bottom. Everything that moves while a
/// run is in flight lives in that bar, so the settings above it can hold still.
///
/// The step detail card is the one exception to "each control has one place": <see
/// cref="MainForm.ShowStepFields"/> repositions a shared pool of row controls every time the
/// selected step's kind changes, since which fields a step needs depends on its kind and the
/// list of kinds is too long for each to own a fixed row of its own. The bounds set here for
/// those controls are just a safe starting size — ShowStepFields is what actually places them.
///
/// No control here is given its real text: every field is built with an empty string and
/// filled in by <see cref="MainForm.ApplyLanguage"/>, which runs immediately after this and
/// again on every language switch. That keeps the words in exactly one place instead of
/// splitting them between the control that first shows them and the method that updates them
/// later — the same reason none of these bounds is duplicated in <see cref="Loc"/>.
/// </summary>
internal sealed partial class MainForm
{
    private const int WindowWidth = 1200;
    private const int WindowHeight = 990;

    private const int RailWidth = 252;
    private const int ColumnLeft = 276;
    private const int ColumnRight = 736;
    private const int CardWidth = 444;
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
        BuildStepsCard();
        BuildStepDetailCard();
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
        _languageToggle.Location = new Point(1050, 21);
        _languageToggle.Size = new Size(64, 30);
        _languageToggle.SelectedIndexChanged += (_, _) => SetLanguage((Language)_languageToggle.SelectedIndex);
        Controls.Add(_languageToggle);

        _themeToggle.Location = new Point(1122, 21);
        _themeToggle.ModeRequested += SetTheme;
        Controls.Add(_themeToggle);
    }

    private void BuildStepsCard()
    {
        Card card = _stepsCard;
        ConfigureCard(card, ColumnLeft, 72, 430);

        _stepList.SetBounds(20, 40, 404, 340);
        _stepList.SelectedIndexChanged += StepList_SelectedIndexChanged;
        card.Controls.Add(_stepList);

        _noStepsLabel.SetBounds(20, 40, 404, 340);
        _noStepsLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(_noStepsLabel);

        const int ToolbarTop = 388;
        Configure(_moveUpButton, ButtonKind.Secondary, 20, ToolbarTop, 36, 32);
        _moveUpButton.Click += (_, _) => MoveStepUp();

        Configure(_moveDownButton, ButtonKind.Secondary, 60, ToolbarTop, 36, 32);
        _moveDownButton.Click += (_, _) => MoveStepDown();

        Configure(_deleteStepButton, ButtonKind.Danger, 104, ToolbarTop, 90, 32);
        _deleteStepButton.Click += (_, _) => DeleteStep();

        Configure(_recordButton, ButtonKind.Secondary, 226, ToolbarTop, 90, 32);
        _recordButton.Click += (_, _) => ToggleRecording();

        Configure(_addStepButton, ButtonKind.Primary, 324, ToolbarTop, 100, 32);
        _addStepButton.Click += (_, _) => AddStep();

        card.Controls.AddRange(new Control[]
        {
            _moveUpButton, _moveDownButton, _deleteStepButton, _recordButton, _addStepButton,
        });
    }

    private void BuildStepDetailCard()
    {
        Card card = _stepDetailCard;
        ConfigureCard(card, ColumnRight, 72, 280);

        _noStepSelectedLabel.SetBounds(20, 40, 300, 20);
        card.Controls.Add(_noStepSelectedLabel);

        _kindSelector.SetBounds(20, 36, 240, 28);
        _kindSelector.SelectedIndexChanged += KindSelector_SelectedIndexChanged;
        card.Controls.Add(_kindSelector);

        Configure(_testStepButton, ButtonKind.Secondary, 272, 34, 90, 30);
        _testStepButton.Click += (_, _) => TestStep();
        card.Controls.Add(_testStepButton);

        // Row controls: every one of these is repositioned per step kind by ShowStepFields,
        // so only their change events and starting sizes are set here.
        _stepKeyBox.KeyValueChanged += (_, _) => SyncStepFieldsToModel();

        _stepButtonSelector.SelectedIndexChanged += (_, _) => SyncStepFieldsToModel();

        _stepDoubleClick.CheckedChanged += (_, _) => SyncStepFieldsToModel();

        _stepTargetSelector.SelectedIndexChanged += (_, _) => SyncStepFieldsToModel();

        _stepXInput.ValueChanged += (_, _) => SyncStepFieldsToModel();
        _stepYInput.ValueChanged += (_, _) => SyncStepFieldsToModel();
        Configure(_stepPickButton, ButtonKind.Secondary, 0, 0, 72, 30);
        _stepPickButton.Font = Theme.Base;
        _stepPickButton.Click += (_, _) => PickStepPoint();

        _stepScatter.ValueChanged += (_, _) => SyncStepFieldsToModel();

        _stepToXInput.ValueChanged += (_, _) => SyncStepFieldsToModel();
        _stepToYInput.ValueChanged += (_, _) => SyncStepFieldsToModel();
        Configure(_stepPickToButton, ButtonKind.Secondary, 0, 0, 72, 30);
        _stepPickToButton.Font = Theme.Base;
        _stepPickToButton.Click += (_, _) => PickStepDragTo();

        _stepDragDuration.ValueChanged += (_, _) => SyncStepFieldsToModel();

        _stepTextField.ValueChanged += (_, _) => SyncStepFieldsToModel();

        _stepDelayEditor.ValueChanged += (_, _) => SyncStepFieldsToModel();

        // A plain Panel has no themed frame of its own — BorderStyle.FixedSingle would draw in
        // a fixed system color that stays the same regardless of which palette is active. Drawn
        // by hand instead, in Theme.Border, so it matches every field beside it in both themes;
        // ThemeManager invalidates every control on a switch (see ApplyTo), themed or not, so
        // this repaints itself automatically without needing IThemedControl.
        _stepColorSwatch.BorderStyle = BorderStyle.None;
        _stepColorSwatch.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, _stepColorSwatch.Width - 1, _stepColorSwatch.Height - 1);
            using var path = Theme.RoundedRect(bounds, 6);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawPath(pen, path);
        };

        Configure(_stepCaptureColorButton, ButtonKind.Secondary, 0, 0, 90, 32);
        _stepCaptureColorButton.Click += (_, _) => CaptureStepColor();

        _stepTolerance.ValueChanged += (_, _) => SyncStepFieldsToModel();

        _stepTimeout.ValueChanged += (_, _) => SyncStepFieldsToModel();

        card.Controls.AddRange(new Control[]
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
        });
    }

    private void BuildRepeatCard()
    {
        Card card = _repeatCard;
        ConfigureCard(card, ColumnRight, 368, 260);

        _repeatSelector.SetBounds(20, 42, 404, 30);
        _repeatSelector.SelectedIndexChanged += OnSettingChanged;
        card.Controls.Add(_repeatSelector);

        _repeatLabel.SetBounds(20, 86, 88, 20);
        card.Controls.Add(_repeatLabel);

        _repetitions.SetBounds(114, 80, 118, 32);
        _repetitions.ValueChanged += OnSettingChanged;
        card.Controls.Add(_repetitions);

        _duration.SetBounds(114, 80, 118, 32);
        _duration.ValueChanged += OnSettingChanged;
        card.Controls.Add(_duration);

        _repeatHint.SetBounds(20, 116, 404, 18);
        card.Controls.Add(_repeatHint);

        _startDelayLabel.SetBounds(20, 150, 88, 20);
        card.Controls.Add(_startDelayLabel);

        _startDelay.SetBounds(114, 144, 84, 32);
        _startDelay.ValueChanged += OnSettingChanged;
        card.Controls.Add(_startDelay);

        _startDelayHint.SetBounds(206, 150, 218, 20);
        card.Controls.Add(_startDelayHint);

        _restrictWindowCheck.SetBounds(20, 184, 404, 24);
        _restrictWindowCheck.CheckedChanged += RestrictWindowCheck_CheckedChanged;
        card.Controls.Add(_restrictWindowCheck);

        _windowTitleField.SetBounds(20, 214, 300, 32);
        _windowTitleField.ValueChanged += WindowTitleField_ValueChanged;
        card.Controls.Add(_windowTitleField);

        Configure(_useCurrentWindowButton, ButtonKind.Secondary, 328, 214, 96, 32);
        _useCurrentWindowButton.Click += (_, _) => BeginCaptureWindowTitle();
        card.Controls.Add(_useCurrentWindowButton);
    }

    private void BuildHotkeyCard()
    {
        Card card = _hotkeyCard;
        ConfigureCard(card, ColumnLeft, 518, 216);

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
        ConfigureCard(card, ColumnRight, 644, 190);

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
    /// started from a phone, so it stays live on purpose. Sits in the gap the hotkeys card
    /// leaves below it.
    /// </summary>
    private void BuildRemoteCard()
    {
        _remoteCard.SetBounds(ColumnLeft, 750, CardWidth, 84);
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
        _runPanel.SetBounds(ColumnLeft, 850, (CardWidth * 2) + Gutter, 120);
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
