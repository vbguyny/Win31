namespace ProgramManager31.Controls;

internal sealed class ClassicCheckBox : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public ClassicCheckBox()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        TabStop = true;
        Font = ClassicTheme.UI;
        BackColor = ClassicTheme.Face;
        ForeColor = ClassicTheme.Black;
        Height = ClassicTheme.S(16);
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        int boxSize = ClassicTheme.S(13);
        var box = new Rectangle(ClassicTheme.S(1), (Height - boxSize) / 2, boxSize, boxSize);
        using (var white = new SolidBrush(ClassicTheme.Window))
            g.FillRectangle(white, box);
        using (var black = new Pen(ClassicTheme.Black))
            g.DrawRectangle(black, box);
        using (var dark = new Pen(ClassicTheme.Dark))
        using (var light = new Pen(ClassicTheme.Light))
        {
            g.DrawLine(dark, box.Left + 1, box.Top + 1, box.Right - 2, box.Top + 1);
            g.DrawLine(dark, box.Left + 1, box.Top + 1, box.Left + 1, box.Bottom - 2);
            g.DrawLine(light, box.Left + 1, box.Bottom - 1, box.Right - 1, box.Bottom - 1);
            g.DrawLine(light, box.Right - 1, box.Top + 1, box.Right - 1, box.Bottom - 1);
        }

        if (_checked)
            ClassicTheme.DrawCheckMark(g, box);

        var textRect = new Rectangle(box.Right + ClassicTheme.S(4), 0,
            Math.Max(0, Width - box.Right - ClassicTheme.S(4)), Height);
        ClassicTheme.DrawMnemonicText(g, Text, Font, Enabled ? ForeColor : ClassicTheme.DisabledText, textRect,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

        if (Focused && ShowFocusCues)
            ClassicTheme.DrawFocusRect(g, new Rectangle(textRect.Left - 1, textRect.Top + 1, textRect.Width, textRect.Height - 2));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            Focus();
            Checked = !Checked;
        }
        base.OnMouseDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && Enabled)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }
}
