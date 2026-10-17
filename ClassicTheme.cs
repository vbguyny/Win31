using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ProgramManager31;

/// <summary>
/// Colours, fonts and small drawing primitives for the Windows 3.1 look.
/// </summary>
internal static class ClassicTheme
{
    public const int BaseFrame = 4;
    public const int BaseTitleBar = 18;
    public const int BaseMenuBar = 18;
    public const int BaseSystemBox = 16;
    public const int BaseTitleButton = 18;
    public const int BaseScrollBar = 16;
    public const int BaseButtonHeight = 21;
    public const int BaseIcon = 32;
    public const int BaseCellWidth = 92;
    public const int BaseCellHeight = 56;
    public const int BaseLabelHeight = 15;
    public const int BaseListItem = 16;

    public static float DpiScale { get; private set; } = 1f;

    public static int S(int value) => (int)Math.Round(value * DpiScale);
    public static int Frame => S(BaseFrame);
    public static int TitleBar => S(BaseTitleBar);
    public static int MenuBar => S(BaseMenuBar);
    public static int SystemBox => S(BaseSystemBox);
    public static int TitleButton => S(BaseTitleButton);
    public static int ScrollBar => S(BaseScrollBar);
    public static int ButtonHeight => S(BaseButtonHeight);
    public static int IconSize => S(BaseIcon);
    public static int CellWidth => S(BaseCellWidth);
    public static int CellHeight => S(BaseCellHeight);
    public static int LabelHeight => S(BaseLabelHeight);
    public static int ListItemHeight => S(BaseListItem);

    public static readonly Color Face = Color.FromArgb(0xC0, 0xC0, 0xC0);
    public static readonly Color Light = Color.FromArgb(0xFF, 0xFF, 0xFF);
    public static readonly Color Dark = Color.FromArgb(0x80, 0x80, 0x80);
    public static readonly Color Black = Color.Black;
    public static readonly Color ActiveTitle = Color.FromArgb(0x00, 0x00, 0x80);
    public static readonly Color ActiveTitleText = Color.White;
    public static readonly Color InactiveTitle = Color.White;
    public static readonly Color InactiveTitleText = Color.Black;
    public static readonly Color MenuBackground = Color.White;
    public static readonly Color Highlight = Color.FromArgb(0x00, 0x00, 0x80);
    public static readonly Color HighlightText = Color.White;
    public static readonly Color DisabledText = Color.FromArgb(0x80, 0x80, 0x80);
    public static readonly Color Desktop = Color.FromArgb(0xC0, 0xC0, 0xC0);
    public static readonly Color Window = Color.White;

    public static Font UI { get; private set; } = SystemFonts.DefaultFont;
    public static Font UIBold { get; private set; } = SystemFonts.DefaultFont;
    public static Font Title { get; private set; } = SystemFonts.DefaultFont;
    public static Font Small { get; private set; } = SystemFonts.DefaultFont;

    public static void Initialize()
    {
        using (var screen = Graphics.FromHwnd(IntPtr.Zero))
        {
            DpiScale = Math.Max(1f, screen.DpiX / 96f);
        }

        var family = PickFamily("Microsoft Sans Serif", "MS Sans Serif", "Tahoma", "Arial");
        UI = CreateFont(family, 8.25f, FontStyle.Regular);
        UIBold = CreateFont(family, 8.25f, FontStyle.Bold);
        Title = CreateFont(family, 8.25f, FontStyle.Bold);
        Small = CreateFont(family, 7.5f, FontStyle.Regular);
    }

