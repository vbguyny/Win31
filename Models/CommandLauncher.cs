using System.Diagnostics;
using System.Text;

namespace ProgramManager31.Models;

internal sealed class CommandLauncher
{
    public Func<string, bool>? BuiltInLauncher { get; set; }

    public bool TryLaunch(ProgramItem item, out string error)
    {
        return TryLaunchCommand(item.CommandLine, item.WorkingDirectory, item.RunMinimized, out error);
    }

    public bool TryLaunchCommand(string commandLine, string workingDirectory, bool minimized, out string error)
    {
        error = string.Empty;
        string command = Environment.ExpandEnvironmentVariables(commandLine ?? string.Empty).Trim();
        if (command.Length == 0)
        {
            error = "The command line is empty.";
            return false;
        }

        if (command.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
        {
            string key = command["builtin:".Length..].Trim();
            if (BuiltInLauncher == null || !BuiltInLauncher(key))
            {
                error = $"The built-in program '{key}' is not available.";
                return false;
            }
            return true;
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            string expandedDirectory = Environment.ExpandEnvironmentVariables(workingDirectory);
            if (!Directory.Exists(expandedDirectory))
            {
                error = $"The working directory '{workingDirectory}' does not exist.";
                return false;
            }
            workingDirectory = expandedDirectory;
        }

        var (fileName, arguments) = SplitCommand(command);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = "The command line is empty.";
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                ErrorDialog = false,
                WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal
            };
            if (!string.IsNullOrWhiteSpace(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Cannot find the file '{fileName}' or one of its components.\n\n{ex.Message}";
            return false;
        }
    }

    public static (string FileName, string Arguments) SplitCommand(string commandLine)
    {
        commandLine = commandLine.Trim();
        if (commandLine.Length == 0)
            return (string.Empty, string.Empty);

        var builder = new StringBuilder();
        bool inQuotes = false;
        int i = 0;
        for (; i < commandLine.Length; i++)
        {
            char ch = commandLine[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                builder.Append(ch);
                continue;
            }
            if (char.IsWhiteSpace(ch) && !inQuotes)
                break;
            builder.Append(ch);
        }

        string fileName = builder.ToString().Trim();
        if (fileName.StartsWith('"') && fileName.EndsWith('"') && fileName.Length >= 2)
            fileName = fileName[1..^1];

        string arguments = i < commandLine.Length ? commandLine[(i + 1)..].Trim() : string.Empty;
        return (fileName, arguments);
    }
}
