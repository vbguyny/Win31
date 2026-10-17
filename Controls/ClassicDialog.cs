namespace ProgramManager31.Controls;

internal class ClassicDialog : ClassicForm
{
    public ClassicButton? DefaultButton { get; set; }
    public new ClassicButton? CancelButton { get; set; }

    public ClassicDialog()
    {
        Resizable = false;
        ShowMinimize = false;
        ShowMaximize = false;
        ShowSystemBox = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        BackColor = ClassicTheme.Face;
        Font = ClassicTheme.UI;
        Content.BackColor = ClassicTheme.Face;
    }

    public Panel Body => Content;

    public void SetContentSize(int width, int height)
    {
        ClientSize = new Size(
            width + ClassicTheme.Frame * 2,
            height + ClassicTheme.Frame * 2 + ClassicTheme.TitleBar + ClassicTheme.S(1));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (CancelButton != null)
                CancelButton.PerformClick();
            else
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            return true;
        }

        if (keyData == Keys.Enter)
        {
            var focused = FindFocused(this);
            if (focused is TextBox { Multiline: true })
                return base.ProcessCmdKey(ref msg, keyData);
            if (focused is ClassicButton focusedButton)
            {
                focusedButton.PerformClick();
                return true;
            }
            if (DefaultButton != null)
            {
                DefaultButton.PerformClick();
                return true;
            }
        }

        if ((keyData & Keys.Alt) != 0)
        {
            var key = keyData & Keys.KeyCode;
            if (key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
            {
                if (ClickButtonMnemonic((char)key))
                    return true;
                if (FocusLabelTargetIn(this, (char)key))
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected bool ClickButtonMnemonic(char key)
    {
        foreach (Control control in Controls)
        {
            if (ClickButtonMnemonic(control, key))
                return true;
        }
        return false;
    }

    private static bool ClickButtonMnemonic(Control parent, char key)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is ClassicButton button && button.Enabled && button.Mnemonic != '\0' &&
                char.ToUpperInvariant(button.Mnemonic) == char.ToUpperInvariant(key))
            {
                button.PerformClick();
                return true;
            }
            if (control.HasChildren && ClickButtonMnemonic(control, key))
                return true;
        }
        return false;
    }

    private static bool FocusLabelTargetIn(Control parent, char key)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is ClassicLabel label && label.Target != null && label.Mnemonic != '\0' &&
                char.ToUpperInvariant(label.Mnemonic) == char.ToUpperInvariant(key))
            {
                label.Target.Focus();
                return true;
            }
            if (control.HasChildren && FocusLabelTargetIn(control, key))
                return true;
        }
        return false;
    }

    private static Control? FindFocused(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control.Focused)
                return control;
            var child = FindFocused(control);
            if (child != null)
                return child;
        }
        return null;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (DefaultButton != null)
            DefaultButton.Focus();
    }
}
