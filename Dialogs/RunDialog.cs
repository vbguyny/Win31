using ProgramManager31.Controls;

namespace ProgramManager31.Dialogs;

internal sealed class RunDialog : ClassicDialog
{
    private readonly ClassicTextBox _command;
    private readonly ClassicCheckBox _minimized;

    public string CommandLine => _command.Text.Trim();
    public bool RunMinimized => _minimized.Checked;

    public RunDialog(string initialCommand = "")
    {
        Text = "Run";
        SetContentSize(ClassicTheme.S(430), ClassicTheme.S(112));

        var label = new ClassicLabel
        {
            Text = "&Command Line:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(22), ClassicTheme.S(96), ClassicTheme.S(16))
        };
        _command = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(20), ClassicTheme.S(200), ClassicTheme.S(21)),
            Text = initialCommand,
            TabIndex = 0
        };
        label.Target = _command;
        _minimized = new ClassicCheckBox
        {
            Text = "Run &Minimized",
            Bounds = new Rectangle(ClassicTheme.S(110), ClassicTheme.S(50), ClassicTheme.S(170), ClassicTheme.S(16)),
            TabIndex = 1
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(344), ClassicTheme.S(8), ClassicTheme.S(74), ClassicTheme.ButtonHeight),
            TabIndex = 2
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(344), ClassicTheme.S(32), ClassicTheme.S(74), ClassicTheme.ButtonHeight),
            TabIndex = 3
        };
        var browse = new ClassicButton
        {
            Text = "&Browse...",
            Bounds = new Rectangle(ClassicTheme.S(344), ClassicTheme.S(56), ClassicTheme.S(74), ClassicTheme.ButtonHeight),
            TabIndex = 4
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(344), ClassicTheme.S(80), ClassicTheme.S(74), ClassicTheme.ButtonHeight),
            TabIndex = 5
        };

        ok.Click += (_, _) =>
        {
            if (CommandLine.Length == 0)
            {
                ClassicMessageBox.ShowWarning(this, "Type the name of a program, folder, document, or Internet resource.", Text);
                _command.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        browse.Click += (_, _) =>
        {
            using var dialog = new ClassicFileDialog("Browse",
                "Programs (*.exe;*.com;*.bat;*.cmd)|*.exe;*.com;*.bat;*.cmd|All files (*.*)|*.*");
            if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                _command.Text = dialog.SelectedPath.Contains(' ') ? $"\"{dialog.SelectedPath}\"" : dialog.SelectedPath;
        };
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            "Type a program name, a document name, or a command. The program is started with the Windows shell.",
            Text);

        Body.Controls.Add(label);
        Body.Controls.Add(_command);
        Body.Controls.Add(_minimized);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(browse);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _command.Focus();
    }
}
