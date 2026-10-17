using ProgramManager31.Controls;

namespace ProgramManager31.BuiltIns;

internal sealed class ClipboardViewerWindow : ClassicForm
{
    private readonly ClassicMenuBar _menuBar;
    private readonly ClassicTextBox _text;

    public ClipboardViewerWindow()
    {
        Text = "Clipboard Viewer";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(ClassicTheme.S(460), ClassicTheme.S(330));
        MinimumSize = new Size(ClassicTheme.S(280), ClassicTheme.S(180));
        Resizable = true;
        ShowMinimize = true;
        ShowMaximize = true;

        _text = new ClassicTextBox
        {
            Multiline = true,
            ReadOnly = true
        };
        _menuBar = new ClassicMenuBar { ItemsProvider = BuildMenu };
        _menuBar.SetTopLevelItems("&File", "&View", "&Help");

        Content.Controls.Add(_text);
        Content.Controls.Add(_menuBar);
        _menuBar.BringToFront();
        Content.Resize += (_, _) => LayoutViewer();
        LayoutViewer();
        RefreshClipboard();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        RefreshClipboard();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            RefreshClipboard();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void LayoutViewer()
    {
        int menuHeight = ClassicTheme.MenuBar;
        int margin = ClassicTheme.S(6);
        _menuBar.Bounds = new Rectangle(0, 0, Content.ClientSize.Width, menuHeight);
        _text.Bounds = new Rectangle(margin, menuHeight + margin,
            Math.Max(0, Content.ClientSize.Width - margin * 2),
            Math.Max(0, Content.ClientSize.Height - menuHeight - margin * 2));
    }

    private IReadOnlyList<MenuEntry> BuildMenu(int index)
    {
        if (index == 0)
        {
            return new[]
            {
                MenuEntry.Item("&Refresh", RefreshClipboard, "F5"),
                MenuEntry.Item("&Close", Close),
                MenuEntry.Separator(),
                MenuEntry.Item("E&xit", Close, "Alt+F4")
            };
        }
        if (index == 1)
        {
            return new[]
            {
                MenuEntry.Item("&Refresh", RefreshClipboard, "F5")
            };
        }
        return new[]
        {
            MenuEntry.Item("&About Clipboard Viewer", () => ClassicMessageBox.ShowInfo(this,
                "Clipboard Viewer\n\nDisplays text currently stored on the Windows Clipboard.\n" +
                "This recreation uses the modern .NET clipboard API.", "About Clipboard Viewer"))
        };
    }

    private void RefreshClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                _text.Text = string.IsNullOrEmpty(text)
                    ? "[The Clipboard does not contain any text.]"
                    : text;
            }
            else
            {
                _text.Text = "[The Clipboard is empty or does not contain text.]";
            }
        }
        catch (Exception ex)
        {
            _text.Text = $"[The Clipboard could not be read: {ex.Message}]";
        }
    }
}
