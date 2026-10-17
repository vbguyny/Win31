using ProgramManager31.Controls;
using ProgramManager31.Icons;
using ProgramManager31.Models;

namespace ProgramManager31.Dialogs;

internal sealed class ProgramItemPropertiesDialog : ClassicDialog
{
    private readonly ClassicTextBox _name;
    private readonly ClassicTextBox _command;
    private readonly ClassicTextBox _directory;
    private readonly ShortcutKeyBox _shortcut;
    private readonly ClassicCheckBox _runMinimized;
    private readonly PictureBox _iconPreview;
    private readonly List<string> _reservedShortcuts;
    private string _iconKey;

    public string ItemName => _name.Text.Trim();
    public string CommandLine => _command.Text.Trim();
    public string WorkingDirectory => _directory.Text.Trim();
    public string ShortcutKey => _shortcut.ShortcutValue;
    public bool RunMinimized => _runMinimized.Checked;
    public string IconKey => _iconKey;

    public ProgramItemPropertiesDialog(ProgramItem? item, string groupName, IEnumerable<string> reservedShortcuts)
    {
        Text = "Program Item Properties";
        SetContentSize(ClassicTheme.S(445), ClassicTheme.S(218));
        _reservedShortcuts = reservedShortcuts.ToList();
        _iconKey = item?.IconKey ?? "application";

        var nameLabel = new ClassicLabel
        {
            Text = "&Description:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(22), ClassicTheme.S(94), ClassicTheme.S(16))
        };
        _name = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(20), ClassicTheme.S(202), ClassicTheme.S(21)),
            Text = item?.Name ?? string.Empty,
            TabIndex = 0
        };

        var commandLabel = new ClassicLabel
        {
            Text = "&Command Line:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(48), ClassicTheme.S(94), ClassicTheme.S(16))
        };
        _command = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(46), ClassicTheme.S(202), ClassicTheme.S(21)),
            Text = item?.CommandLine ?? string.Empty,
            TabIndex = 1
        };

        var directoryLabel = new ClassicLabel
        {
            Text = "&Working Directory:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(74), ClassicTheme.S(94), ClassicTheme.S(16))
        };
        _directory = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(72), ClassicTheme.S(202), ClassicTheme.S(21)),
            Text = item?.WorkingDirectory ?? string.Empty,
            TabIndex = 2
        };

        var shortcutLabel = new ClassicLabel
        {
            Text = "&Shortcut Key:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(100), ClassicTheme.S(94), ClassicTheme.S(16))
        };
        _shortcut = new ShortcutKeyBox
        {
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(98), ClassicTheme.S(202), ClassicTheme.S(21)),
            TabIndex = 3
        };
        _shortcut.ShortcutValue = item?.ShortcutKey ?? string.Empty;
        nameLabel.Target = _name;
        commandLabel.Target = _command;
        directoryLabel.Target = _directory;
        shortcutLabel.Target = _shortcut;

        _iconPreview = new PictureBox
        {
            Bounds = new Rectangle(ClassicTheme.S(14), ClassicTheme.S(132), ClassicTheme.S(40), ClassicTheme.S(40)),
            BackColor = ClassicTheme.Face
        };
        _iconPreview.Paint += (_, e) =>
        {
            ClassicTheme.DrawSunkenBorder(e.Graphics, _iconPreview.ClientRectangle);
            IconPainter.DrawIcon(e.Graphics, _iconKey,
                new Rectangle(ClassicTheme.S(4), ClassicTheme.S(4), ClassicTheme.IconSize, ClassicTheme.IconSize));
        };

        var groupLabel = new ClassicLabel
        {
            Text = $"Group: {groupName}",
            Bounds = new Rectangle(ClassicTheme.S(66), ClassicTheme.S(138), ClassicTheme.S(180), ClassicTheme.S(16)),
            ForeColor = ClassicTheme.Dark
        };

        _runMinimized = new ClassicCheckBox
        {
            Text = "Run &Minimized",
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(136), ClassicTheme.S(150), ClassicTheme.S(16)),
            Checked = item?.RunMinimized ?? false,
            TabIndex = 4
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(354), ClassicTheme.S(8), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 5
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(354), ClassicTheme.S(32), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 6
        };
        var browse = new ClassicButton
        {
            Text = "&Browse...",
            Bounds = new Rectangle(ClassicTheme.S(354), ClassicTheme.S(56), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 7
        };
        var changeIcon = new ClassicButton
        {
            Text = "Change &Icon...",
            Bounds = new Rectangle(ClassicTheme.S(354), ClassicTheme.S(80), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 8
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(354), ClassicTheme.S(104), ClassicTheme.S(78), ClassicTheme.ButtonHeight),
            TabIndex = 9
        };

        ok.Click += (_, _) => Accept();
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        browse.Click += (_, _) => Browse();
        changeIcon.Click += (_, _) => ChangeIcon();
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            "A program item starts a program. The command line can include arguments. " +
            "The shortcut key is Ctrl+Alt plus a letter or digit and works while Program Manager is running.",
            Text);

        Body.Controls.Add(nameLabel);
        Body.Controls.Add(_name);
        Body.Controls.Add(commandLabel);
        Body.Controls.Add(_command);
        Body.Controls.Add(directoryLabel);
        Body.Controls.Add(_directory);
        Body.Controls.Add(shortcutLabel);
        Body.Controls.Add(_shortcut);
        Body.Controls.Add(groupLabel);
        Body.Controls.Add(_iconPreview);
        Body.Controls.Add(_runMinimized);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(browse);
        Body.Controls.Add(changeIcon);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;

        _command.Inner.TextChanged += (_, _) => UpdateIconFromCommand();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _name.Focus();
        _name.SelectAll();
    }

    private void Accept()
    {
        if (ItemName.Length == 0)
        {
            ClassicMessageBox.ShowWarning(this, "You must provide a description for the program item.", Text);
            _name.Focus();
            return;
        }
        if (CommandLine.Length == 0)
        {
            ClassicMessageBox.ShowWarning(this, "You must provide a command line for the program item.", Text);
            _command.Focus();
            return;
        }

        string shortcut = ShortcutKey;
        if (!string.IsNullOrEmpty(shortcut) && _reservedShortcuts.Any(k =>
                string.Equals(k, shortcut, StringComparison.OrdinalIgnoreCase)))
        {
            ClassicMessageBox.ShowWarning(this,
                $"The shortcut key {shortcut} is already used by another program item.", Text);
            _shortcut.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void Browse()
    {
        using var dialog = new ClassicFileDialog("Browse",
            "Programs (*.exe;*.com;*.bat;*.cmd)|*.exe;*.com;*.bat;*.cmd|All files (*.*)|*.*");
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(dialog.SelectedPath))
            return;

        _command.Text = QuoteIfNeeded(dialog.SelectedPath);
        if (string.IsNullOrWhiteSpace(_directory.Text))
            _directory.Text = Path.GetDirectoryName(dialog.SelectedPath) ?? string.Empty;
        UpdateIconFromCommand();
    }

    private void ChangeIcon()
    {
        using var dialog = new ChangeIconDialog(_iconKey);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _iconKey = dialog.SelectedIconKey;
            _iconPreview.Invalidate();
        }
    }

    private void UpdateIconFromCommand()
    {
        if (_iconKey is not ("application" or "dos"))
            return;

        string command = _command.Text.Trim().Trim('"');
        string name = Path.GetFileNameWithoutExtension(command).ToLowerInvariant();
        _iconKey = name switch
        {
            "notepad" or "write" => "write",
            "mspaint" or "paint" or "pbrush" => "paint",
            "calc" or "calculator" => "calculator",
            "clock" => "clock",
            "charmap" => "charmap",
            "explorer" or "winfile" => "filemanager",
            "control" => "controlpanel",
            "cmd" or "command" or "dos" => "dos",
            _ => _iconKey
        };
        _iconPreview.Invalidate();
    }

    private static string QuoteIfNeeded(string value)
        => value.Contains(' ') && !value.StartsWith('"') ? $"\"{value}\"" : value;
}
