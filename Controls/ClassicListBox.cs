namespace ProgramManager31.Controls;

internal sealed class ClassicListBox : Control
{
    public ListBox Inner { get; }

    public ClassicListBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.Window;
        Font = ClassicTheme.UI;
        Height = ClassicTheme.S(80);
        TabStop = false;

        Inner = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            BackColor = ClassicTheme.Window,
            ForeColor = ClassicTheme.Black,
            Font = ClassicTheme.UI,
            IntegralHeight = false,
            ItemHeight = ClassicTheme.ListItemHeight,
            TabStop = true
        };
        Inner.DrawItem += DrawItem;
        Inner.GotFocus += (_, _) => Invalidate();
        Inner.LostFocus += (_, _) => Invalidate();
        Controls.Add(Inner);
    }

    public ListBox.ObjectCollection Items => Inner.Items;

    public new void Focus() => Inner.Focus();

    public int SelectedIndex
    {
        get => Inner.SelectedIndex;
        set => Inner.SelectedIndex = value;
    }

    public object? SelectedItem
    {
        get => Inner.SelectedItem;
        set => Inner.SelectedItem = value;
    }

    public event EventHandler? SelectedIndexChanged
    {
        add => Inner.SelectedIndexChanged += value;
        remove => Inner.SelectedIndexChanged -= value;
    }

    public new event EventHandler? DoubleClick
    {
        add => Inner.DoubleClick += value;
        remove => Inner.DoubleClick -= value;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Inner != null)
            Inner.Bounds = new Rectangle(ClassicTheme.S(3), ClassicTheme.S(3),
                Math.Max(0, Width - ClassicTheme.S(6)), Math.Max(0, Height - ClassicTheme.S(6)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        ClassicTheme.DrawSunkenBorder(e.Graphics, ClientRectangle);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Inner.Focus();
    }

    private void DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;

        bool selected = (e.State & DrawItemState.Selected) != 0;
        var back = selected ? ClassicTheme.Highlight : ClassicTheme.Window;
        var fore = selected ? ClassicTheme.HighlightText : ClassicTheme.Black;
        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, e.Bounds);
        var text = Inner.Items[e.Index]?.ToString() ?? string.Empty;
        var rect = new Rectangle(e.Bounds.Left + ClassicTheme.S(2), e.Bounds.Top, e.Bounds.Width - ClassicTheme.S(4), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, Inner.Font, rect, fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (selected && Inner.Focused)
            ClassicTheme.DrawFocusRect(e.Graphics, rect);
    }
}
