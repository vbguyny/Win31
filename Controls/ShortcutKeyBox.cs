using ProgramManager31.Models;

namespace ProgramManager31.Controls;

internal sealed class ShortcutKeyBox : ClassicTextBox
{
    public ShortcutKeyBox()
    {
        ReadOnly = true;
        Text = ShortcutKey.None;
        Inner.KeyDown += OnInnerKeyDown;
    }

    public string ShortcutValue
    {
        get => Text.Equals(ShortcutKey.None, StringComparison.OrdinalIgnoreCase) ? string.Empty : Text;
        set => Text = ShortcutKey.Display(value);
    }

    private void OnInnerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey ||
            e.KeyCode is Keys.LControlKey or Keys.RControlKey or Keys.LMenu or Keys.RMenu or Keys.LShiftKey or Keys.RShiftKey)
        {
            return;
        }

        if (e.KeyCode is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
        {
            Text = ShortcutKey.Format((char)e.KeyCode);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode is Keys.Back or Keys.Delete)
        {
            Text = ShortcutKey.None;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }
}
