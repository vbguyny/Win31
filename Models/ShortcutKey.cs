namespace ProgramManager31.Models;

internal static class ShortcutKey
{
    public const string None = "None";

    public static bool TryParse(string? text, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;
        if (string.IsNullOrWhiteSpace(text) || text.Equals(None, StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        string keyPart = parts[^1];
        if (keyPart.Length != 1)
            return false;
        char key = char.ToUpperInvariant(keyPart[0]);
        if (!char.IsLetterOrDigit(key))
            return false;

        bool control = false;
        bool alt = false;
        bool shift = false;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                parts[i].Equals("Control", StringComparison.OrdinalIgnoreCase))
                control = true;
            else if (parts[i].Equals("Alt", StringComparison.OrdinalIgnoreCase))
                alt = true;
            else if (parts[i].Equals("Shift", StringComparison.OrdinalIgnoreCase))
                shift = true;
            else
                return false;
        }

        if (!control || !alt)
            return false;

        modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
        if (shift)
            modifiers |= NativeMethods.MOD_SHIFT;
        virtualKey = (uint)char.ToUpperInvariant(key);
        return true;
    }

    public static string Format(char key)
    {
        char upper = char.ToUpperInvariant(key);
        return char.IsLetterOrDigit(upper) ? $"Ctrl+Alt+{upper}" : string.Empty;
    }

    public static string Display(string? text)
        => string.IsNullOrWhiteSpace(text) || text.Equals(None, StringComparison.OrdinalIgnoreCase)
            ? None
            : text;
}
