using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProgramManager31.Icons;

internal static class IconPainter
{
    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Color Ink = Color.Black;
    private static readonly Color White = Color.White;
    private static readonly Color Face = Color.FromArgb(0xC0, 0xC0, 0xC0);
    private static readonly Color Gray = Color.FromArgb(0x80, 0x80, 0x80);
    private static readonly Color LightGray = Color.FromArgb(0xE0, 0xE0, 0xE0);
    private static readonly Color Navy = Color.FromArgb(0x00, 0x00, 0x80);
    private static readonly Color Blue = Color.FromArgb(0x00, 0x00, 0xFF);
    private static readonly Color Cyan = Color.FromArgb(0x00, 0xFF, 0xFF);
    private static readonly Color Green = Color.FromArgb(0x00, 0x80, 0x00);
    private static readonly Color BrightGreen = Color.FromArgb(0x00, 0xC0, 0x00);
    private static readonly Color Red = Color.FromArgb(0xFF, 0x00, 0x00);
    private static readonly Color Maroon = Color.FromArgb(0x80, 0x00, 0x00);
    private static readonly Color Yellow = Color.FromArgb(0xFF, 0xFF, 0x00);
    private static readonly Color Olive = Color.FromArgb(0x80, 0x80, 0x00);
    private static readonly Color Magenta = Color.FromArgb(0xFF, 0x00, 0xFF);
    private static readonly Color Brown = Color.FromArgb(0x80, 0x40, 0x00);
    private static readonly Color Tan = Color.FromArgb(0xFF, 0xCC, 0x99);

    public static Bitmap GetIcon(string key)
    {
        string normalized = Normalize(key);
        int size = Math.Max(16, ClassicTheme.IconSize);
        string cacheKey = normalized + "@" + size;
        if (Cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.ScaleTransform(size / 32f, size / 32f);
            DrawIconCore(g, normalized);
        }
        Cache[cacheKey] = bitmap;
        return bitmap;
    }

    public static void DrawIcon(Graphics g, string key, Rectangle destination)
    {
        var bitmap = GetIcon(key);
        if (destination.Width == bitmap.Width && destination.Height == bitmap.Height)
        {
            g.DrawImageUnscaled(bitmap, destination.X, destination.Y);
            return;
        }

        var oldInterpolation = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.DrawImage(bitmap, destination);
        g.InterpolationMode = oldInterpolation;
    }

