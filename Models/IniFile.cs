using System.Text;

namespace ProgramManager31.Models;

internal sealed class IniFile
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    public static IniFile Load(string path)
    {
        var ini = new IniFile();
        if (!File.Exists(path))
            return ini;

        string section = string.Empty;
        foreach (var rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                if (!ini._sections.ContainsKey(section))
                    ini._sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            int equals = line.IndexOf('=');
            if (equals <= 0)
                continue;

            string key = line[..equals].Trim();
            string value = line[(equals + 1)..].Trim();
            ini.Set(section, key, value);
        }
        return ini;
    }

    public string Get(string section, string key, string fallback = "")
    {
        return _sections.TryGetValue(section, out var values) &&
               values.TryGetValue(key, out var value)
            ? value
            : fallback;
    }

    public int GetInt(string section, string key, int fallback)
        => int.TryParse(Get(section, key, fallback.ToString()), out var value) ? value : fallback;

    public bool GetBool(string section, string key, bool fallback)
    {
        string value = Get(section, key, fallback ? "1" : "0").Trim();
        if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;
        return fallback;
    }

    public void Set(string section, string key, string value)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }
        values[key] = value;
    }

    public IReadOnlyDictionary<string, string> Section(string section)
        => _sections.TryGetValue(section, out var values)
            ? values
            : new Dictionary<string, string>();

    public void Save(string path)
    {
        var builder = new StringBuilder();
        foreach (var (section, values) in _sections)
        {
            builder.Append('[').Append(section).AppendLine("]");
            foreach (var (key, value) in values)
                builder.Append(key).Append('=').AppendLine(value);
            builder.AppendLine();
        }
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
