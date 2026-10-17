namespace ProgramManager31.Models;

public sealed class ProgramItem
{
    public string Name { get; set; } = "Program";
    public string CommandLine { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string ShortcutKey { get; set; } = string.Empty;
    public bool RunMinimized { get; set; }
    public string IconKey { get; set; } = "application";
    public int X { get; set; } = 4;
    public int Y { get; set; } = 4;

    public ProgramItem Clone() => new()
    {
        Name = Name,
        CommandLine = CommandLine,
        WorkingDirectory = WorkingDirectory,
        ShortcutKey = ShortcutKey,
        RunMinimized = RunMinimized,
        IconKey = IconKey,
        X = X,
        Y = Y
    };
}
