using ProgramManager31.Controls;

namespace ProgramManager31.Dialogs;

internal enum NewObjectType
{
    ProgramGroup,
    ProgramItem
}

internal sealed class NewObjectDialog : ClassicDialog
{
    private readonly ClassicRadioButton _groupButton;
    private readonly ClassicRadioButton _programButton;

    public NewObjectType SelectedType { get; private set; } = NewObjectType.ProgramItem;

    public NewObjectDialog(bool defaultToProgram)
    {
        Text = "New";
        SetContentSize(ClassicTheme.S(245), ClassicTheme.S(94));

        _groupButton = new ClassicRadioButton
        {
            Text = "Program &Group",
            GroupName = "NewObject",
            Bounds = new Rectangle(ClassicTheme.S(14), ClassicTheme.S(16), ClassicTheme.S(150), ClassicTheme.S(16)),
            Checked = !defaultToProgram,
            TabIndex = 0
        };
        _programButton = new ClassicRadioButton
        {
            Text = "&Program Item",
            GroupName = "NewObject",
            Bounds = new Rectangle(ClassicTheme.S(14), ClassicTheme.S(40), ClassicTheme.S(150), ClassicTheme.S(16)),
            Checked = defaultToProgram,
            TabIndex = 1
        };
        Body.Controls.Add(_groupButton);
        Body.Controls.Add(_programButton);

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(172), ClassicTheme.S(8), ClassicTheme.S(62), ClassicTheme.ButtonHeight),
            TabIndex = 2
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(172), ClassicTheme.S(32), ClassicTheme.S(62), ClassicTheme.ButtonHeight),
            TabIndex = 3
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(172), ClassicTheme.S(56), ClassicTheme.S(62), ClassicTheme.ButtonHeight),
            TabIndex = 4
        };

        ok.Click += (_, _) =>
        {
            SelectedType = _groupButton.Checked ? NewObjectType.ProgramGroup : NewObjectType.ProgramItem;
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        help.Click += (_, _) => ShowHelp();
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _groupButton.Focus();
    }

    private void ShowHelp()
    {
        ClassicMessageBox.ShowInfo(this,
            "Choose Program Group to create a new group, or Program Item to add a program to the open group.",
            "New");
    }
}
