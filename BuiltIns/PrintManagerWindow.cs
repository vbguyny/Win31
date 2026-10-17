using ProgramManager31.Controls;
using ProgramManager31.Icons;

namespace ProgramManager31.BuiltIns;

internal sealed class PrintManagerWindow : ClassicForm
{
    private readonly ClassicMenuBar _menuBar;
    private readonly PrintQueueView _view;

    public PrintManagerWindow()
    {
        Text = "Print Manager";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(ClassicTheme.S(500), ClassicTheme.S(300));
        MinimumSize = new Size(ClassicTheme.S(300), ClassicTheme.S(200));
        Resizable = true;
        ShowMinimize = true;
        ShowMaximize = true;

        _view = new PrintQueueView();
        _menuBar = new ClassicMenuBar { ItemsProvider = BuildMenu };
        _menuBar.SetTopLevelItems("&View", "&Help");

        Content.Controls.Add(_view);
        Content.Controls.Add(_menuBar);
        _menuBar.BringToFront();
        Content.Resize += (_, _) => LayoutPrintManager();
        LayoutPrintManager();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            _view.Invalidate();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void LayoutPrintManager()
    {
        int menuHeight = ClassicTheme.MenuBar;
        _menuBar.Bounds = new Rectangle(0, 0, Content.ClientSize.Width, menuHeight);
        _view.Bounds = new Rectangle(0, menuHeight, Content.ClientSize.Width,
            Math.Max(0, Content.ClientSize.Height - menuHeight));
    }

    private IReadOnlyList<MenuEntry> BuildMenu(int index)
    {
        if (index == 0)
        {
            return new[]
            {
                MenuEntry.Item("&Refresh", () => _view.Invalidate(), "F5"),
                MenuEntry.Separator(),
                MenuEntry.Item("&Close", Close),
                MenuEntry.Item("E&xit", Close, "Alt+F4")
            };
        }
        return new[]
        {
            MenuEntry.Item("&About Print Manager", () => ClassicMessageBox.ShowInfo(this,
                "Print Manager\n\nNo print devices are configured for this recreation.\n" +
                "The window shows the classic empty-queue view.", "About Print Manager"))
        };
    }

    private sealed class PrintQueueView : Control
    {
        public PrintQueueView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = ClassicTheme.Window;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(ClassicTheme.Window);
            IconPainter.DrawIcon(g, "printmanager", new Rectangle(ClassicTheme.S(14), ClassicTheme.S(14),
                ClassicTheme.IconSize, ClassicTheme.IconSize));

            var titleRect = new Rectangle(ClassicTheme.S(56), ClassicTheme.S(14),
                Math.Max(0, Width - ClassicTheme.S(72)), ClassicTheme.S(18));
            TextRenderer.DrawText(g, "No documents are waiting to print.", ClassicTheme.UIBold, titleRect,
                ClassicTheme.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            var subtitleRect = new Rectangle(ClassicTheme.S(56), ClassicTheme.S(34),
                Math.Max(0, Width - ClassicTheme.S(72)), ClassicTheme.S(16));
            TextRenderer.DrawText(g, "The default printer is ready.", ClassicTheme.UI, subtitleRect,
                ClassicTheme.Dark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            var list = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(62),
                Math.Max(0, Width - ClassicTheme.S(24)), Math.Max(0, Height - ClassicTheme.S(76)));
            using (var white = new SolidBrush(ClassicTheme.Window))
                g.FillRectangle(white, list);
            ClassicTheme.DrawSunkenBorder(g, list);

            int headerHeight = ClassicTheme.S(20);
            var header = new Rectangle(list.Left + 1, list.Top + 1, list.Width - 2, headerHeight);
            using (var face = new SolidBrush(ClassicTheme.Face))
                g.FillRectangle(face, header);
            using (var black = new Pen(ClassicTheme.Black))
                g.DrawLine(black, header.Left, header.Bottom, header.Right, header.Bottom);

            string[] headers = { "Document", "Status", "Owner", "Progress" };
            int[] widths = { 40, 24, 20, 16 };
            int x = header.Left + ClassicTheme.S(4);
            for (int i = 0; i < headers.Length; i++)
            {
                int w = header.Width * widths[i] / 100;
                TextRenderer.DrawText(g, headers[i], ClassicTheme.UI,
                    new Rectangle(x, header.Top, w - ClassicTheme.S(4), header.Height),
                    ClassicTheme.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                x += w;
            }

            var emptyRect = new Rectangle(list.Left + ClassicTheme.S(8), header.Bottom + ClassicTheme.S(8),
                list.Width - ClassicTheme.S(16), list.Height - headerHeight - ClassicTheme.S(16));
            TextRenderer.DrawText(g, "No documents in the queue.", ClassicTheme.UI, emptyRect,
                ClassicTheme.Dark, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }
}
