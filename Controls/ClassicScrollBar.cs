namespace ProgramManager31.Controls;

internal sealed class ClassicScrollBar : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _largeChange = 10;
    private int _value;
    private bool _dragging;
    private int _dragStart;
    private int _dragValue;
    private bool _upPressed;
    private bool _downPressed;

    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public event EventHandler? ValueChanged;

    public ClassicScrollBar()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.Face;
        TabStop = true;
        Width = ClassicTheme.ScrollBar;
        Height = ClassicTheme.ScrollBar * 4;
    }

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; ClampValue(); Invalidate(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(value, _minimum); ClampValue(); Invalidate(); }
    }

    public int LargeChange
    {
        get => _largeChange;
        set { _largeChange = Math.Max(1, value); ClampValue(); Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, Math.Max(_minimum, _maximum - _largeChange));
            if (clamped == _value)
                return;
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetRange(int contentSize, int viewport)
    {
        _maximum = Math.Max(1, contentSize);
        _largeChange = Math.Max(1, viewport);
        ClampValue();
        Invalidate();
    }

    private void ClampValue()
    {
        int max = Math.Max(_minimum, _maximum - _largeChange);
        if (_value > max)
            _value = max;
        if (_value < _minimum)
            _value = _minimum;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.Face);

        var up = ArrowRect(0);
        var down = ArrowRect(1);
        var track = TrackRect();
        var thumb = ThumbRect();

        ClassicTheme.DrawDitherTrack(g, track);
        DrawArrowButton(g, up, Orientation == Orientation.Vertical ? false : true, _upPressed);
        DrawArrowButton(g, down, Orientation == Orientation.Vertical ? true : false, _downPressed);
        ClassicTheme.DrawRaisedBorder(g, thumb);

        if (Orientation == Orientation.Vertical)
        {
            for (int i = 1; i < 4; i++)
            {
                int y = thumb.Top + thumb.Height * i / 4;
                using var pen = new Pen(ClassicTheme.Dark);
                g.DrawLine(pen, thumb.Left + ClassicTheme.S(3), y, thumb.Right - ClassicTheme.S(4), y);
            }
        }
        else
        {
            for (int i = 1; i < 4; i++)
            {
                int x = thumb.Left + thumb.Width * i / 4;
                using var pen = new Pen(ClassicTheme.Dark);
                g.DrawLine(pen, x, thumb.Top + ClassicTheme.S(3), x, thumb.Bottom - ClassicTheme.S(4));
            }
        }
    }

    private void DrawArrowButton(Graphics g, Rectangle rect, bool downArrow, bool pressed)
    {
        using (var face = new SolidBrush(ClassicTheme.Face))
            g.FillRectangle(face, rect);
        if (pressed)
        {
            using var dark = new Pen(ClassicTheme.Dark);
            using var light = new Pen(ClassicTheme.Light);
            using var black = new Pen(ClassicTheme.Black);
            g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Right - 1, rect.Top + 1);
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Left + 1, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Right - 1, rect.Top + 1, rect.Right - 1, rect.Bottom - 1);
        }
        else
        {
            ClassicTheme.DrawRaisedBorder(g, rect);
        }
        var inner = Rectangle.Inflate(rect, -ClassicTheme.S(4), -ClassicTheme.S(4));
        if (Orientation == Orientation.Vertical)
            ClassicTheme.DrawTriangle(g, inner, !downArrow);
        else
            DrawHorizontalTriangle(g, inner, !downArrow);
    }

    private static void DrawHorizontalTriangle(Graphics g, Rectangle rect, bool left)
    {
        int w = Math.Max(ClassicTheme.S(3), rect.Width / 2);
        int h = Math.Max(ClassicTheme.S(5), rect.Height - ClassicTheme.S(2));
        int cx = rect.Left + rect.Width / 2;
        int cy = rect.Top + rect.Height / 2;
        Point[] points = left
            ? new[] { new Point(cx + w / 2, cy - h / 2), new Point(cx + w / 2, cy + h / 2), new Point(cx - w / 2, cy) }
            : new[] { new Point(cx - w / 2, cy - h / 2), new Point(cx - w / 2, cy + h / 2), new Point(cx + w / 2, cy) };
        using var brush = new SolidBrush(ClassicTheme.Black);
        g.FillPolygon(brush, points);
    }

    private Rectangle ArrowRect(int index)
    {
        int size = ClassicTheme.ScrollBar;
        if (Orientation == Orientation.Vertical)
            return new Rectangle(0, index == 0 ? 0 : Height - size, Width, size);
        return new Rectangle(index == 0 ? 0 : Width - size, 0, size, Height);
    }

    private Rectangle TrackRect()
    {
        int size = ClassicTheme.ScrollBar;
        if (Orientation == Orientation.Vertical)
            return new Rectangle(0, size, Width, Math.Max(0, Height - size * 2));
        return new Rectangle(size, 0, Math.Max(0, Width - size * 2), Height);
    }

    private Rectangle ThumbRect()
    {
        var track = TrackRect();
        int span = Orientation == Orientation.Vertical ? track.Height : track.Width;
        if (span <= 0)
            return Rectangle.Empty;

        int range = Math.Max(1, _maximum);
        int thumbLength = Math.Max(ClassicTheme.ScrollBar, (int)((long)span * _largeChange / range));
        thumbLength = Math.Min(thumbLength, span);
        int scrollable = Math.Max(1, _maximum - _largeChange);
        int offset = (int)((long)(_value - _minimum) * (span - thumbLength) / scrollable);
        offset = Math.Clamp(offset, 0, Math.Max(0, span - thumbLength));
        if (Orientation == Orientation.Vertical)
            return new Rectangle(track.Left, track.Top + offset, track.Width, thumbLength);
        return new Rectangle(track.Left + offset, track.Top, thumbLength, track.Height);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        Focus();
        var up = ArrowRect(0);
        var down = ArrowRect(1);
        var thumb = ThumbRect();

        if (up.Contains(e.Location))
        {
            _upPressed = true;
            Value -= 1;
        }
        else if (down.Contains(e.Location))
        {
            _downPressed = true;
            Value += 1;
        }
        else if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragStart = Orientation == Orientation.Vertical ? e.Y : e.X;
            _dragValue = _value;
            Capture = true;
        }
        else if (TrackRect().Contains(e.Location))
        {
            bool before = Orientation == Orientation.Vertical ? e.Y < thumb.Top : e.X < thumb.Left;
            Value += before ? -_largeChange : _largeChange;
        }
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            var track = TrackRect();
            var thumb = ThumbRect();
            int span = Orientation == Orientation.Vertical ? track.Height : track.Width;
            int thumbLength = Orientation == Orientation.Vertical ? thumb.Height : thumb.Width;
            int movable = Math.Max(1, span - thumbLength);
            int delta = (Orientation == Orientation.Vertical ? e.Y : e.X) - _dragStart;
            int scrollable = Math.Max(1, _maximum - _largeChange);
            Value = _dragValue + (int)((long)delta * scrollable / movable);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        _upPressed = false;
        _downPressed = false;
        Capture = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Left:
                Value -= 1;
                e.Handled = true;
                break;
            case Keys.Down:
            case Keys.Right:
                Value += 1;
                e.Handled = true;
                break;
            case Keys.PageUp:
                Value -= _largeChange;
                e.Handled = true;
                break;
            case Keys.PageDown:
                Value += _largeChange;
                e.Handled = true;
                break;
            case Keys.Home:
                Value = _minimum;
                e.Handled = true;
                break;
            case Keys.End:
                Value = _maximum;
                e.Handled = true;
                break;
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
