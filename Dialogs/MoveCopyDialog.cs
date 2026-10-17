using ProgramManager31.Controls;
using ProgramManager31.Models;

namespace ProgramManager31.Dialogs;

internal sealed class MoveCopyDialog : ClassicDialog
{
    private readonly ClassicComboBox _targets;

    public ProgramGroup? TargetGroup { get; private set; }

    public MoveCopyDialog(ProgramItem item, ProgramGroup fromGroup, IReadOnlyList<ProgramGroup> groups, bool move)
    {
        Text = move ? "Move Program" : "Copy Program";
        SetContentSize(ClassicTheme.S(390), ClassicTheme.S(110));

        var actionLabel = new ClassicLabel
        {
            Text = move ? "Move program:" : "Copy program:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(20), ClassicTheme.S(92), ClassicTheme.S(16))
        };
        var nameValue = new ClassicLabel
        {
            Text = item.Name,
            Bounds = new Rectangle(ClassicTheme.S(108), ClassicTheme.S(20), ClassicTheme.S(160), ClassicTheme.S(16))
        };
        var fromLabel = new ClassicLabel
        {
            Text = "From group:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(42), ClassicTheme.S(92), ClassicTheme.S(16))
        };
        var fromValue = new ClassicLabel
        {
            Text = fromGroup.Name,
            Bounds = new Rectangle(ClassicTheme.S(108), ClassicTheme.S(42), ClassicTheme.S(160), ClassicTheme.S(16))
        };
        var toLabel = new ClassicLabel
        {
            Text = "&To group:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(68), ClassicTheme.S(92), ClassicTheme.S(16))
        };
        _targets = new ClassicComboBox
        {
            Bounds = new Rectangle(ClassicTheme.S(108), ClassicTheme.S(66), ClassicTheme.S(190), ClassicTheme.S(21)),
            TabIndex = 0
        };
        _targets.SetItems(groups.Select(g => g.Name));
        _targets.SelectedIndex = groups.Count > 0 ? 0 : -1;
        toLabel.Target = _targets;

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(310), ClassicTheme.S(8), ClassicTheme.S(68), ClassicTheme.ButtonHeight),
            TabIndex = 1
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(310), ClassicTheme.S(32), ClassicTheme.S(68), ClassicTheme.ButtonHeight),
            TabIndex = 2
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(310), ClassicTheme.S(56), ClassicTheme.S(68), ClassicTheme.ButtonHeight),
            TabIndex = 3
        };

        ok.Click += (_, _) =>
        {
            if (_targets.SelectedIndex < 0 || _targets.SelectedIndex >= groups.Count)
            {
                ClassicMessageBox.ShowWarning(this, "Choose a destination group.", Text);
                return;
            }
            TargetGroup = groups[_targets.SelectedIndex];
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            move
                ? "The selected program item will be removed from its current group and added to the destination group."
                : "A copy of the selected program item will be added to the destination group.",
            Text);

        Body.Controls.Add(actionLabel);
        Body.Controls.Add(nameValue);
        Body.Controls.Add(fromLabel);
        Body.Controls.Add(fromValue);
        Body.Controls.Add(toLabel);
        Body.Controls.Add(_targets);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _targets.Focus();
    }
}
