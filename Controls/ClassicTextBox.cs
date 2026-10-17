using System.Diagnostics.CodeAnalysis;

namespace ProgramManager31.Controls;

internal class ClassicTextBox : Control
{
    public TextBox Inner { get; }

    public ClassicTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.Window;
        Font = ClassicTheme.UI;
        Height = ClassicTheme.S(21);
        TabStop = false;

        Inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = ClassicTheme.Window,
            ForeColor = ClassicTheme.Black,
            Font = ClassicTheme.UI,
            AutoSize = false,
            TabStop = true
        };
        Inner.GotFocus += (_, _) => Invalidate();
        Inner.LostFocus += (_, _) => Invalidate();
        Controls.Add(Inner);
    }

    [AllowNull]
    public override string Text
    {
        get => Inner.Text;
        set => Inner.Text = value ?? string.Empty;
    }

    public bool ReadOnly
    {
        get => Inner.ReadOnly;
        set => Inner.ReadOnly = value;
    }

    public bool Multiline
    {
        get => Inner.Multiline;
        set
        {
            Inner.Multiline = value;
            Inner.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None;
        }
    }

    public int MaxLength
    {
        get => Inner.MaxLength;
        set => Inner.MaxLength = value;
    }

    public void SelectAll() => Inner.SelectAll();

    public new void Focus() => Inner.Focus();

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (Inner != null)
            Inner.Bounds = new Rectangle(ClassicTheme.S(3), ClassicTheme.S(3),
                Math.Max(0, Width - ClassicTheme.S(6)), Math.Max(0, Height - ClassicTheme.S(6)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        ClassicTheme.DrawSunkenBorder(g, ClientRectangle);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Inner.Focus();
    }
}
