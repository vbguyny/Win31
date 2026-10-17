using ProgramManager31.Icons;

namespace ProgramManager31.Desktop;

internal sealed class DragGhostForm : Form
{
    private static readonly Color Key = Color.FromArgb(255, 0, 255);
    private string _iconKey = "application";

    public DragGhostForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Key;
        TransparencyKey = Key;
        Size = new Size(ClassicTheme.IconSize, ClassicTheme.IconSize);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void ShowItem(string iconKey, Point screenPoint)
    {
        _iconKey = iconKey;
        MoveTo(screenPoint);
        if (!Visible)
            Show();
        Invalidate();
    }

    public void MoveTo(Point screenPoint)
    {
        var screen = Screen.FromPoint(screenPoint).WorkingArea;
        int x = Math.Clamp(screenPoint.X - Width / 2, screen.Left, Math.Max(screen.Left, screen.Right - Width));
        int y = Math.Clamp(screenPoint.Y - Height / 2, screen.Top, Math.Max(screen.Top, screen.Bottom - Height));
        Location = new Point(x, y);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Key);
        IconPainter.DrawIcon(e.Graphics, _iconKey, new Rectangle(0, 0, Width, Height));
    }
}
