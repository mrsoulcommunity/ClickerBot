using System.Drawing.Drawing2D;

namespace ClickerBot;

/// <summary>Owner-drawn list of run history entries: two lines per row, newest first.</summary>
internal sealed class HistoryListBox : ListBox, IThemedControl
{
    public HistoryListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        Font = Theme.Base;
        ItemHeight = 56;
        IntegralHeight = false;
        SelectionMode = SelectionMode.None;
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        BackColor = Theme.Surface;
        ForeColor = Theme.TextPrimary;
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var background = new SolidBrush(Theme.Surface))
        {
            g.FillRectangle(background, e.Bounds);
        }

        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not RunHistoryEntry entry)
        {
            return;
        }

        var row = new Rectangle(e.Bounds.X + 20, e.Bounds.Y, e.Bounds.Width - 40, e.Bounds.Height);

        Color outcomeColor = entry.Outcome switch
        {
            "Finished" or "Test complete" => Theme.Success,
            _ when entry.Outcome.StartsWith("Stopped", StringComparison.Ordinal) => Theme.TextSecondary,
            _ => Theme.Danger,
        };

        var nameArea = new Rectangle(row.X, row.Y + 8, row.Width - 160, 20);
        TextRenderer.DrawText(g, entry.ProfileName, Theme.Base, nameArea, Theme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var outcomeArea = new Rectangle(row.Right - 150, row.Y + 8, 150, 20);
        TextRenderer.DrawText(g, entry.Outcome, Theme.Base, outcomeArea, outcomeColor,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        string detail = $"{entry.StartedAt.LocalDateTime:g} · {FormatElapsed(entry.Elapsed)} · " +
            $"{entry.Iterations:N0} iteration{(entry.Iterations == 1 ? "" : "s")} · {ActionModeNames.Describe(entry.Mode)}";
        var detailArea = new Rectangle(row.X, row.Y + 30, row.Width, 18);
        TextRenderer.DrawText(g, detail, Theme.Small, detailArea, Theme.TextSecondary,
            TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (e.Index < Items.Count - 1)
        {
            using var pen = new Pen(Theme.Border);
            g.DrawLine(pen, e.Bounds.X + 20, e.Bounds.Bottom - 1, e.Bounds.Right - 20, e.Bounds.Bottom - 1);
        }
    }

    private static string FormatElapsed(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}h {value.Minutes}m"
        : value.TotalMinutes >= 1
            ? $"{(int)value.TotalMinutes}m {value.Seconds}s"
            : $"{value.Seconds}s";
}

/// <summary>
/// Lists finished runs — what ran, when, for how long, and how it ended — so a run left going
/// unattended has something to show for itself afterwards.
/// </summary>
internal sealed class RunHistoryDialog : Form
{
    private readonly RunHistoryStore _history;
    private readonly HistoryListBox _list = new();
    private readonly ThemedLabel _empty;
    private readonly FlatButton _clear;

    private RunHistoryDialog(IWin32Window owner, RunHistoryStore history)
    {
        _history = history;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        Text = "Run history";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(560, 520);

        BackColor = Theme.Background;
        Font = Theme.Base;

        var titleLabel = new ThemedLabel { Text = "Run history", Font = Theme.Title };
        titleLabel.SetBounds(28, 24, ClientSize.Width - 56, 28);
        Controls.Add(titleLabel);

        _list.SetBounds(24, 64, ClientSize.Width - 48, ClientSize.Height - 140);
        Controls.Add(_list);

        _empty = new ThemedLabel
        {
            Text = "No runs yet — start one and it will show up here.",
            Role = TextRole.Secondary,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _empty.SetBounds(24, 64, ClientSize.Width - 48, ClientSize.Height - 140);
        Controls.Add(_empty);

        _clear = new FlatButton { Text = "Clear history", Kind = ButtonKind.Danger };
        _clear.SetBounds(24, ClientSize.Height - 58, 150, 38);
        _clear.Click += (_, _) => ClearHistory(owner);
        Controls.Add(_clear);

        var close = new FlatButton { Text = "Close", Kind = ButtonKind.Primary, DialogResult = DialogResult.OK };
        close.SetBounds(ClientSize.Width - 144, ClientSize.Height - 58, 120, 38);
        Controls.Add(close);

        AcceptButton = close;
        CancelButton = close;

        Populate();
    }

    /// <summary>Shows the history dialog modally.</summary>
    public static void Show(IWin32Window owner, RunHistoryStore history)
    {
        using var dialog = new RunHistoryDialog(owner, history);
        dialog.ShowDialog(owner);
    }

    private void ClearHistory(IWin32Window owner)
    {
        if (_history.Entries.Count == 0)
        {
            return;
        }

        bool confirmed = ConfirmDialog.Ask(owner, "Clear history",
            "Every entry will be removed. This cannot be undone.", "Clear", destructive: true);
        if (!confirmed)
        {
            return;
        }

        _history.Clear();
        TrySave();
        Populate();
    }

    private void TrySave()
    {
        try
        {
            _history.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort: the in-memory list (and this dialog) already reflect the change,
            // and the next successful save picks up from wherever this one left off.
        }
    }

    private void Populate()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var entry in _history.Entries)
        {
            _list.Items.Add(entry);
        }

        _list.EndUpdate();

        bool empty = _history.Entries.Count == 0;
        _list.Visible = !empty;
        _empty.Visible = empty;
        _clear.Enabled = !empty;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var frame = new Rectangle(0, 0, Width - 1, Height - 1);
        using var pen = new Pen(Theme.BorderStrong);
        g.DrawRectangle(pen, frame);
    }
}
