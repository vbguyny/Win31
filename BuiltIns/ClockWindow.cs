using ProgramManager31.Controls;

namespace ProgramManager31.BuiltIns;

internal sealed class ClockWindow : ClassicForm
{
    private readonly ClassicMenuBar _menuBar;
    private readonly ClockFace _face;

    public ClockWindow()
    {
        Text = "Clock";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(ClassicTheme.S(260), ClassicTheme.S(280));
        MinimumSize = new Size(ClassicTheme.S(180), ClassicTheme.S(180));
        Resizable = true;
        ShowMinimize = true;
        ShowMaximize = true;

        _face = new ClockFace();
        _menuBar = new ClassicMenuBar { ItemsProvider = BuildMenu };
        _menuBar.SetTopLevelItems("&Settings", "&Help");

        Content.Controls.Add(_face);
        Content.Controls.Add(_menuBar);
        _menuBar.BringToFront();
        Content.Resize += (_, _) => LayoutClock();
        LayoutClock();
    }

    private void LayoutClock()
    {
        int menuHeight = ClassicTheme.MenuBar;
        _menuBar.Bounds = new Rectangle(0, 0, Content.ClientSize.Width, menuHeight);
        _face.Bounds = new Rectangle(0, menuHeight, Content.ClientSize.Width,
            Math.Max(0, Content.ClientSize.Height - menuHeight));
    }

    private IReadOnlyList<MenuEntry> BuildMenu(int index)
    {
        if (index == 0)
        {
            return new[]
            {
                MenuEntry.Item("&Analog", () => SetMode(true), check: _face.Analog),
                MenuEntry.Item("&Digital", () => SetMode(false), check: !_face.Analog),
                MenuEntry.Separator(),
                MenuEntry.Item("Show &Seconds", ToggleSeconds, check: _face.ShowSeconds),
                MenuEntry.Item("Show &Date", ToggleDate, check: _face.ShowDate)
            };
        }

        return new[]
        {
            MenuEntry.Item("&About Clock", () => ClassicMessageBox.ShowInfo(this,
                "Clock\nWindows 3.1 style recreation\n\nAn original analog and digital clock.", "About Clock"))
        };
    }

    private void SetMode(bool analog)
    {
        _face.Analog = analog;
        _face.Invalidate();
    }

    private void ToggleSeconds()
    {
        _face.ShowSeconds = !_face.ShowSeconds;
        _face.Invalidate();
    }

    private void ToggleDate()
    {
        _face.ShowDate = !_face.ShowDate;
        _face.Invalidate();
    }

    private sealed class ClockFace : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Font _digitalFont;

        public bool Analog { get; set; } = true;
        public bool ShowSeconds { get; set; }
        public bool ShowDate { get; set; }

        public ClockFace()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = ClassicTheme.Window;
            _digitalFont = new Font("Consolas", 28f, FontStyle.Bold, GraphicsUnit.Point);
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (_, _) => Invalidate();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _digitalFont.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(ClassicTheme.Window);
            var now = DateTime.Now;
            if (Analog)
                DrawAnalog(g, now);
            else
                DrawDigital(g, now);
        }

        private void DrawAnalog(Graphics g, DateTime now)
        {
            int size = Math.Min(Width, Height) - ClassicTheme.S(20);
            if (size < 40)
                return;
            var face = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
            using (var white = new SolidBrush(ClassicTheme.Window))
                g.FillEllipse(white, face);
            using (var black = new Pen(ClassicTheme.Black, Math.Max(1, ClassicTheme.S(2))))
                g.DrawEllipse(black, face);

            int cx = face.Left + face.Width / 2;
            int cy = face.Top + face.Height / 2;
            int radius = size / 2 - ClassicTheme.S(3);

            for (int i = 0; i < 60; i++)
            {
                double angle = i * Math.PI / 30 - Math.PI / 2;
                bool hour = i % 5 == 0;
                int outer = radius;
                int inner = radius - ClassicTheme.S(hour ? 7 : 3);
                using var pen = new Pen(ClassicTheme.Black, Math.Max(1, ClassicTheme.S(hour ? 2 : 1)));
                g.DrawLine(pen,
                    cx + (int)(Math.Cos(angle) * inner), cy + (int)(Math.Sin(angle) * inner),
                    cx + (int)(Math.Cos(angle) * outer), cy + (int)(Math.Sin(angle) * outer));
            }

            double hourAngle = (now.Hour % 12 + now.Minute / 60.0) * Math.PI / 6 - Math.PI / 2;
            double minuteAngle = (now.Minute + now.Second / 60.0) * Math.PI / 30 - Math.PI / 2;
            double secondAngle = now.Second * Math.PI / 30 - Math.PI / 2;

            DrawHand(g, cx, cy, hourAngle, radius * 0.5, Math.Max(2, ClassicTheme.S(3)), ClassicTheme.Black);
            DrawHand(g, cx, cy, minuteAngle, radius * 0.72, Math.Max(1, ClassicTheme.S(2)), ClassicTheme.Black);
            if (ShowSeconds)
                DrawHand(g, cx, cy, secondAngle, radius * 0.82, Math.Max(1, ClassicTheme.S(1)), Color.Red);

            using (var dot = new SolidBrush(ClassicTheme.Black))
                g.FillEllipse(dot, cx - ClassicTheme.S(2), cy - ClassicTheme.S(2),
                    ClassicTheme.S(5), ClassicTheme.S(5));

            if (ShowDate)
            {
                var dateRect = new Rectangle(0, face.Bottom + ClassicTheme.S(2), Width, ClassicTheme.S(18));
                TextRenderer.DrawText(g, now.ToLongDateString(), ClassicTheme.UI, dateRect, ClassicTheme.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        private static void DrawHand(Graphics g, int cx, int cy, double angle, double length, int width, Color color)
        {
            using var pen = new Pen(color, width);
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            g.DrawLine(pen, cx, cy,
                cx + (int)(Math.Cos(angle) * length),
                cy + (int)(Math.Sin(angle) * length));
        }

        private void DrawDigital(Graphics g, DateTime now)
        {
            string text = ShowSeconds ? now.ToString("h:mm:ss tt") : now.ToString("h:mm tt");
            using (var brush = new SolidBrush(ClassicTheme.Black))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                var rect = new RectangleF(0, 0, Width, Height - (ShowDate ? ClassicTheme.S(24) : 0));
                g.DrawString(text, _digitalFont, brush, rect, format);
            }
            if (ShowDate)
            {
                var dateRect = new Rectangle(0, Height - ClassicTheme.S(24), Width, ClassicTheme.S(20));
                TextRenderer.DrawText(g, now.ToLongDateString(), ClassicTheme.UI, dateRect, ClassicTheme.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }
}
