namespace ProgramManager31.Models;

public sealed class ProgramManagerSettings
{
    public bool AutoArrange { get; set; } = true;
    public bool MinimizeOnUse { get; set; }
    public bool SaveSettingsOnExit { get; set; } = true;
    public Rectangle MainBounds { get; set; } = new(40, 40, 1024, 720);
    public bool MainMaximized { get; set; }
    public string AutoStartGroup { get; set; } = "StartUp";
    public List<string> GroupFiles { get; } = new();
}
