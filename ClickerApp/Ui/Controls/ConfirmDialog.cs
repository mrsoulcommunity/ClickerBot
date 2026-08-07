using System.Drawing.Drawing2D;

namespace ClickerApp;

/// <summary>
/// A small themed confirmation modal.
///
/// The system <see cref="MessageBox"/> is painted by Windows and stays light in a dark
/// palette, which is the one place the theme would visibly break, so it is replaced here.
/// </summary>
internal sealed class ConfirmDialog : Form
{
    private readonly string _message;
    private readonly FlatButton _cancel;

    private ConfirmDialog(string title, string message, string confirmText, bool destructive)
    {
        _message = message;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(392, 168);

        // The canvas color, not Surface: the buttons are Surface-filled and would otherwise
        // disappear into the dialog behind them.
        BackColor = Theme.Background;
        Font = Theme.Base;

        var titleLabel = new ThemedLabel { Text = title, Font = Theme.Title };
        titleLabel.SetBounds(28, 26, ClientSize.Width - 56, 28);
        Controls.Add(titleLabel);

        _cancel = new FlatButton { Text = "Cancel", Kind = ButtonKind.Secondary, DialogResult = DialogResult.Cancel };
        _cancel.SetBounds(ClientSize.Width - 268, 106, 120, 38);

        var confirm = new FlatButton
        {
            Text = confirmText,
            Kind = destructive ? ButtonKind.Danger : ButtonKind.Primary,
            DialogResult = DialogResult.Yes,
        };
        confirm.SetBounds(ClientSize.Width - 140, 106, 120, 38);

        Controls.Add(_cancel);
        Controls.Add(confirm);

        AcceptButton = confirm;
        CancelButton = _cancel;
    }

    /// <summary>Shows the dialog modally and returns true when the user confirms.</summary>
    public static bool Ask(IWin32Window owner, string title, string message, string confirmText, bool destructive = false)
    {
        using var dialog = new ConfirmDialog(title, message, confirmText, destructive);
        return dialog.ShowDialog(owner) == DialogResult.Yes;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Land on Cancel so a stray Enter cannot confirm a destructive action.
        _cancel.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var frame = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var pen = new Pen(Theme.BorderStrong))
        {
            g.DrawRectangle(pen, frame);
        }

        var body = new Rectangle(28, 62, ClientSize.Width - 56, 40);
        TextRenderer.DrawText(g, _message, Theme.Base, body, Theme.TextSecondary,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
    }
}
