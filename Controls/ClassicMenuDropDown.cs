namespace ProgramManager31.Controls;

internal sealed class MenuEntry
{
    public string Text { get; set; } = string.Empty;
    public string Shortcut { get; set; } = string.Empty;
    public bool IsSeparator { get; set; }
    public bool Checked { get; set; }
    public bool Enabled { get; set; } = true;
    public Action? Action { get; set; }
    public object? Tag { get; set; }

    public static MenuEntry Separator() => new() { IsSeparator = true };

    public static MenuEntry Item(string text, Action? action = null, string shortcut = "", bool enabled = true,
        bool check = false, object? tag = null) => new()
    {
        Text = text,
        Action = action,
        Shortcut = shortcut,
        Enabled = enabled,
        Checked = check,
        Tag = tag
    };
}

internal sealed class ClassicMenuDropDown : Form
{
    private static readonly HashSet<Form> OpenOwners = new();
    private readonly List<MenuEntry> _items;
    private int _selectedIndex = -1;
    private int _pressedIndex = -1;
    private bool _executing;
    private Form? _ownerForm;

    public event Action<int>? Navigate;

    public static bool HasOpenMenuFor(Form form)
    {
        lock (OpenOwners)
            return OpenOwners.Contains(form);
    }

    public ClassicMenuDropDown(IEnumerable<MenuEntry> items)
    {
        _items = items.ToList();
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = ClassicTheme.MenuBackground;
        Font = ClassicTheme.UI;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        Size = MeasureMenu();
        _selectedIndex = FirstSelectable();
    }

    public IReadOnlyList<MenuEntry> Items => _items;

    public void ShowAt(Control owner, Point screenLocation, int minimumWidth = 0)
    {
        var desired = MeasureMenu();
        int width = Math.Max(desired.Width, minimumWidth);
        Width = width;
        Height = desired.Height;

        var screen = Screen.FromPoint(screenLocation).WorkingArea;
        int x = Math.Min(screenLocation.X, screen.Right - Width);
        int y = screenLocation.Y;
        if (y + Height > screen.Bottom)
        {
            var ownerPoint = owner.PointToScreen(Point.Empty);
            y = ownerPoint.Y - Height;
        }
        x = Math.Max(screen.Left, x);
        y = Math.Max(screen.Top, y);

        Bounds = new Rectangle(x, y, Width, Height);
        _ownerForm = owner.FindForm();
        if (_ownerForm != null)
        {
            lock (OpenOwners)
                OpenOwners.Add(_ownerForm);
        }
        Show(_ownerForm);
        BringToFront();
    }