    public static void DrawIcon(Graphics g, string key, int x, int y)
        => DrawIcon(g, key, new Rectangle(x, y, ClassicTheme.IconSize, ClassicTheme.IconSize));

    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "application";
        return key.Trim().ToLowerInvariant();
    }

    public static IReadOnlyList<(string Key, string Label)> AvailableIcons { get; } = new[]
    {
        ("application", "Application"),
        ("dos", "MS-DOS program"),
        ("filemanager", "File Manager"),
        ("controlpanel", "Control Panel"),
        ("printmanager", "Print Manager"),
        ("clipboard", "Clipboard Viewer"),
        ("write", "Write"),
        ("paint", "Paintbrush"),
        ("calculator", "Calculator"),
        ("clock", "Clock"),
        ("cardfile", "Cardfile"),
        ("charmap", "Character Map"),
        ("setup", "Windows Setup"),
        ("notepad", "Notepad"),
        ("folder", "Folder"),
        ("solitaire", "Solitaire"),
        ("minesweeper", "Minesweeper"),
        ("help", "Help")
    };

    public static string LabelFor(string key)
    {
        foreach (var item in AvailableIcons)
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                return item.Label;
        return "Application";
    }

    private static void DrawIconCore(Graphics g, string key)
    {
        switch (key)
        {
            case "group":
                DrawGroup(g);
                break;
            case "dos":
                DrawDos(g);
                break;
            case "filemanager":
                DrawFileManager(g);
                break;
            case "controlpanel":
                DrawControlPanel(g);
                break;
            case "printmanager":
                DrawPrintManager(g);
                break;
            case "clipboard":
                DrawClipboard(g);
                break;
            case "write":
                DrawWrite(g);
                break;
            case "paint":
                DrawPaint(g);
                break;
            case "calculator":
                DrawCalculator(g);
                break;
            case "clock":
                DrawClock(g);
                break;
            case "cardfile":
                DrawCardfile(g);
                break;
            case "charmap":
                DrawCharMap(g);
                break;
            case "setup":
                DrawSetup(g);
                break;
            case "notepad":
                DrawNotepad(g);
                break;
            case "folder":
                DrawFolder(g);
                break;
            case "solitaire":
                DrawSolitaire(g);
                break;
            case "minesweeper":
                DrawMinesweeper(g);
                break;
            case "help":
                DrawHelp(g);
                break;
            case "info":
                DrawInfo(g);
                break;
            case "warning":
                DrawWarning(g);
                break;
            case "error":
                DrawError(g);
                break;
            case "question":
                DrawQuestion(g);
                break;
            default:
                DrawApplication(g);
                break;
        }
    }

    private static void DrawGroup(Graphics g)
    {
        FillRect(g, 3, 4, 27, 25, Ink);
        FillRect(g, 4, 5, 25, 23, Face);
        FillRect(g, 5, 6, 23, 5, Navy);
        FillRect(g, 5, 11, 23, 16, White);
        FrameRect(g, 5, 6, 23, 21, Ink);
        FillRect(g, 7, 8, 2, 1, White);
        FillRect(g, 10, 8, 2, 1, Face);
        FillRect(g, 13, 8, 2, 1, Face);

        var colors = new[] { Yellow, Red, Green, Cyan, Magenta, Blue };
        int index = 0;
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int x = 7 + col * 6;
                int y = 14 + row * 6;
                FillRect(g, x, y, 5, 4, Ink);
                FillRect(g, x + 1, y + 1, 3, 2, colors[index++]);
            }
        }
    }

    private static void DrawApplication(Graphics g)
    {
        FillRect(g, 3, 5, 27, 24, Ink);
        FillRect(g, 4, 6, 25, 22, Face);
        FillRect(g, 5, 7, 23, 5, Navy);
        FillRect(g, 5, 12, 23, 15, White);
        FrameRect(g, 5, 7, 23, 20, Ink);
        FillRect(g, 7, 9, 2, 1, White);
        FillRect(g, 10, 9, 2, 1, Face);
        FillRect(g, 13, 9, 2, 1, Face);
        FillRect(g, 8, 16, 17, 1, Gray);
        FillRect(g, 8, 19, 12, 1, Gray);
        FillRect(g, 8, 22, 15, 1, Gray);
        FrameRect(g, 7, 14, 19, 11, LightGray);
    }

    private static void DrawDos(Graphics g)
    {
        FrameRect(g, 2, 3, 28, 23, Ink);
        FillRect(g, 3, 4, 26, 21, Face);
        FillRect(g, 4, 5, 24, 17, Gray);
        FillRect(g, 5, 6, 22, 15, Ink);
        PixelText(g, "C:\\>", 7, 11, White, 1);
        FillRect(g, 12, 26, 8, 2, Gray);
        FillRect(g, 9, 28, 14, 2, Face);
        FrameRect(g, 9, 28, 14, 2, Ink);
    }

    private static void DrawFileManager(Graphics g)
    {
        FrameRect(g, 5, 3, 22, 27, Ink);
        FillRect(g, 6, 4, 20, 25, Yellow);
        FillRect(g, 7, 5, 18, 4, White);
        FillRect(g, 9, 6, 14, 2, Navy);
        for (int i = 0; i < 3; i++)
        {
            int y = 11 + i * 6;
            FillRect(g, 7, y, 18, 5, LightGray);
            FrameRect(g, 7, y, 18, 5, Ink);
            FillRect(g, 13, y + 2, 6, 1, Ink);
        }
    }

    private static void DrawControlPanel(Graphics g)
    {
        FillRect(g, 3, 4, 27, 25, Ink);
        FillRect(g, 4, 5, 25, 23, Face);
        FillRect(g, 5, 6, 23, 5, Navy);
        FillRect(g, 6, 13, 21, 13, LightGray);
        FrameRect(g, 5, 12, 23, 15, Ink);
        for (int i = 0; i < 3; i++)
        {
            int y = 15 + i * 4;
            FillRect(g, 8, y, 15, 1, Gray);
            var knob = i == 0 ? Red : i == 1 ? Green : Blue;
            FillRect(g, 12 + i * 3, y - 2, 3, 5, Ink);
            FillRect(g, 13 + i * 3, y - 1, 1, 3, knob);
        }
    }

    private static void DrawPrintManager(Graphics g)
    {
        FillRect(g, 8, 4, 16, 12, White);
        FrameRect(g, 8, 4, 16, 12, Ink);
        FillRect(g, 10, 6, 12, 1, Gray);
        FillRect(g, 10, 9, 12, 1, Gray);
        FillRect(g, 4, 14, 24, 13, Face);
        FrameRect(g, 4, 14, 24, 13, Ink);
        FillRect(g, 6, 16, 20, 5, Gray);
        FillRect(g, 8, 18, 16, 1, White);
        FillRect(g, 9, 24, 14, 3, White);
        FrameRect(g, 9, 24, 14, 3, Ink);
        FillRect(g, 7, 15, 2, 1, Green);
    }

    private static void DrawClipboard(Graphics g)
    {
        FillRect(g, 8, 5, 16, 24, Brown);
        FrameRect(g, 8, 5, 16, 24, Ink);
        FillRect(g, 10, 7, 12, 20, White);
        FrameRect(g, 10, 7, 12, 20, Gray);
        FillRect(g, 12, 3, 8, 5, Gray);
        FrameRect(g, 12, 3, 8, 5, Ink);
        FillRect(g, 13, 4, 6, 1, LightGray);
        FillRect(g, 12, 11, 8, 1, Gray);
        FillRect(g, 12, 14, 7, 1, Gray);
        FillRect(g, 12, 17, 8, 1, Gray);
        FillRect(g, 12, 20, 5, 1, Gray);
    }

    private static void DrawWrite(Graphics g)
    {
        FillRect(g, 6, 3, 18, 25, White);
        FrameRect(g, 6, 3, 18, 25, Ink);
        FillRect(g, 18, 3, 6, 5, Face);
        Line(g, 18, 3, 24, 8, Ink);
        FillRect(g, 9, 11, 12, 1, Navy);
        FillRect(g, 9, 14, 10, 1, Navy);
        FillRect(g, 9, 17, 12, 1, Navy);
        FillRect(g, 9, 20, 8, 1, Navy);
        Line(g, 20, 23, 28, 15, Ink);
        Line(g, 21, 24, 29, 16, Blue);
        Line(g, 20, 24, 21, 25, Navy);
    }

    private static void DrawPaint(Graphics g)
    {
        FillOval(g, 4, 7, 24, 19, Tan);
        DrawOval(g, 4, 7, 24, 19, Ink);
        FillOval(g, 9, 16, 6, 5, White);
        DrawOval(g, 9, 16, 6, 5, Ink);
        FillRect(g, 8, 10, 3, 3, Red);
        FillRect(g, 14, 9, 3, 3, Blue);
        FillRect(g, 20, 11, 3, 3, Green);
        FillRect(g, 21, 18, 3, 3, Yellow);
        Line(g, 19, 8, 28, 2, Brown);
        Line(g, 20, 9, 29, 3, Brown);
        Line(g, 18, 8, 27, 1, Ink);
        FillRect(g, 26, 1, 4, 3, Gray);
        FillRect(g, 27, 2, 2, 1, Red);
    }

    private static void DrawCalculator(Graphics g)
    {
        FillRect(g, 6, 3, 20, 27, Face);
        FrameRect(g, 6, 3, 20, 27, Ink);
        FillRect(g, 8, 5, 16, 7, Navy);
        FrameRect(g, 8, 5, 16, 7, Ink);
        FillRect(g, 10, 8, 8, 2, Green);
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int x = 8 + col * 4;
                int y = 15 + row * 4;
                FillRect(g, x, y, 3, 3, LightGray);
                FillRect(g, x, y, 3, 1, White);
                FillRect(g, x, y + 2, 3, 1, Gray);
            }
        }
        FillRect(g, 24, 15, 3, 3, Red);
        FillRect(g, 24, 27, 3, 3, Red);
    }

    private static void DrawClock(Graphics g)
    {
        FillOval(g, 3, 3, 26, 26, Yellow);
        DrawOval(g, 3, 3, 26, 26, Ink);
        FillOval(g, 6, 6, 20, 20, White);
        DrawOval(g, 6, 6, 20, 20, Ink);
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            int x = 16 + (int)Math.Round(Math.Sin(angle) * 8);
            int y = 16 - (int)Math.Round(Math.Cos(angle) * 8);
            FillRect(g, x, y, 1, 1, Ink);
        }
        Line(g, 16, 16, 16, 9, Ink);
        Line(g, 16, 16, 21, 19, Ink);
        Line(g, 16, 16, 12, 20, Red);
        FillRect(g, 15, 15, 3, 3, Ink);
    }

    private static void DrawCardfile(Graphics g)
    {
        FillRect(g, 4, 8, 24, 21, Face);
        FrameRect(g, 4, 8, 24, 21, Ink);
        FillRect(g, 7, 4, 18, 18, White);
        FrameRect(g, 7, 4, 18, 18, Ink);
        FillRect(g, 9, 2, 4, 5, Red);
        FillRect(g, 15, 3, 4, 4, Blue);
        FillRect(g, 21, 2, 3, 5, Green);
        FillRect(g, 9, 10, 14, 1, Gray);
        FillRect(g, 9, 13, 12, 1, Gray);
        FillRect(g, 9, 16, 14, 1, Gray);
        FillRect(g, 6, 26, 20, 3, Gray);
        FrameRect(g, 6, 26, 20, 3, Ink);
    }

    private static void DrawCharMap(Graphics g)
    {
        FillRect(g, 4, 4, 24, 24, White);
        FrameRect(g, 4, 4, 24, 24, Ink);
        for (int i = 0; i < 4; i++)
        {
            FillRect(g, 6 + i * 5, 7, 1, 18, LightGray);
            FillRect(g, 6, 7 + i * 5, 20, 1, LightGray);
        }
        PixelText(g, "A", 8, 8, Red, 1);
        PixelText(g, "B", 14, 14, Blue, 1);
        PixelText(g, "1", 19, 20, Green, 1);
        FillRect(g, 4, 24, 24, 4, Face);
        FrameRect(g, 4, 24, 24, 4, Ink);
        FillRect(g, 7, 25, 16, 1, Gray);
    }

    private static void DrawSetup(Graphics g)
    {
        FillRect(g, 2, 5, 28, 19, Ink);
        FillRect(g, 3, 6, 26, 17, Face);
        FillRect(g, 4, 7, 24, 13, Gray);
        FillRect(g, 5, 8, 22, 11, White);
        FillRect(g, 7, 10, 18, 1, Green);
        FillRect(g, 7, 13, 14, 1, Green);
        FillRect(g, 7, 16, 16, 1, Green);
        FillRect(g, 11, 24, 10, 2, Gray);
        FillRect(g, 7, 26, 18, 3, Face);
        FrameRect(g, 7, 26, 18, 3, Ink);
        FillRect(g, 9, 27, 14, 1, Gray);
    }

    private static void DrawNotepad(Graphics g)
    {
        FillRect(g, 6, 3, 20, 26, White);
        FrameRect(g, 6, 3, 20, 26, Ink);
        for (int i = 0; i < 4; i++)
        {
            FillRect(g, 9, 10 + i * 4, 14, 1, Blue);
            FillRect(g, 8, 8 + i * 4, 2, 2, Gray);
        }
        FillRect(g, 9, 5, 14, 2, Face);
        for (int i = 0; i < 4; i++)
            FillRect(g, 8, 5 + i * 1, 2, 2, Gray);
    }

    private static void DrawFolder(Graphics g)
    {
        FillRect(g, 3, 8, 26, 19, Yellow);
        FrameRect(g, 3, 8, 26, 19, Ink);
        FillRect(g, 5, 5, 10, 5, Yellow);
        FrameRect(g, 5, 5, 10, 5, Ink);
        FillRect(g, 4, 10, 24, 15, Color.FromArgb(0xFF, 0xE0, 0x60));
        Line(g, 4, 10, 27, 10, Ink);
        FillRect(g, 6, 13, 20, 9, LightGray);
        FrameRect(g, 6, 13, 20, 9, Gray);
    }

    private static void DrawSolitaire(Graphics g)
    {
        FillRect(g, 7, 3, 18, 26, White);
        FrameRect(g, 7, 3, 18, 26, Ink);
        FillRect(g, 11, 8, 10, 8, Red);
        FillRect(g, 12, 9, 8, 6, White);
        FillRect(g, 12, 12, 8, 1, Red);
        FillRect(g, 12, 15, 8, 1, Red);
        FillRect(g, 10, 19, 12, 7, White);
        FrameRect(g, 10, 19, 12, 7, Gray);
        FillRect(g, 12, 21, 8, 1, Ink);
        FillRect(g, 12, 23, 6, 1, Ink);
    }

    private static void DrawMinesweeper(Graphics g)
    {
        FillRect(g, 3, 3, 26, 26, Face);
        FrameRect(g, 3, 3, 26, 26, Ink);
        FillRect(g, 4, 4, 24, 2, White);
        FillRect(g, 4, 4, 2, 24, White);
        FillRect(g, 4, 26, 24, 2, Gray);
        FillRect(g, 26, 4, 2, 24, Gray);
        FillOval(g, 10, 11, 12, 11, Ink);
        Line(g, 16, 7, 16, 25, Ink);
        Line(g, 8, 16, 24, 16, Ink);
        Line(g, 10, 10, 22, 22, Ink);
        Line(g, 22, 10, 10, 22, Ink);
        FillRect(g, 14, 14, 4, 4, White);
    }

    private static void DrawHelp(Graphics g)
    {
        FillRect(g, 5, 3, 22, 26, Blue);
        FrameRect(g, 5, 3, 22, 26, Ink);
        FillRect(g, 7, 5, 18, 22, White);
        FrameRect(g, 7, 5, 18, 22, Ink);
        FillRect(g, 7, 5, 3, 22, LightGray);
        PixelText(g, "?", 13, 10, Navy, 2);
        FillRect(g, 10, 8, 12, 1, Ink);
        FillRect(g, 10, 24, 12, 1, Ink);
    }

    private static void DrawInfo(Graphics g)
    {
        FillOval(g, 3, 3, 26, 26, Blue);
        DrawOval(g, 3, 3, 26, 26, Ink);
        FillOval(g, 5, 5, 22, 22, Blue);
        PixelText(g, "i", 14, 10, White, 2);
    }

    private static void DrawWarning(Graphics g)
    {
        using var brush = new SolidBrush(Yellow);
        Point[] points =
        {
            new(16, 2), new(30, 29), new(2, 29)
        };
        g.FillPolygon(brush, points);
        using var pen = new Pen(Ink);
        g.DrawPolygon(pen, points);
        PixelText(g, "!", 14, 9, Ink, 2);
    }

    private static void DrawError(Graphics g)
    {
        FillOval(g, 3, 3, 26, 26, Red);
        DrawOval(g, 3, 3, 26, 26, Ink);
        PixelText(g, "X", 10, 10, White, 2);
    }

    private static void DrawQuestion(Graphics g)
    {
        FillOval(g, 3, 3, 26, 26, Cyan);
        DrawOval(g, 3, 3, 26, 26, Ink);
        PixelText(g, "?", 11, 9, Navy, 2);
    }

    private static void FillRect(Graphics g, int x, int y, int w, int h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, x, y, w, h);
    }

    private static void FrameRect(Graphics g, int x, int y, int w, int h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        using var pen = new Pen(color);
        g.DrawRectangle(pen, x, y, w - 1, h - 1);
    }

    private static void DrawOval(Graphics g, int x, int y, int w, int h, Color color)
    {
        using var pen = new Pen(color);
        g.DrawEllipse(pen, x, y, w - 1, h - 1);
    }

    private static void FillOval(Graphics g, int x, int y, int w, int h, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x, y, w, h);
    }

    private static void Line(Graphics g, int x1, int y1, int x2, int y2, Color color)
    {
        using var pen = new Pen(color);
        g.DrawLine(pen, x1, y1, x2, y2);
    }

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
        ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
        ['C'] = new[] { "01110", "10001", "10000", "10000", "10000", "10001", "01110" },
        ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
        ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
        ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
        ['G'] = new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01110" },
        ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
        ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
        ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
        ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
        ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
        ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
        ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
        ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
        ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
        ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
        ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
        ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" },
        ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
        ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
        ['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
        ['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
        ['2'] = new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
        ['3'] = new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
        ['4'] = new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
        ['5'] = new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" },
        [':'] = new[] { "00000", "00100", "00100", "00000", "00100", "00100", "00000" },
        ['\\'] = new[] { "10000", "10000", "01000", "00100", "00010", "00001", "00001" },
        ['>'] = new[] { "10000", "01000", "00100", "00010", "00100", "01000", "10000" },
        ['!'] = new[] { "00100", "00100", "00100", "00100", "00100", "00000", "00100" },
        ['?'] = new[] { "01110", "10001", "00001", "00010", "00100", "00000", "00100" },
        ['i'] = new[] { "00100", "00000", "01100", "00100", "00100", "00100", "01110" }
    };

    private static void PixelText(Graphics g, string text, int x, int y, Color color, int scale)
    {
        int cursor = x;
        foreach (char ch in text)
        {
            if (Glyphs.TryGetValue(char.ToUpperInvariant(ch), out var rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] == '1')
                            FillRect(g, cursor + col * scale, y + row * scale, scale, scale, color);
                    }
                }
            }
            cursor += 6 * scale;
        }
    }
}
