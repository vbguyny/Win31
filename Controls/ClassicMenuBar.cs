namespace ProgramManager31.Controls;

internal sealed class ClassicMenuBar : Control
{
    private readonly List<string> _topItems = new();
    private readonly List<Rectangle> _itemRects = new();
    private ClassicMenuDropDown? _dropDown;
    private int _openIndex = -1;
    private int _focusIndex = 0;

    public Func<int, IReadOnlyList<MenuEntry>>? ItemsProvider { get; set; }
    public bool ShowChildControlBox { get; private set; }
    public bool ShowChildRestoreButton { get; private set; }
    public Action? ChildControlBoxClicked { get; private set; }
    public Action? ChildRestoreClicked { get; private set; }

    public ClassicMenuBar()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.MenuBackground;
        ForeColor = ClassicTheme.Black;
        Font = ClassicTheme.UI;
        TabStop = true;
        Height = ClassicTheme.MenuBar;
    }

    public bool IsOpen => _dropDown is { Visible: true };

    public void SetChildControls(bool showControlBox, Action? controlBoxAction,
        bool showRestoreButton, Action? restoreAction)
    {
        ShowChildControlBox = showControlBox;
        ChildControlBoxClicked = controlBoxAction;
        ShowChildRestoreButton = showRestoreButton;
        ChildRestoreClicked = restoreAction;
        Invalidate();
    }

    public void SetTopLevelItems(params string[] texts)
    {
        _topItems.Clear();
        _topItems.AddRange(texts);
        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _topItems.Count - 1));
        Invalidate();
    }

    public bool HandleMnemonic(char key)
    {
        for (int i = 0; i < _topItems.Count; i++)
        {
            var (clean, mnemonic) = ClassicTheme.StripMnemonic(_topItems[i]);
            if (mnemonic >= 0 && mnemonic < clean.Length &&
                char.ToUpperInvariant(clean[mnemonic]) == char.ToUpperInvariant(key))
            {
                _focusIndex = i;
                OpenMenu(i);
                return true;
            }
        }
        return false;
    }

    public void OpenMenu(int index)
    {
        if (index < 0 || index >= _topItems.Count)
            return;

        if (IsOpen && _openIndex == index)
        {
            CloseMenu();
            return;
        }

        CloseMenu();

        var entries = ItemsProvider?.Invoke(index);
        if (entries == null || entries.Count == 0)
            return;

        EnsureItemRects();
        var itemRect = _itemRects[index];
        var screen = PointToScreen(new Point(itemRect.Left, Height));
        var drop = new ClassicMenuDropDown(entries);
        _dropDown = drop;
        _openIndex = index;
        _focusIndex = index;
        drop.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_dropDown, drop))
            {
                _dropDown = null;
                _openIndex = -1;
                Invalidate();
                if (Focused)
                    Focus();
            }
        };
        drop.Navigate += direction =>
        {
            int next = (index + direction + _topItems.Count) % _topItems.Count;
            BeginInvoke(() => OpenMenu(next));
        };
        Invalidate();
        drop.ShowAt(this, screen);
    }

    public void CloseMenu()
    {
        var drop = _dropDown;
        _dropDown = null;
        _openIndex = -1;
        if (drop != null && !drop.IsDisposed)
            drop.Close();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.MenuBackground);
        EnsureItemRects();

        for (int i = 0; i < _topItems.Count; i++)
        {
            var rect = _itemRects[i];
            bool active = _openIndex == i;
            var color = ClassicTheme.Black;
            if (active)
            {
                using var brush = new SolidBrush(ClassicTheme.Highlight);
                g.FillRectangle(brush, rect);
                color = ClassicTheme.HighlightText;
            }

            var textRect = new Rectangle(rect.Left + ClassicTheme.S(8), rect.Top, rect.Width - ClassicTheme.S(16), rect.Height);
            ClassicTheme.DrawMnemonicText(g, _topItems[i], Font, color, textRect,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            if (Focused && _focusIndex == i && !active)
            {
                using var focus = new Pen(ClassicTheme.Black);
                focus.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawRectangle(focus, rect.Left + 2, rect.Top + 2, rect.Width - 5, rect.Height - 5);
            }
        }

        if (ShowChildControlBox)
        {
            var box = new Rectangle(ClassicTheme.S(2), ClassicTheme.S(1),
                Math.Min(ClassicTheme.SystemBox, Height - ClassicTheme.S(2)),
                Math.Min(ClassicTheme.SystemBox, Height - ClassicTheme.S(2)));
            ClassicTheme.DrawSystemBox(g, box);
        }

        if (ShowChildRestoreButton)
        {
            var button = new Rectangle(Width - ClassicTheme.S(2) - ClassicTheme.TitleButton,
                ClassicTheme.S(1), ClassicTheme.TitleButton, Height - ClassicTheme.S(2));
            ClassicTheme.DrawTitleButton(g, button, false, TitleButtonGlyph.Restore);
        }

        using var black = new Pen(ClassicTheme.Black);
        g.DrawLine(black, 0, Height - 1, Width, Height - 1);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (ShowChildControlBox)
            {
                var box = new Rectangle(ClassicTheme.S(2), ClassicTheme.S(1),
                    Math.Min(ClassicTheme.SystemBox, Height - ClassicTheme.S(2)),
                    Math.Min(ClassicTheme.SystemBox, Height - ClassicTheme.S(2)));
                if (box.Contains(e.Location))
                {
                    ChildControlBoxClicked?.Invoke();
                    return;
                }
            }
            if (ShowChildRestoreButton)
            {
                var button = new Rectangle(Width - ClassicTheme.S(2) - ClassicTheme.TitleButton,
                    ClassicTheme.S(1), ClassicTheme.TitleButton, Height - ClassicTheme.S(2));
                if (button.Contains(e.Location))
                {
                    ChildRestoreClicked?.Invoke();
                    return;
                }
            }

            int index = HitTest(e.Location);
            if (index >= 0)
            {
                Focus();
                OpenMenu(index);
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (IsOpen)
        {
            int index = HitTest(e.Location);
            if (index >= 0 && index != _openIndex)
                OpenMenu(index);
        }
        base.OnMouseMove(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_topItems.Count == 0)
            return;

        switch (e.KeyCode)
        {
            case Keys.Left:
                _focusIndex = (_focusIndex - 1 + _topItems.Count) % _topItems.Count;
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Right:
                _focusIndex = (_focusIndex + 1) % _topItems.Count;
                Invalidate();
                e.Handled = true;
                break;
            case Keys.Down:
            case Keys.Enter:
                OpenMenu(_focusIndex);
                e.Handled = true;
                break;
            case Keys.Escape:
                CloseMenu();
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
                if (HandleMnemonic((char)key))
                    return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void EnsureItemRects()
    {
        _itemRects.Clear();
        using var g = CreateGraphics();
        int x = ShowChildControlBox ? ClassicTheme.S(21) : 0;
        foreach (var text in _topItems)
        {
            var size = ClassicTheme.MeasureMnemonicText(g, text, Font,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int width = size.Width + ClassicTheme.S(16);
            _itemRects.Add(new Rectangle(x, 0, width, Height));
            x += width;
        }
    }

    private int HitTest(Point point)
    {
        EnsureItemRects();
        for (int i = 0; i < _itemRects.Count; i++)
            if (_itemRects[i].Contains(point))
                return i;
        return -1;
    }
}