    public int IndexOfMnemonic(char key)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.IsSeparator || !item.Enabled)
                continue;
            var (_, mnemonic) = ClassicTheme.StripMnemonic(item.Text);
            if (mnemonic < 0)
                continue;
            var clean = ClassicTheme.StripMnemonic(item.Text).Text;
            if (mnemonic < clean.Length && char.ToUpperInvariant(clean[mnemonic]) == char.ToUpperInvariant(key))
                return i;
        }
        return -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.MenuBackground);

        using (var black = new Pen(ClassicTheme.Black))
        {
            g.DrawRectangle(black, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
        using (var light = new Pen(ClassicTheme.Light))
        using (var dark = new Pen(ClassicTheme.Dark))
        {
            g.DrawLine(light, 1, 1, ClientSize.Width - 2, 1);
            g.DrawLine(light, 1, 1, 1, ClientSize.Height - 2);
            g.DrawLine(dark, 1, ClientSize.Height - 2, ClientSize.Width - 2, ClientSize.Height - 2);
            g.DrawLine(dark, ClientSize.Width - 2, 1, ClientSize.Width - 2, ClientSize.Height - 2);
        }

        int y = ClassicTheme.S(2);
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.IsSeparator)
            {
                int lineY = y + ClassicTheme.S(2);
                using var dark = new Pen(ClassicTheme.Dark);
                using var light = new Pen(ClassicTheme.Light);
                g.DrawLine(dark, ClassicTheme.S(4), lineY, ClientSize.Width - ClassicTheme.S(5), lineY);
                g.DrawLine(light, ClassicTheme.S(4), lineY + 1, ClientSize.Width - ClassicTheme.S(5), lineY + 1);
                y += ClassicTheme.S(5);
                continue;
            }

            var rect = new Rectangle(ClassicTheme.S(2), y, ClientSize.Width - ClassicTheme.S(4), ClassicTheme.S(18));
            bool selected = i == _selectedIndex;
            var textColor = item.Enabled ? ClassicTheme.Black : ClassicTheme.DisabledText;

            if (selected && item.Enabled)
            {
                using var highlight = new SolidBrush(ClassicTheme.Highlight);
                g.FillRectangle(highlight, rect);
                textColor = ClassicTheme.HighlightText;
            }

            if (item.Checked)
            {
                ClassicTheme.DrawCheckMark(g, new Rectangle(rect.Left + ClassicTheme.S(4), rect.Top + ClassicTheme.S(4), ClassicTheme.S(12), ClassicTheme.S(12)));
            }

            var textRect = new Rectangle(rect.Left + ClassicTheme.S(20), rect.Top, rect.Width - ClassicTheme.S(24), rect.Height);
            ClassicTheme.DrawMnemonicText(g, item.Text, Font, textColor, textRect,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            if (!string.IsNullOrEmpty(item.Shortcut))
            {
                var shortcutRect = new Rectangle(rect.Left + ClassicTheme.S(80), rect.Top, rect.Width - ClassicTheme.S(88), rect.Height);
                TextRenderer.DrawText(g, item.Shortcut, Font, shortcutRect, textColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }

            y += ClassicTheme.S(18);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int index = HitTest(e.Location);
        if (index >= 0 && index != _selectedIndex)
        {
            _selectedIndex = index;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressedIndex = HitTest(e.Location);
            _selectedIndex = _pressedIndex;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int index = HitTest(e.Location);
            if (index >= 0 && index == _pressedIndex)
                Execute(index);
            _pressedIndex = -1;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Keys.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Keys.Home:
                _selectedIndex = FirstSelectable();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.End:
                _selectedIndex = LastSelectable();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Enter:
                if (_selectedIndex >= 0)
                    Execute(_selectedIndex);
                e.Handled = true;
                break;
            case Keys.Escape:
                Close();
                e.Handled = true;
                break;
            case Keys.Left:
                Navigate?.Invoke(-1);
                Close();
                e.Handled = true;
                break;
            case Keys.Right:
                Navigate?.Invoke(1);
                Close();
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Alt) != 0)
        {
            var key = keyData & Keys.KeyCode;
            if (key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
            {
                int index = IndexOfMnemonic((char)key);
                if (index >= 0)
                {
                    Execute(index);
                    return true;
                }
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (!_executing && Visible)
            Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_ownerForm != null)
        {
            lock (OpenOwners)
                OpenOwners.Remove(_ownerForm);
            _ownerForm = null;
        }
        base.OnFormClosed(e);
    }

    private void Execute(int index)
    {
        if (index < 0 || index >= _items.Count)
            return;
        var item = _items[index];
        if (item.IsSeparator || !item.Enabled)
            return;

        _executing = true;
        var action = item.Action;
        Close();
        action?.Invoke();
    }

    private void MoveSelection(int direction)
    {
        if (_items.Count == 0)
            return;
        int index = _selectedIndex;
        for (int step = 0; step < _items.Count; step++)
        {
            index += direction;
            if (index < 0)
                index = _items.Count - 1;
            if (index >= _items.Count)
                index = 0;
            if (!_items[index].IsSeparator && _items[index].Enabled)
            {
                _selectedIndex = index;
                Invalidate();
                return;
            }
        }
    }

    private int FirstSelectable()
    {
        for (int i = 0; i < _items.Count; i++)
            if (!_items[i].IsSeparator && _items[i].Enabled)
                return i;
        return -1;
    }

    private int LastSelectable()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            if (!_items[i].IsSeparator && _items[i].Enabled)
                return i;
        return -1;
    }

    private int HitTest(Point point)
    {
        int y = ClassicTheme.S(2);
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            int height = item.IsSeparator ? ClassicTheme.S(5) : ClassicTheme.S(18);
            if (!item.IsSeparator && point.Y >= y && point.Y < y + height)
                return item.Enabled ? i : -1;
            y += height;
        }
        return -1;
    }

    private Size MeasureMenu()
    {
        int textWidth = 0;
        int height = ClassicTheme.S(4);
        using var g = CreateGraphics();
        foreach (var item in _items)
        {
            if (item.IsSeparator)
            {
                height += ClassicTheme.S(5);
                continue;
            }
            height += ClassicTheme.S(18);
            var textSize = ClassicTheme.MeasureMnemonicText(g, item.Text, Font,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int shortcutWidth = string.IsNullOrEmpty(item.Shortcut)
                ? 0
                : TextRenderer.MeasureText(g, item.Shortcut, Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + ClassicTheme.S(16);
            textWidth = Math.Max(textWidth, ClassicTheme.S(28) + textSize.Width + shortcutWidth);
        }
        return new Size(textWidth + ClassicTheme.S(12), height);
    }
}
