namespace ProgramManager31.Controls;

internal sealed class ClassicLabel : Control
{
    private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;
    private bool _wordWrap;

    public ClassicLabel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint, true);
        Font = ClassicTheme.UI;
        ForeColor = ClassicTheme.Black;
        BackColor = ClassicTheme.Face;
        TabStop = false;
        Height = ClassicTheme.S(16);
    }

    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set { _textAlign = value; Invalidate(); }
    }

    public bool WordWrap
    {
        get => _wordWrap;
        set { _wordWrap = value; Invalidate(); }
    }

    public bool UseMnemonic { get; set; } = true;
    public Control? Target { get; set; }

    public char Mnemonic
    {
        get
        {
            var (clean, index) = ClassicTheme.StripMnemonic(Text);
            return index >= 0 && index < clean.Length ? clean[index] : '\0';
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var back = new SolidBrush(BackColor))
            g.FillRectangle(back, ClientRectangle);

        var flags = TextFormatFlags.SingleLine;
        if (_wordWrap)
            flags = TextFormatFlags.WordBreak;

        flags |= _textAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            _ => TextFormatFlags.Right
        };
        flags |= _textAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter,
            _ => TextFormatFlags.Bottom
        };

        if (UseMnemonic)
        {
            ClassicTheme.DrawMnemonicText(g, Text, Font, ForeColor, ClientRectangle, flags);
        }
        else
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, flags | TextFormatFlags.NoPrefix);
        }
    }
}
