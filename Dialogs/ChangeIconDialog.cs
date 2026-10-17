using ProgramManager31.Controls;
using ProgramManager31.Icons;

namespace ProgramManager31.Dialogs;

internal sealed class ChangeIconDialog : ClassicDialog
{
    private readonly ClassicListBox _list;
    private readonly PictureBox _preview;
    private readonly string[] _keys;

    public string SelectedIconKey { get; private set; }

    public ChangeIconDialog(string currentIcon)
    {
        Text = "Change Icon";
        SetContentSize(ClassicTheme.S(390), ClassicTheme.S(200));
        _keys = IconPainter.AvailableIcons.Select(i => i.Key).ToArray();
        SelectedIconKey = IconPainter.Normalize(currentIcon);

        var label = new ClassicLabel
        {
            Text = "&Current icon:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(18), ClassicTheme.S(120), ClassicTheme.S(16))
        };

        _list = new ClassicListBox
        {
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(38), ClassicTheme.S(190), ClassicTheme.S(145)),
            TabIndex = 0
        };
        foreach (var entry in IconPainter.AvailableIcons)
            _list.Items.Add(entry.Label);
        int index = Array.FindIndex(_keys, k => string.Equals(k, SelectedIconKey, StringComparison.OrdinalIgnoreCase));
        _list.SelectedIndex = index >= 0 ? index : 0;

        _preview = new PictureBox
        {
            Bounds = new Rectangle(ClassicTheme.S(220), ClassicTheme.S(42), ClassicTheme.S(48), ClassicTheme.S(48)),
            BackColor = ClassicTheme.Window
        };
        _preview.Paint += (_, e) =>
        {
            ClassicTheme.DrawSunkenBorder(e.Graphics, _preview.ClientRectangle);
            IconPainter.DrawIcon(e.Graphics, SelectedIconKey,
                new Rectangle(ClassicTheme.S(8), ClassicTheme.S(8), ClassicTheme.IconSize, ClassicTheme.IconSize));
        };

        var previewLabel = new ClassicLabel
        {
            Text = "Preview",
            Bounds = new Rectangle(ClassicTheme.S(220), ClassicTheme.S(96), ClassicTheme.S(120), ClassicTheme.S(16))
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(300), ClassicTheme.S(8), ClassicTheme.S(72), ClassicTheme.ButtonHeight),
            TabIndex = 1
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(300), ClassicTheme.S(32), ClassicTheme.S(72), ClassicTheme.ButtonHeight),
            TabIndex = 2
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(300), ClassicTheme.S(56), ClassicTheme.S(72), ClassicTheme.ButtonHeight),
            TabIndex = 3
        };

        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _keys.Length)
            {
                SelectedIconKey = _keys[_list.SelectedIndex];
                _preview.Invalidate();
            }
        };
        _list.DoubleClick += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            "Choose one of the built-in icons for this program item. " +
            "This recreation does not read icon resources from .EXE or .ICO files.",
            Text);

        Body.Controls.Add(label);
        Body.Controls.Add(_list);
        Body.Controls.Add(_preview);
        Body.Controls.Add(previewLabel);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;
    }
}
