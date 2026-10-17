using ProgramManager31.Controls;

namespace ProgramManager31.Dialogs;

internal sealed class HelpWindow : ClassicDialog
{
    public HelpWindow()
    {
        Text = "Program Manager Help";
        SetContentSize(ClassicTheme.S(520), ClassicTheme.S(330));

        var help = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(12), ClassicTheme.S(496), ClassicTheme.S(270)),
            Multiline = true,
            ReadOnly = true,
            TabIndex = 0,
            Text =
                "Program Manager\r\n\r\n" +
                "Program Manager organizes programs into groups. Each group is a child window that contains program item icons.\r\n\r\n" +
                "Opening programs\r\n" +
                "  Double-click a program item, or select it and press Enter.\r\n" +
                "  Use File > Run to start a program by name.\r\n\r\n" +
                "Working with groups and items\r\n" +
                "  File > New creates a new group or program item.\r\n" +
                "  Drag a program item onto another group to move it.\r\n" +
                "  Hold Ctrl while dragging to copy the item instead.\r\n" +
                "  A group's control menu can minimize, maximize, or close the group.\r\n" +
                "  Window > Cascade, Tile, and Arrange Icons arrange the desktop.\r\n\r\n" +
                "Keyboard\r\n" +
                "  F1 Help, F7 Move, F8 Copy, Del Delete, Alt+Enter Properties\r\n" +
                "  Ctrl+F6 Next group, Ctrl+F4 Minimize group, Alt+F4 Exit\r\n" +
                "  Ctrl+Alt+letter runs a program item that defines that shortcut.\r\n\r\n" +
                "Settings\r\n" +
                "  Options > Auto Arrange keeps icons in columns.\r\n" +
                "  Options > Minimize on Use minimizes Program Manager when a program starts.\r\n" +
                "  Options > Save Settings on Exit stores the desktop layout.\r\n\r\n" +
                "The group files and PROGMAN.INI are stored in your local application data folder."
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(430), ClassicTheme.S(292), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 1
        };
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        Body.Controls.Add(help);
        Body.Controls.Add(ok);
        DefaultButton = ok;
        CancelButton = ok;
    }
}
