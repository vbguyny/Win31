namespace ProgramManager31;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.EnableVisualStyles();
        ClassicTheme.Initialize();

        string? dataDirectory = null;
        string? screenshot = null;
        string? dialogDirectory = null;
        bool selfTest = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--data-dir" when i + 1 < args.Length:
                    dataDirectory = args[++i];
                    break;
                case "--screenshot" when i + 1 < args.Length:
                    screenshot = args[++i];
                    break;
                case "--dialogs" when i + 1 < args.Length:
                    dialogDirectory = args[++i];
                    break;
                case "--self-test":
                    selfTest = true;
                    break;
            }
        }

        if (selfTest)
        {
            Environment.ExitCode = SelfTest.Run();
            return;
        }

        if (screenshot != null || dialogDirectory != null)
        {
            bool ownsData = dataDirectory == null;
            string shotData = dataDirectory ??
                Path.Combine(Path.GetTempPath(), "ProgramManager31-shot-" + Guid.NewGuid().ToString("N"));
            ScreenshotHarness.Run(screenshot, dialogDirectory, shotData, ownsData);
            return;
        }

        Application.Run(new MainForm(dataDirectory));
    }
}