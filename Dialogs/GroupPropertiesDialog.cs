using ProgramManager31.Controls;
using ProgramManager31.Models;

namespace ProgramManager31.Dialogs;

internal sealed class GroupPropertiesDialog : ClassicDialog
{
    private readonly ClassicTextBox _description;
    private readonly ClassicTextBox _groupFile;

    public string GroupNameValue => _description.Text.Trim();
    public string GroupFileValue => _groupFile.Text.Trim();

    public GroupPropertiesDialog(ProgramGroup? group, string? defaultFileName = null)
    {
        Text = group == null ? "Program Group Properties" : "Program Group Properties";
        SetContentSize(ClassicTheme.S(410), ClassicTheme.S(100));

        var descriptionLabel = new ClassicLabel
        {
            Text = "&Description:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(24), ClassicTheme.S(92), ClassicTheme.S(16))
        };
        _description = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(106), ClassicTheme.S(22), ClassicTheme.S(198), ClassicTheme.S(21)),
            Text = group?.Name ?? string.Empty,
            TabIndex = 0
        };

        var fileLabel = new ClassicLabel
        {
            Text = "&Group File:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(56), ClassicTheme.S(92), ClassicTheme.S(16))
        };
        descriptionLabel.Target = _description;
        fileLabel.Target = _groupFile;
        _groupFile = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(106), ClassicTheme.S(54), ClassicTheme.S(198), ClassicTheme.S(21)),
            Text = group?.FileName ?? defaultFileName ?? "GROUP.GRP",
            TabIndex = 1
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(328), ClassicTheme.S(8), ClassicTheme.S(70), ClassicTheme.ButtonHeight),
            TabIndex = 2
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(328), ClassicTheme.S(32), ClassicTheme.S(70), ClassicTheme.ButtonHeight),
            TabIndex = 3
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(328), ClassicTheme.S(56), ClassicTheme.S(70), ClassicTheme.ButtonHeight),
            TabIndex = 4
        };

        ok.Click += (_, _) =>
        {
            if (GroupNameValue.Length == 0)
            {
                ClassicMessageBox.ShowWarning(this, "You must provide a description for the program group.", Text);
                _description.Focus();
                return;
            }
            if (GroupFileValue.Length == 0)
            {
                ClassicMessageBox.ShowWarning(this, "You must provide a group file name.", Text);
                _groupFile.Focus();
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
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            "A program group stores a collection of program items. The group file is the file on disk that holds the group's contents.",
            Text);

        Body.Controls.Add(descriptionLabel);
        Body.Controls.Add(_description);
        Body.Controls.Add(fileLabel);
        Body.Controls.Add(_groupFile);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _description.Focus();
        _description.SelectAll();
    }
}