    private static string PickFamily(params string[] names)
    {
        try
        {
            using var installed = new InstalledFontCollection();
            var available = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                if (available.Contains(name))
                    return name;
            }
        }
        catch
        {
            // Fall through to a known font.
        }
        return "Tahoma";
    }

    private static Font CreateFont(string family, float size, FontStyle style)
    {
        try
        {
            return new Font(family, size, style, GraphicsUnit.Point);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }
    }

    public static void DrawRaisedBorder(Graphics g, Rectangle rect)
    {
        if (rect.Width < 2 || rect.Height < 2)
            return;

        using var black = new Pen(Black);
        using var light = new Pen(Light);
        using var dark = new Pen(Dark);

        g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        g.DrawLine(light, rect.Left + 1, rect.Top + 1, rect.Right - 2, rect.Top + 1);
        g.DrawLine(light, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 2);
        g.DrawLine(dark, rect.Left + 1, rect.Bottom - 2, rect.Right - 2, rect.Bottom - 2);
        g.DrawLine(dark, rect.Right - 2, rect.Top + 1, rect.Right - 2, rect.Bottom - 2);
    }

    public static void DrawSunkenBorder(Graphics g, Rectangle rect)
    {
        if (rect.Width < 2 || rect.Height < 2)
            return;

        using var black = new Pen(Black);
        using var light = new Pen(Light);
        using var dark = new Pen(Dark);

        g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Right - 2, rect.Top + 1);
        g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 2);
        g.DrawLine(light, rect.Left + 1, rect.Bottom - 2, rect.Right - 2, rect.Bottom - 2);
        g.DrawLine(light, rect.Right - 2, rect.Top + 1, rect.Right - 2, rect.Bottom - 2);
    }

    public static void DrawThinSunkenBorder(Graphics g, Rectangle rect)
    {
        using var black = new Pen(Black);
        using var light = new Pen(Light);
        using var dark = new Pen(Dark);

        g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Right - 2, rect.Top + 1);
        g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 2);
        g.DrawLine(light, rect.Left + 1, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
        g.DrawLine(light, rect.Right - 1, rect.Top + 1, rect.Right - 1, rect.Bottom - 1);
    }

    public static void DrawWindowFrame(Graphics g, Rectangle rect)
    {
        using var black = new Pen(Black);
        using var face = new SolidBrush(Face);

        g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        g.FillRectangle(face, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
        g.DrawRectangle(black, rect.X + Frame - 1, rect.Y + Frame - 1, rect.Width - Frame * 2 + 1, rect.Height - Frame * 2 + 1);
    }

    public static void DrawSystemBox(Graphics g, Rectangle rect, bool pressed = false)
    {
        using var black = new Pen(Black);
        using var light = new Pen(Light);
        using var dark = new Pen(Dark);
        using var white = new SolidBrush(Light);
        using var blackBrush = new SolidBrush(Black);

        g.FillRectangle(white, rect);
        g.DrawRectangle(black, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        if (pressed)
        {
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Right - 1, rect.Top + 1);
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Left + 1, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Right - 1, rect.Top + 1, rect.Right - 1, rect.Bottom - 1);
        }

        int inset = S(3);
        int barY = rect.Top + rect.Height / 2 - S(1);
        g.FillRectangle(blackBrush, rect.Left + inset, barY, rect.Width - inset * 2, S(2));
    }

    public static void DrawTitleButton(Graphics g, Rectangle rect, bool pressed, TitleButtonGlyph glyph)
    {
        using var face = new SolidBrush(Face);
        g.FillRectangle(face, rect);
        if (pressed)
        {
            using var dark = new Pen(Dark);
            using var light = new Pen(Light);
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Right - 1, rect.Top + 1);
            g.DrawLine(dark, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Left + 1, rect.Bottom - 1, rect.Right - 1, rect.Bottom - 1);
            g.DrawLine(light, rect.Right - 1, rect.Top + 1, rect.Right - 1, rect.Bottom - 1);
        }
        else
        {
            using var light = new Pen(Light);
            using var dark = new Pen(Dark);
            g.DrawLine(light, rect.Left + 1, rect.Top + 1, rect.Right - 2, rect.Top + 1);
            g.DrawLine(light, rect.Left + 1, rect.Top + 1, rect.Left + 1, rect.Bottom - 2);
            g.DrawLine(dark, rect.Left + 1, rect.Bottom - 2, rect.Right - 2, rect.Bottom - 2);
            g.DrawLine(dark, rect.Right - 2, rect.Top + 1, rect.Right - 2, rect.Bottom - 2);
        }

        var arrow = Rectangle.Inflate(rect, -S(4), -S(4));
        switch (glyph)
        {
            case TitleButtonGlyph.Minimize:
                DrawTriangle(g, arrow, false);
                break;
            case TitleButtonGlyph.Maximize:
                DrawTriangle(g, arrow, true);
                break;
            case TitleButtonGlyph.Restore:
                DrawRestoreGlyph(g, arrow);
                break;
        }
    }

    public static void DrawTriangle(Graphics g, Rectangle rect, bool up)
    {
        using var brush = new SolidBrush(Black);
        int w = Math.Max(S(5), rect.Width - S(2));
        int h = Math.Max(S(3), rect.Height / 2);
        int cx = rect.Left + rect.Width / 2;
        int cy = rect.Top + rect.Height / 2;
        Point[] points = up
            ? new[]
            {
                new Point(cx - w / 2, cy + h / 2),
                new Point(cx + w / 2, cy + h / 2),
                new Point(cx, cy - h / 2)
            }
            : new[]
            {
                new Point(cx - w / 2, cy - h / 2),
                new Point(cx + w / 2, cy - h / 2),
                new Point(cx, cy + h / 2)
            };
        g.FillPolygon(brush, points);
    }

    private static void DrawRestoreGlyph(Graphics g, Rectangle rect)
    {
        using var black = new Pen(Black);
        int size = Math.Min(rect.Width, rect.Height) - S(2);
        var back = new Rectangle(rect.Left + (rect.Width - size) / 2, rect.Top + (rect.Height - size) / 2, size, size);
        g.DrawRectangle(black, back);
        g.DrawLine(black, back.Right - S(5), back.Top, back.Right - S(5), back.Bottom - S(5));
        g.DrawLine(black, back.Right - S(5), back.Bottom - S(5), back.Left, back.Bottom - S(5));
        g.FillRectangle(new SolidBrush(Face), back.Left + S(3), back.Top + S(3), back.Width - S(4), back.Height - S(4));
        g.DrawRectangle(black, back.Left + S(2), back.Top + S(2), back.Width - S(3), back.Height - S(3));
    }

    public static void DrawCheckMark(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Black, Math.Max(1, S(1)));
        int x = rect.Left;
        int y = rect.Top;
        var points = new[]
        {
            new Point(x + S(2), y + S(6)),
            new Point(x + S(5), y + S(9)),
            new Point(x + S(11), y + S(2))
        };
        g.DrawLines(pen, points);
    }

    public static void DrawRadioDot(Graphics g, Rectangle rect)
    {
        using var brush = new SolidBrush(Black);
        int d = Math.Max(2, S(4));
        g.FillEllipse(brush, rect.Left + (rect.Width - d) / 2, rect.Top + (rect.Height - d) / 2, d, d);
    }

    public static void DrawDitherTrack(Graphics g, Rectangle rect)
    {
        using var light = new SolidBrush(Light);
        using var face = new SolidBrush(Face);
        g.FillRectangle(light, rect);
        for (int y = rect.Top; y < rect.Bottom; y += S(2))
        {
            for (int x = rect.Left + (((y - rect.Top) / Math.Max(1, S(2))) % 2) * S(2); x < rect.Right; x += S(4))
            {
                g.FillRectangle(face, x, y, Math.Min(S(2), rect.Right - x), Math.Min(S(2), rect.Bottom - y));
            }
        }
    }

    public static void DrawMnemonicText(Graphics g, string text, Font font, Color color, Rectangle bounds, TextFormatFlags flags)
    {
        var (clean, mnemonicIndex) = StripMnemonic(text);
        TextRenderer.DrawText(g, clean, font, bounds, color, flags | TextFormatFlags.NoPrefix);

        if (mnemonicIndex < 0 || mnemonicIndex >= clean.Length)
            return;

        var textSize = TextRenderer.MeasureText(g, clean, font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        int textTop = bounds.Top + (bounds.Height - textSize.Height) / 2;
        if ((flags & TextFormatFlags.Bottom) != 0)
            textTop = bounds.Bottom - textSize.Height - S(2);
        else if ((flags & TextFormatFlags.VerticalCenter) == 0)
            textTop = bounds.Top;

        int prefixWidth = TextRenderer.MeasureText(g, clean[..mnemonicIndex], font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
        int charWidth = TextRenderer.MeasureText(g, clean[mnemonicIndex].ToString(), font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;

        int x = bounds.Left;
        if ((flags & TextFormatFlags.HorizontalCenter) != 0)
            x = bounds.Left + (bounds.Width - textSize.Width) / 2;
        else if ((flags & TextFormatFlags.Right) != 0)
            x = bounds.Right - textSize.Width;

        int underlineY = textTop + textSize.Height - S(2);
        using var pen = new Pen(color);
        g.DrawLine(pen, x + prefixWidth, underlineY, x + prefixWidth + Math.Max(1, charWidth - 1), underlineY);
    }

    public static (string Text, int MnemonicIndex) StripMnemonic(string text)
    {
        if (string.IsNullOrEmpty(text))
            return (string.Empty, -1);

        var builder = new System.Text.StringBuilder(text.Length);
        int mnemonic = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '&' && i + 1 < text.Length)
            {
                if (text[i + 1] == '&')
                {
                    builder.Append('&');
                    i++;
                }
                else
                {
                    if (mnemonic < 0)
                        mnemonic = builder.Length;
                    builder.Append(text[i + 1]);
                    i++;
                }
            }
            else
            {
                builder.Append(text[i]);
            }
        }
        return (builder.ToString(), mnemonic);
    }

    public static Size MeasureMnemonicText(Graphics g, string text, Font font, TextFormatFlags flags)
    {
        var (clean, _) = StripMnemonic(text);
        return TextRenderer.MeasureText(g, clean, font, Size.Empty, flags | TextFormatFlags.NoPrefix);
    }

    public static void DrawFocusRect(Graphics g, Rectangle rect)
    {
        ControlPaint.DrawFocusRectangle(g, rect);
    }

    public static Bitmap CreateCheckerTexture()
    {
        var bmp = new Bitmap(S(2), S(2));
        using var g = Graphics.FromImage(bmp);
        g.Clear(Light);
        using var brush = new SolidBrush(Face);
        g.FillRectangle(brush, 0, 0, S(1), S(1));
        g.FillRectangle(brush, S(1), S(1), S(1), S(1));
        return bmp;
    }

    public static void FillDither(Graphics g, Rectangle rect)
    {
        using var texture = new TextureBrush(CreateCheckerTexture());
        g.FillRectangle(texture, rect);
    }

    public static void DrawBeveledPanel(Graphics g, Rectangle rect, bool raised = true)
    {
        if (raised)
            DrawRaisedBorder(g, rect);
        else
            DrawSunkenBorder(g, rect);
    }
}

internal enum TitleButtonGlyph
{
    Minimize,
    Maximize,
    Restore
}
