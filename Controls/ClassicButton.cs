namespace ProgramManager31.Controls;

internal sealed class ClassicButton : Control
{
    private bool _pressed;
    private bool _hover;

    public bool Default { get; set; }

    public ClassicButton()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        TabStop = true;
        Font = ClassicTheme.UI;
        BackColor = ClassicTheme.Face;
        ForeColor = ClassicTheme.Black;
        Height = ClassicTheme.ButtonHeight;
        Width = ClassicTheme.S(70);
    }

    public char Mnemonic
    {
        get
        {
            var (clean, index) = ClassicTheme.StripMnemonic(Text);
            return index >= 0 && index < clean.Length ? clean[index] : '\0';
        }
    }

    public void PerformClick()
    {
        if (!Enabled)
            return;
        OnClick(EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        var outer = ClientRectangle;
        var button = outer;
        if (Default)
        {
            using var black = new Pen(ClassicTheme.Black);
            g.DrawRectangle(black, outer.X, outer.Y, outer.Width - 1, outer.Height - 1);
            button = Rectangle.Inflate(outer, -1, -1);
        }

        using (var face = new SolidBrush(ClassicTheme.Face))
            g.FillRectangle(face, button);

        if (_pressed && _hover)
        {
            using var dark = new Pen(ClassicTheme.Dark);
            using var light = new Pen(ClassicTheme.Light);
            using var black = new Pen(ClassicTheme.Black);
            g.DrawRectangle(black, button.X, button.Y, button.Width - 1, button.Height - 1);
            g.DrawLine(dark, button.Left + 1, button.Top + 1, button.Right - 1, button.Top + 1);
            g.DrawLine(dark, button.Left + 1, button.Top + 1, button.Left + 1, button.Bottom - 1);
            g.DrawLine(light, button.Left + 1, button.Bottom - 1, button.Right - 1, button.Bottom - 1);
            g.DrawLine(light, button.Right - 1, button.Top + 1, button.Right - 1, button.Bottom - 1);
        }
        else
        {
            ClassicTheme.DrawRaisedBorder(g, button);
        }

        int offset = _pressed && _hover ? ClassicTheme.S(1) : 0;
        var textRect = new Rectangle(button.Left + offset, button.Top + offset, button.Width, button.Height);
        var color = Enabled ? ClassicTheme.Black : ClassicTheme.DisabledText;
        ClassicTheme.DrawMnemonicText(g, Text, Font, color, textRect,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(button, -ClassicTheme.S(4), -ClassicTheme.S(4));
            ClassicTheme.DrawFocusRect(g, focus);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            Focus();
            _pressed = true;
            _hover = true;
            Capture = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _pressed)
        {
            bool click = ClientRectangle.Contains(e.Location);
            _pressed = false;
            Capture = false;
            Invalidate();
            if (click)
                PerformClick();
        }
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && Enabled)
        {
            _pressed = true;
            Invalidate();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && _pressed)
        {
            _pressed = false;
            Invalidate();
            PerformClick();
            e.Handled = true;
        }
        base.OnKeyUp(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnLostFocus(e);
    }
}
