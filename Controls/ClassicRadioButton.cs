namespace ProgramManager31.Controls;

internal sealed class ClassicRadioButton : Control
{
    private bool _checked;

    public event EventHandler? CheckedChanged;

    public string GroupName { get; set; } = string.Empty;

    public ClassicRadioButton()
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
            if (value)
                UncheckSiblings();
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        int size = ClassicTheme.S(12);
        var circle = new Rectangle(ClassicTheme.S(1), (Height - size) / 2, size, size);
        using (var white = new SolidBrush(ClassicTheme.Window))
            g.FillEllipse(white, circle);
        using (var black = new Pen(ClassicTheme.Black))
            g.DrawEllipse(black, circle);
        using (var dark = new Pen(ClassicTheme.Dark))
        {
            g.DrawArc(dark, circle, 90, 180);
        }
        using (var light = new Pen(ClassicTheme.Light))
        {
            g.DrawArc(light, circle, 270, 180);
        }

        if (_checked)
            ClassicTheme.DrawRadioDot(g, circle);

        var textRect = new Rectangle(circle.Right + ClassicTheme.S(4), 0,
            Math.Max(0, Width - circle.Right - ClassicTheme.S(4)), Height);
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
            Checked = true;
        }
        base.OnMouseDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && Enabled)
        {
            Checked = true;
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

    private void UncheckSiblings()
    {
        if (Parent == null || string.IsNullOrEmpty(GroupName))
            return;
        foreach (Control control in Parent.Controls)
        {
            if (control != this && control is ClassicRadioButton radio &&
                string.Equals(radio.GroupName, GroupName, StringComparison.Ordinal))
            {
                radio.Checked = false;
            }
        }
    }
}
