namespace ProgramManager31.Controls;

internal enum ClassicMessageBoxButtons
{
    OK,
    OKCancel,
    YesNo
}

internal enum ClassicMessageBoxIcon
{
    None,
    Information,
    Warning,
    Error,
    Question
}

internal static class ClassicMessageBox
{
    public static DialogResult Show(IWin32Window? owner, string text, string title = "Program Manager",
        ClassicMessageBoxButtons buttons = ClassicMessageBoxButtons.OK,
        ClassicMessageBoxIcon icon = ClassicMessageBoxIcon.None)
    {
        using var dialog = new MessageBoxDialog(text, title, buttons, icon);
        var form = owner as Form ?? (owner as Control)?.FindForm();
        return form != null ? dialog.ShowDialog(form) : dialog.ShowDialog();
    }

    public static void ShowError(IWin32Window? owner, string text, string title = "Program Manager")
        => Show(owner, text, title, ClassicMessageBoxButtons.OK, ClassicMessageBoxIcon.Error);

    public static void ShowInfo(IWin32Window? owner, string text, string title = "Program Manager")
        => Show(owner, text, title, ClassicMessageBoxButtons.OK, ClassicMessageBoxIcon.Information);

    public static void ShowWarning(IWin32Window? owner, string text, string title = "Program Manager")
        => Show(owner, text, title, ClassicMessageBoxButtons.OK, ClassicMessageBoxIcon.Warning);

    public static bool Confirm(IWin32Window? owner, string text, string title = "Program Manager")
        => Show(owner, text, title, ClassicMessageBoxButtons.YesNo, ClassicMessageBoxIcon.Question) == DialogResult.Yes;

    private sealed class MessageBoxDialog : ClassicDialog
    {
        public MessageBoxDialog(string text, string title, ClassicMessageBoxButtons buttons, ClassicMessageBoxIcon icon)
        {
            Text = title;
            int contentWidth = ClassicTheme.S(370);
            int textLeft = ClassicTheme.S(56);
            int textWidth = contentWidth - textLeft - ClassicTheme.S(12);

            using (var g = CreateGraphics())
            {
                var measured = TextRenderer.MeasureText(g, text, ClassicTheme.UI,
                    new Size(textWidth, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                int contentHeight = Math.Max(ClassicTheme.S(92), measured.Height + ClassicTheme.S(52));
                SetContentSize(contentWidth, contentHeight);

                if (icon != ClassicMessageBoxIcon.None)
                {
                    var key = icon switch
                    {
                        ClassicMessageBoxIcon.Warning => "warning",
                        ClassicMessageBoxIcon.Error => "error",
                        ClassicMessageBoxIcon.Question => "question",
                        _ => "info"
                    };
                    var iconBox = new PictureBox
                    {
                        Bounds = new Rectangle(ClassicTheme.S(14), ClassicTheme.S(14),
                            ClassicTheme.IconSize, ClassicTheme.IconSize),
                        BackColor = ClassicTheme.Face
                    };
                    iconBox.Paint += (_, e) => Icons.IconPainter.DrawIcon(e.Graphics, key,
                        new Rectangle(0, 0, iconBox.Width, iconBox.Height));
                    Body.Controls.Add(iconBox);
                }

                var label = new ClassicLabel
                {
                    Text = text,
                    WordWrap = true,
                    Bounds = new Rectangle(textLeft, ClassicTheme.S(14), textWidth, contentHeight - ClassicTheme.S(50))
                };
                Body.Controls.Add(label);

                int buttonY = contentHeight - ClassicTheme.S(12) - ClassicTheme.ButtonHeight;
                int right = contentWidth - ClassicTheme.S(12);
                var buttonList = new List<ClassicButton>();
                switch (buttons)
                {
                    case ClassicMessageBoxButtons.OK:
                        buttonList.Add(CreateButton("OK", right - ClassicTheme.S(72), buttonY, DialogResult.OK));
                        break;
                    case ClassicMessageBoxButtons.OKCancel:
                        buttonList.Add(CreateButton("OK", right - ClassicTheme.S(152), buttonY, DialogResult.OK));
                        buttonList.Add(CreateButton("Cancel", right - ClassicTheme.S(72), buttonY, DialogResult.Cancel));
                        break;
                    case ClassicMessageBoxButtons.YesNo:
                        buttonList.Add(CreateButton("&Yes", right - ClassicTheme.S(152), buttonY, DialogResult.Yes));
                        buttonList.Add(CreateButton("&No", right - ClassicTheme.S(72), buttonY, DialogResult.No));
                        break;
                }

                foreach (var button in buttonList)
                    Body.Controls.Add(button);

                DefaultButton = buttonList[0];
                CancelButton = buttonList.Count > 1 ? buttonList[1] : null;
            }
        }

        private ClassicButton CreateButton(string text, int x, int y, DialogResult result)
        {
            var button = new ClassicButton
            {
                Text = text,
                Bounds = new Rectangle(x, y, ClassicTheme.S(70), ClassicTheme.ButtonHeight)
            };
            button.Click += (_, _) =>
            {
                DialogResult = result;
                Close();
            };
            return button;
        }
    }
}
