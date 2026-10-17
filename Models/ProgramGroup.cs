namespace ProgramManager31.Models;

public enum GroupWindowState
{
    Normal,
    Minimized,
    Maximized
}

public sealed class ProgramGroup
{
    public string Name { get; set; } = "Group";
    public string FileName { get; set; } = string.Empty;
    public List<ProgramItem> Items { get; } = new();
    public GroupWindowState WindowState { get; set; } = GroupWindowState.Normal;
    public Rectangle NormalBounds { get; set; } = new(20, 20, 520, 340);
    public Point IconLocation { get; set; } = new(20, 400);

    public ProgramItem? FindItem(string name)
        => Items.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
}
