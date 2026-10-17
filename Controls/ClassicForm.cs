namespace ProgramManager31.Controls;

internal class ClassicForm : Form
{
    private bool _systemPressed;
    private bool _minPressed;
    private bool _maxPressed;
    private ClassicMenuDropDown? _systemMenu;

    public bool Resizable { get; set; } = true;
    public bool ShowMinimize { get; set; } = true;
    public bool ShowMaximize { get; set; } = true;
    public bool ShowSystemBox { get; set; } = true;

    public Panel Content { get; }

    public ClassicForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = ClassicTheme.Face;
        ForeColor = ClassicTheme.Black;
        Font = ClassicTheme.UI;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        Content = new Panel
        {
            BackColor = ClassicTheme.Face
        };
        Controls.Add(Content);
    }

    protected int SystemAreaWidth => ShowSystemBox ? ClassicTheme.SystemBox + ClassicTheme.S(2) : 0;

    protected Rectangle SystemBoxRect =>
        new(ClassicTheme.Frame + ClassicTheme.S(1), ClassicTheme.Frame + ClassicTheme.S(1),
            ClassicTheme.SystemBox, ClassicTheme.TitleBar - ClassicTheme.S(2));

    protected Rectangle MinButtonRect
    {
        get
        {
            if (!ShowMinimize)
                return Rectangle.Empty;
            int right = ClientSize.Width - ClassicTheme.Frame;
            int left = right - ClassicTheme.TitleButton;
            if (ShowMaximize)
                left -= ClassicTheme.S(1) + ClassicTheme.TitleButton;
            return new Rectangle(left, ClassicTheme.Frame, ClassicTheme.TitleButton, ClassicTheme.TitleBar);
        }
    }

    protected Rectangle MaxButtonRect
    {
        get
        {
            if (!ShowMaximize)
                return Rectangle.Empty;
            int right = ClientSize.Width - ClassicTheme.Frame;
            return new Rectangle(right - ClassicTheme.TitleButton, ClassicTheme.Frame, ClassicTheme.TitleButton, ClassicTheme.TitleBar);
        }
    }

    public Rectangle ContentBounds
    {
        get
        {
            int x = ClassicTheme.Frame;
            int y = ClassicTheme.Frame + ClassicTheme.TitleBar + ClassicTheme.S(1);
            int width = Math.Max(0, ClientSize.Width - ClassicTheme.Frame * 2);
            int height = Math.Max(0, ClientSize.Height - ClassicTheme.Frame * 2 - ClassicTheme.TitleBar - ClassicTheme.S(1));
            return new Rectangle(x, y, width, height);
        }
    }

    protected override void OnShown(EventArgs e)
    {
        MaximizedBounds = Screen.FromControl(this).WorkingArea;
        base.OnShown(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Content != null)
            Content.Bounds = ContentBounds;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.Face);
        var client = ClientRectangle;
        ClassicTheme.DrawWindowFrame(g, client);

        int titleTop = ClassicTheme.Frame;
        int titleHeight = ClassicTheme.TitleBar;
        int left = ClassicTheme.Frame;
        int right = ClientSize.Width - ClassicTheme.Frame;

        using (var face = new SolidBrush(ClassicTheme.Face))
            g.FillRectangle(face, left, titleTop, Math.Max(0, right - left), titleHeight);

        int blueLeft = left + SystemAreaWidth + (ShowSystemBox ? ClassicTheme.S(1) : 0);
        int blueRight = right;

        var maxRect = MaxButtonRect;
        var minRect = MinButtonRect;
        if (ShowMaximize && maxRect.Width > 0)
        {
            blueRight = maxRect.Left - ClassicTheme.S(1);
            using var black = new Pen(ClassicTheme.Black);
            g.DrawLine(black, maxRect.Left - 1, titleTop, maxRect.Left - 1, titleTop + titleHeight - 1);
        }
        if (ShowMinimize && minRect.Width > 0)
        {
            blueRight = minRect.Left - ClassicTheme.S(1);
            using var black = new Pen(ClassicTheme.Black);
            g.DrawLine(black, minRect.Left - 1, titleTop, minRect.Left - 1, titleTop + titleHeight - 1);
        }

        if (ShowSystemBox)
        {
            using var black = new Pen(ClassicTheme.Black);
            g.DrawLine(black, left + SystemAreaWidth, titleTop, left + SystemAreaWidth, titleTop + titleHeight - 1);
            ClassicTheme.DrawSystemBox(g, SystemBoxRect, _systemPressed);
        }

        bool active = ActiveForm == this || ClassicMenuDropDown.HasOpenMenuFor(this);
        var titleColor = active ? ClassicTheme.ActiveTitle : ClassicTheme.InactiveTitle;
        var titleTextColor = active ? ClassicTheme.ActiveTitleText : ClassicTheme.InactiveTitleText;
        var blueRect = new Rectangle(blueLeft, titleTop, Math.Max(0, blueRight - blueLeft), titleHeight);
        using (var brush = new SolidBrush(titleColor))
            g.FillRectangle(brush, blueRect);
        if (blueRect.Width > 0)
        {
            TextRenderer.DrawText(g, Text, ClassicTheme.Title, blueRect, titleTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        if (ShowMinimize && minRect.Width > 0)
            ClassicTheme.DrawTitleButton(g, minRect, _minPressed, TitleButtonGlyph.Minimize);

        if (ShowMaximize && maxRect.Width > 0)
        {
            var glyph = WindowState == FormWindowState.Maximized ? TitleButtonGlyph.Restore : TitleButtonGlyph.Maximize;
            ClassicTheme.DrawTitleButton(g, maxRect, _maxPressed, glyph);
        }

        using (var black = new Pen(ClassicTheme.Black))
            g.DrawLine(black, left, titleTop + titleHeight, right - 1, titleTop + titleHeight);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_ACTIVATEAPP)
            Invalidate(true);

        if (m.Msg == NativeMethods.WM_NCHITTEST)
        {
            var screenPoint = NativeMethods.GetScreenPoint(m.LParam);
            var point = PointToClient(screenPoint);
            m.Result = (IntPtr)HitTestChrome(point);
            return;
        }

        if (m.Msg == NativeMethods.WM_NCLBUTTONDBLCLK)
        {
            var screenPoint = NativeMethods.GetScreenPoint(m.LParam);
            var point = PointToClient(screenPoint);
            if (HitTestChrome(point) == NativeMethods.HTCAPTION && Resizable)
            {
                ToggleMaximize();
                m.Result = IntPtr.Zero;
                return;
            }
        }

        if (m.Msg == NativeMethods.WM_LBUTTONDBLCLK)
        {
            var point = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
            if (ShowSystemBox && SystemBoxRect.Contains(point))
            {
                Close();
                return;
            }
        }

        base.WndProc(ref m);
    }

    protected virtual int HitTestChrome(Point point)
    {
        if (Resizable)
        {
            int grip = ClassicTheme.S(5);
            bool left = point.X <= grip;
            bool right = point.X >= ClientSize.Width - grip;
            bool top = point.Y <= grip;
            bool bottom = point.Y >= ClientSize.Height - grip;
            if (left && top) return NativeMethods.HTTOPLEFT;
            if (right && top) return NativeMethods.HTTOPRIGHT;
            if (left && bottom) return NativeMethods.HTBOTTOMLEFT;
            if (right && bottom) return NativeMethods.HTBOTTOMRIGHT;
            if (left) return NativeMethods.HTLEFT;
            if (right) return NativeMethods.HTRIGHT;
            if (top) return NativeMethods.HTTOP;
            if (bottom) return NativeMethods.HTBOTTOM;
        }

        var title = new Rectangle(ClassicTheme.Frame, ClassicTheme.Frame,
            ClientSize.Width - ClassicTheme.Frame * 2, ClassicTheme.TitleBar);
        if (title.Contains(point))
        {
            if (ShowSystemBox && SystemBoxRect.Contains(point))
                return NativeMethods.HTCLIENT;
            if (ShowMinimize && MinButtonRect.Contains(point))
                return NativeMethods.HTCLIENT;
            if (ShowMaximize && MaxButtonRect.Contains(point))
                return NativeMethods.HTCLIENT;
            return NativeMethods.HTCAPTION;
        }
        return NativeMethods.HTCLIENT;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Alt | Keys.Space))
        {
            ShowSystemMenu();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (ShowSystemBox && SystemBoxRect.Contains(e.Location))
            {
                _systemPressed = true;
                Capture = true;
                Invalidate();
            }
            else if (ShowMinimize && MinButtonRect.Contains(e.Location))
            {
                _minPressed = true;
                Capture = true;
                Invalidate();
            }
            else if (ShowMaximize && MaxButtonRect.Contains(e.Location))
            {
                _maxPressed = true;
                Capture = true;
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_systemPressed && !SystemBoxRect.Contains(e.Location) && !Capture)
            _systemPressed = false;
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            bool wasSystem = _systemPressed;
            bool wasMin = _minPressed;
            bool wasMax = _maxPressed;
            _systemPressed = _minPressed = _maxPressed = false;
            Capture = false;
            Invalidate();

            if (wasSystem && SystemBoxRect.Contains(e.Location))
                ShowSystemMenu();
            else if (wasMin && MinButtonRect.Contains(e.Location))
                WindowState = FormWindowState.Minimized;
            else if (wasMax && MaxButtonRect.Contains(e.Location))
                ToggleMaximize();
        }
        base.OnMouseUp(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        Invalidate(true);
        base.OnActivated(e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        Invalidate(true);
        base.OnDeactivate(e);
    }

    public void ToggleMaximize()
    {
        if (!ShowMaximize)
            return;
        if (WindowState == FormWindowState.Maximized)
            WindowState = FormWindowState.Normal;
        else
        {
            MaximizedBounds = Screen.FromControl(this).WorkingArea;
            WindowState = FormWindowState.Maximized;
        }
        Invalidate();
    }

    public virtual void ShowSystemMenu()
    {
        if (_systemMenu is { Visible: true })
            return;

        var items = new List<MenuEntry>();
        bool maximized = WindowState == FormWindowState.Maximized;
        items.Add(MenuEntry.Item("&Restore", () => WindowState = FormWindowState.Normal, enabled: maximized));
        items.Add(MenuEntry.Item("&Move", () =>
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_MOVE, IntPtr.Zero);
        }));
        items.Add(MenuEntry.Item("&Size", () =>
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_SIZE, IntPtr.Zero);
        }, enabled: Resizable));
        items.Add(MenuEntry.Separator());
        items.Add(MenuEntry.Item("Mi&nimize", () => WindowState = FormWindowState.Minimized, enabled: ShowMinimize));
        items.Add(MenuEntry.Item("Ma&ximize", ToggleMaximize, enabled: ShowMaximize && !maximized));
        items.Add(MenuEntry.Separator());
        items.Add(MenuEntry.Item("&Close", Close, "Alt+F4"));

        var drop = new ClassicMenuDropDown(items);
        _systemMenu = drop;
        drop.FormClosed += (_, _) => _systemMenu = null;
        drop.ShowAt(this, PointToScreen(new Point(SystemBoxRect.Left, SystemBoxRect.Bottom + ClassicTheme.S(1))));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (Modal && DialogResult == DialogResult.None)
            DialogResult = DialogResult.Cancel;
        base.OnFormClosing(e);
    }
}
