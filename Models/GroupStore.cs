using System.Text;
using System.Text.Json;
using ProgramManager31.Icons;

namespace ProgramManager31.Models;

internal sealed class GroupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string DataDirectory { get; }
    public string SettingsPath => Path.Combine(DataDirectory, "PROGMAN.INI");
    public List<string> Warnings { get; } = new();
    public bool CreatedDefaults { get; private set; }

    public GroupStore(string dataDirectory)
    {
        DataDirectory = Path.GetFullPath(dataDirectory);
    }

    public ProgramManagerSettings LoadOrCreate(out List<ProgramGroup> groups)
    {
        groups = new List<ProgramGroup>();
        Directory.CreateDirectory(DataDirectory);

        var settings = new ProgramManagerSettings();
        if (File.Exists(SettingsPath))
        {
            try
            {
                var ini = IniFile.Load(SettingsPath);
                settings.AutoArrange = ini.GetBool("Settings", "AutoArrange", true);
                settings.MinimizeOnUse = ini.GetBool("Settings", "MinimizeOnUse", false);
                settings.SaveSettingsOnExit = ini.GetBool("Settings", "SaveSettings", true);
                settings.MainMaximized = ini.GetBool("Settings", "Maximized", false);
                settings.AutoStartGroup = ini.Get("Settings", "AutoStart", "StartUp");
                settings.MainBounds = ParseBounds(ini.Get("Settings", "Window", "40,40,1024,720"), new Rectangle(40, 40, 1024, 720));

                var groupEntries = ini.Section("Groups")
                    .Select(kv => (Key: kv.Key, Value: kv.Value, Number: ParseGroupNumber(kv.Key)))
                    .Where(e => e.Number > 0 && !string.IsNullOrWhiteSpace(e.Value))
                    .OrderBy(e => e.Number)
                    .ToList();

                foreach (var entry in groupEntries)
                {
                    string path = ResolvePath(entry.Value);
                    var group = LoadGroup(path);
                    if (group != null)
                    {
                        group.FileName = entry.Value;
                        groups.Add(group);
                        settings.GroupFiles.Add(entry.Value);
                    }
                    else
                    {
                        Warnings.Add($"The group file '{entry.Value}' could not be opened.");
                    }
                }
            }
            catch (Exception ex)
            {
                Warnings.Add($"PROGMAN.INI could not be read: {ex.Message}");
            }
        }

        if (groups.Count == 0)
        {
            CreatedDefaults = true;
            groups = CreateDefaultGroups(settings);
            SaveSettings(settings);
        }

        return settings;
    }

    public void SaveSettings(ProgramManagerSettings settings)
    {
        Directory.CreateDirectory(DataDirectory);
        var ini = new IniFile();
        ini.Set("Settings", "AutoArrange", settings.AutoArrange ? "1" : "0");
        ini.Set("Settings", "MinimizeOnUse", settings.MinimizeOnUse ? "1" : "0");
        ini.Set("Settings", "SaveSettings", settings.SaveSettingsOnExit ? "1" : "0");
        ini.Set("Settings", "AutoStart", settings.AutoStartGroup);
        ini.Set("Settings", "Maximized", settings.MainMaximized ? "1" : "0");
        var b = settings.MainBounds;
        ini.Set("Settings", "Window", $"{b.X},{b.Y},{b.Width},{b.Height}");

        for (int i = 0; i < settings.GroupFiles.Count; i++)
            ini.Set("Groups", $"Group{i + 1}", settings.GroupFiles[i]);

        WriteAtomic(SettingsPath, BuildIniText(ini));
    }

    public ProgramGroup? LoadGroupFile(string fileName)
    {
        var group = LoadGroup(ResolvePath(fileName));
        if (group != null)
            group.FileName = fileName;
        return group;
    }

    public bool SaveGroup(ProgramGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.FileName))
            group.FileName = NewGroupFileName(group.Name);

        string path = ResolvePath(group.FileName);
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var dto = ToDto(group);
            string json = JsonSerializer.Serialize(dto, JsonOptions);
            WriteAtomic(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Warnings.Add($"The group file '{group.FileName}' could not be written: {ex.Message}");
            return false;
        }
    }

    public void DeleteGroupFileIfManaged(ProgramGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.FileName))
            return;
        string path = ResolvePath(group.FileName);
        if (!IsInsideDataDirectory(path) || !File.Exists(path))
            return;
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Leaving an orphaned group file is harmless.
        }
    }

    public string NewGroupFileName(string description)
    {
        string stem = SanitizeStem(description);
        for (int i = 1; i < 1000; i++)
        {
            string candidate = i == 1 ? $"{stem}.GRP" : $"{Truncate(stem, 7)}{i}.GRP";
            if (!File.Exists(ResolvePath(candidate)))
                return candidate;
        }
        return $"GROUP{Environment.TickCount % 1000}.GRP";
    }

    public string ResolvePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Path.Combine(DataDirectory, "GROUP.GRP");
        return Path.IsPathRooted(fileName)
            ? Path.GetFullPath(fileName)
            : Path.GetFullPath(Path.Combine(DataDirectory, fileName));
    }

    public string MakeStoredPath(string fullPath)
    {
        try
        {
            string relative = Path.GetRelativePath(DataDirectory, fullPath);
            if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
                return relative;
        }
        catch
        {
            // Fall back to the full path.
        }
        return fullPath;
    }

    private ProgramGroup? LoadGroup(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string content = File.ReadAllText(path);
            if (!content.TrimStart().StartsWith('{'))
            {
                Warnings.Add($"'{Path.GetFileName(path)}' is not a group file created by this program. " +
                             "The original binary Windows 3.1 .GRP format is not supported.");
                return null;
            }

            var dto = JsonSerializer.Deserialize<GroupFileDto>(content, JsonOptions);
            if (dto == null || !string.Equals(dto.Format, "ProgramManager31.Group", StringComparison.Ordinal))
            {
                Warnings.Add($"'{Path.GetFileName(path)}' is not a recognized Program Manager group file.");
                return null;
            }

            var group = new ProgramGroup
            {
                Name = string.IsNullOrWhiteSpace(dto.Name) ? Path.GetFileNameWithoutExtension(path) : dto.Name,
                FileName = MakeStoredPath(path)
            };
            if (dto.Window != null)
            {
                group.WindowState = Enum.TryParse<GroupWindowState>(dto.Window.State, true, out var state)
                    ? state
                    : GroupWindowState.Normal;
                group.NormalBounds = new Rectangle(dto.Window.X, dto.Window.Y,
                    Math.Max(160, dto.Window.Width), Math.Max(100, dto.Window.Height));
                group.IconLocation = new Point(dto.Window.IconX, dto.Window.IconY);
            }

            foreach (var itemDto in dto.Items ?? new List<GroupItemDto>())
            {
                group.Items.Add(new ProgramItem
                {
                    Name = string.IsNullOrWhiteSpace(itemDto.Name) ? "Program" : itemDto.Name,
                    CommandLine = itemDto.CommandLine ?? string.Empty,
                    WorkingDirectory = itemDto.WorkingDirectory ?? string.Empty,
                    ShortcutKey = itemDto.ShortcutKey ?? string.Empty,
                    RunMinimized = itemDto.RunMinimized,
                    IconKey = IconPainter.Normalize(itemDto.IconKey ?? "application"),
                    X = Math.Max(0, itemDto.X),
                    Y = Math.Max(0, itemDto.Y)
                });
            }
            return group;
        }
        catch (Exception ex)
        {
            Warnings.Add($"The group file '{Path.GetFileName(path)}' could not be read: {ex.Message}");
            return null;
        }
    }

    private List<ProgramGroup> CreateDefaultGroups(ProgramManagerSettings settings)
    {
        var groups = new List<ProgramGroup>();

        var main = new ProgramGroup
        {
            Name = "Main",
            FileName = "MAIN.GRP",
            WindowState = GroupWindowState.Normal,
            NormalBounds = new Rectangle(24, 24, 540, 360),
            IconLocation = new Point(24, 400)
        };
        main.Items.AddRange(new[]
        {
            Item("File Manager", "explorer.exe", "filemanager"),
            Item("Control Panel", "control.exe", "controlpanel"),
            Item("Print Manager", "builtin:printmanager", "printmanager"),
            Item("Clipboard Viewer", "builtin:clipboard", "clipboard"),
            Item("MS-DOS Prompt", "cmd.exe", "dos"),
            Item("Windows Setup", "builtin:about", "setup")
        });
        groups.Add(main);

        var accessories = new ProgramGroup
        {
            Name = "Accessories",
            FileName = "ACCESSOR.GRP",
            WindowState = GroupWindowState.Minimized,
            NormalBounds = new Rectangle(110, 70, 470, 320),
            IconLocation = new Point(130, 400)
        };
        accessories.Items.AddRange(new[]
        {
            Item("Write", "notepad.exe", "write"),
            Item("Paintbrush", "mspaint.exe", "paint"),
            Item("Calculator", "calc.exe", "calculator"),
            Item("Clock", "builtin:clock", "clock"),
            Item("Character Map", "charmap.exe", "charmap"),
            Item("Notepad", "notepad.exe", "notepad")
        });
        groups.Add(accessories);

        var startUp = new ProgramGroup
        {
            Name = "StartUp",
            FileName = "STARTUP.GRP",
            WindowState = GroupWindowState.Minimized,
            NormalBounds = new Rectangle(150, 100, 430, 280),
            IconLocation = new Point(24, 420)
        };
        groups.Add(startUp);

        var apps = new ProgramGroup
        {
            Name = "Applications",
            FileName = "APPS.GRP",
            WindowState = GroupWindowState.Minimized,
            NormalBounds = new Rectangle(190, 130, 430, 280),
            IconLocation = new Point(120, 420)
        };
        groups.Add(apps);

        foreach (var group in groups)
        {
            if (SaveGroup(group))
                settings.GroupFiles.Add(group.FileName);
        }
        return groups;
    }

    private static ProgramItem Item(string name, string command, string icon) => new()
    {
        Name = name,
        CommandLine = command,
        IconKey = icon
    };

    private static GroupFileDto ToDto(ProgramGroup group)
    {
        var dto = new GroupFileDto
        {
            Format = "ProgramManager31.Group",
            Version = 1,
            Name = group.Name,
            Window = new GroupWindowDto
            {
                State = group.WindowState.ToString(),
                X = group.NormalBounds.X,
                Y = group.NormalBounds.Y,
                Width = group.NormalBounds.Width,
                Height = group.NormalBounds.Height,
                IconX = group.IconLocation.X,
                IconY = group.IconLocation.Y
            }
        };
        foreach (var item in group.Items)
        {
            dto.Items.Add(new GroupItemDto
            {
                Name = item.Name,
                CommandLine = item.CommandLine,
                WorkingDirectory = item.WorkingDirectory,
                ShortcutKey = item.ShortcutKey,
                RunMinimized = item.RunMinimized,
                IconKey = item.IconKey,
                X = item.X,
                Y = item.Y
            });
        }
        return dto;
    }

    private static Rectangle ParseBounds(string value, Rectangle fallback)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 4 &&
            int.TryParse(parts[0], out int x) &&
            int.TryParse(parts[1], out int y) &&
            int.TryParse(parts[2], out int width) &&
            int.TryParse(parts[3], out int height) &&
            width > 0 && height > 0)
        {
            return new Rectangle(x, y, width, height);
        }
        return fallback;
    }

    private static int ParseGroupNumber(string key)
    {
        if (key.StartsWith("Group", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key[5..], out int number))
            return number;
        return 0;
    }

    private static string BuildIniText(IniFile ini)
    {
        var builder = new StringBuilder();
        foreach (var section in new[] { "Settings", "Groups" })
        {
            builder.Append('[').Append(section).AppendLine("]");
            foreach (var (key, value) in ini.Section(section))
                builder.Append(key).Append('=').AppendLine(value);
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private static void WriteAtomic(string path, string content)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temp, path, overwrite: true);
    }

    private static string SanitizeStem(string description)
    {
        var builder = new StringBuilder();
        foreach (char ch in description.ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(ch);
            if (builder.Length == 8)
                break;
        }
        string stem = builder.Length == 0 ? "GROUP" : builder.ToString();
        return Truncate(stem, 8);
    }

    private static string Truncate(string value, int length)
        => value.Length <= length ? value : value[..length];

    private bool IsInsideDataDirectory(string path)
    {
        string root = DataDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                      Path.DirectorySeparatorChar;
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GroupFileDto
    {
        public string Format { get; set; } = "ProgramManager31.Group";
        public int Version { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public GroupWindowDto? Window { get; set; }
        public List<GroupItemDto> Items { get; set; } = new();
    }

    private sealed class GroupWindowDto
    {
        public string State { get; set; } = "Normal";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 520;
        public int Height { get; set; } = 340;
        public int IconX { get; set; }
        public int IconY { get; set; }
    }

    private sealed class GroupItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? CommandLine { get; set; }
        public string? WorkingDirectory { get; set; }
        public string? ShortcutKey { get; set; }
        public bool RunMinimized { get; set; }
        public string? IconKey { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
