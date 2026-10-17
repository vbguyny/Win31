namespace ProgramManager31.Controls;

internal sealed class ClassicComboBox : Control
{
    private readonly List<string> _items = new();
    private int _selectedIndex = -1;
    private ClassicMenuDropDown? _dropDown;

    public event EventHandler? SelectedIndexChanged;

    public ClassicComboBox()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        TabStop = true;
        Font = ClassicTheme.UI;
        BackColor = ClassicTheme.Face;
        ForeColor = ClassicTheme.Black;
        Height = ClassicTheme.S(21);
        Width = ClassicTheme.S(140);
    }

    public IReadOnlyList<string> Items => _items;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = value >= 0 && value < _items.Count ? value : -1;
            if (clamped == _selectedIndex)
                return;
            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public void SetItems(IEnumerable<string> items)
    {
        _items.Clear();
        _items.AddRange(items);
        if (_selectedIndex >= _items.Count)
            _selectedIndex = _items.Count - 1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        var buttonWidth = ClassicTheme.S(17);
        var field = new Rectangle(0, 0, Math.Max(0, Width - buttonWidth), Height);
        using (var white = new SolidBrush(ClassicTheme.Window))
            g.FillRectangle(white, field);
        ClassicTheme.DrawThinSunkenBorder(g, field);

        var textRect = new Rectangle(ClassicTheme.S(4), 0, Math.Max(0, field.Width - ClassicTheme.S(6)), Height);
        TextRenderer.DrawText(g, SelectedItem ?? string.Empty, Font, textRect,
            Enabled ? ClassicTheme.Black : ClassicTheme.DisabledText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var button = new Rectangle(field.Right, 0, buttonWidth, Height);
        using (var face = new SolidBrush(ClassicTheme.Face))
            g.FillRectangle(face, button);
        ClassicTheme.DrawRaisedBorder(g, button);
        ClassicTheme.DrawTriangle(g, Rectangle.Inflate(button, -ClassicTheme.S(5), -ClassicTheme.S(5)), false);

        if (Focused && ShowFocusCues)
            ClassicTheme.DrawFocusRect(g, new Rectangle(ClassicTheme.S(3), ClassicTheme.S(3),
                Math.Max(0, field.Width - ClassicTheme.S(6)), Height - ClassicTheme.S(6)));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Enabled)
        {
            Focus();
            ShowList();
        }
        base.OnMouseDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Enabled)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Down:
                if (_dropDown is { Visible: true })
                    break;
                if (e.Alt)
                    ShowList();
                else if (_selectedIndex < _items.Count - 1)
                    SelectedIndex++;
                e.Handled = true;
                break;
            case Keys.Up:
                if (_selectedIndex > 0)
                    SelectedIndex--;
                e.Handled = true;
                break;
            case Keys.Space:
            case Keys.F4:
                ShowList();
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

    private void ShowList()
    {
        if (_dropDown is { Visible: true } || _items.Count == 0)
            return;

        var entries = new List<MenuEntry>();
        for (int i = 0; i < _items.Count; i++)
        {
            int index = i;
            entries.Add(MenuEntry.Item(_items[i].Replace("&", "&&"), () => SelectedIndex = index, check: i == _selectedIndex));
        }
        var drop = new ClassicMenuDropDown(entries);
        _dropDown = drop;
        drop.FormClosed += (_, _) => _dropDown = null;
        drop.ShowAt(this, PointToScreen(new Point(0, Height)), Width);
    }
}
