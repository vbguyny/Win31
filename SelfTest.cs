using ProgramManager31.Controls;
using ProgramManager31.Desktop;
using ProgramManager31.Models;

namespace ProgramManager31;

internal static class SelfTest
{
    public static int Run()
    {
        var failures = new List<string>();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "ProgramManager31-self-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new GroupStore(tempDirectory);
            var settings = store.LoadOrCreate(out var groups);
            Check(failures, groups.Count >= 4, "default groups were created");
            Check(failures, groups.Any(g => g.Name == "Main"), "Main group exists");
            var main = groups.First(g => g.Name == "Main");
            Check(failures, main.Items.Count > 0, "Main group has items");

            var testItem = new ProgramItem
            {
                Name = "Self Test Item",
                CommandLine = "notepad.exe",
                IconKey = "notepad",
                X = 30,
                Y = 40
            };
            main.Items.Add(testItem);
            Check(failures, store.SaveGroup(main), "group file is written");

            var reloadStore = new GroupStore(tempDirectory);
            var settings2 = reloadStore.LoadOrCreate(out var groups2);
            var reloadedMain = groups2.FirstOrDefault(g => g.Name == "Main");
            Check(failures, reloadedMain?.Items.Any(i => i.Name == "Self Test Item") == true,
                "group file round-trips");
            Check(failures, settings2.GroupFiles.Count == groups2.Count,
                "PROGMAN.INI retains the group file list");

            settings2.AutoArrange = false;
            settings2.MinimizeOnUse = true;
            reloadStore.SaveSettings(settings2);
            var settings3 = new GroupStore(tempDirectory).LoadOrCreate(out _);
            Check(failures, !settings3.AutoArrange && settings3.MinimizeOnUse,
                "PROGMAN.INI round-trips");

            var layoutGroup = new ProgramGroup();
            for (int i = 0; i < 7; i++)
            {
                layoutGroup.Items.Add(new ProgramItem { Name = $"Item {i}", CommandLine = "x.exe" });
            }
            var (layout, content) = GroupLayout.Compute(layoutGroup, new Size(300, 200), true, Point.Empty);
            Check(failures, layout.Count == 7, "auto arrange lays out every item");
            Check(failures, layout.Select(l => l.Cell).Distinct().Count() == 7, "auto arrange cells are distinct");
            Check(failures, content.Width > 0 && content.Height > 0, "layout reports content size");

            Check(failures, ShortcutKey.TryParse("Ctrl+Alt+N", out uint modifiers, out uint virtualKey) &&
                            modifiers == (NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT) && virtualKey == 'N',
                "shortcut keys parse");

            var (fileName, arguments) = CommandLauncher.SplitCommand("\"C:\\Program Files\\Test App.exe\" /run");
            Check(failures, fileName == "C:\\Program Files\\Test App.exe" && arguments == "/run",
                "quoted command lines split correctly");
            var (plainFile, plainArgs) = CommandLauncher.SplitCommand("notepad.exe C:\\Temp\\a.txt");
            Check(failures, plainFile == "notepad.exe" && plainArgs == "C:\\Temp\\a.txt",
                "plain command lines split correctly");

            using (var form = new MainForm(tempDirectory) { SuppressExitPrompt = true })
            {
                form.Show();
                Application.DoEvents();
                Check(failures, form.Visible, "main window becomes visible");
                for (int i = 0; i < 4; i++)
                {
                    form.MenuBar.OpenMenu(i);
                    Application.DoEvents();
                    Check(failures, form.MenuBar.IsOpen, $"menu {i} opens");
                    form.MenuBar.CloseMenu();
                    Application.DoEvents();
                }
                Check(failures, form.MenuBar.HandleMnemonic('F'), "Alt+F mnemonic is handled");
                Application.DoEvents();
                Check(failures, form.MenuBar.IsOpen, "Alt+F opens the File menu");
                form.MenuBar.CloseMenu();

                var options = form.MenuBar.ItemsProvider?.Invoke(1) ?? Array.Empty<MenuEntry>();
                var autoArrange = options.FirstOrDefault(e => e.Text.Contains("Auto Arrange", StringComparison.OrdinalIgnoreCase));
                Check(failures, autoArrange != null && autoArrange.Action != null, "Options menu exposes Auto Arrange");
                if (autoArrange?.Action != null)
                {
                    bool before = form.Desktop.AutoArrange;
                    autoArrange.Action();
                    Application.DoEvents();
                    Check(failures, form.Desktop.AutoArrange != before, "Auto Arrange menu item toggles the option");
                    autoArrange.Action();
                    Application.DoEvents();
                    Check(failures, form.Desktop.AutoArrange == before, "Auto Arrange menu item toggles back");
                }

                Check(failures, form.Desktop.Windows.Count == groups2.Count,
                    "desktop has one window per group");
                var mainWindow = form.Desktop.FindWindow(form.Desktop.ActiveGroup!);
                Check(failures, mainWindow != null, "active group window exists");
                if (mainWindow != null)
                {
                    form.Desktop.MaximizeGroup(mainWindow);
                    Application.DoEvents();
                    Check(failures, mainWindow.Group.WindowState == GroupWindowState.Maximized,
                        "group can maximize");
                    form.Desktop.RestoreGroup(mainWindow);
                    Application.DoEvents();
                    Check(failures, mainWindow.Group.WindowState == GroupWindowState.Normal,
                        "group can restore");
                    form.Desktop.MinimizeGroup(mainWindow);
                    Application.DoEvents();
                    Check(failures, mainWindow.Group.WindowState == GroupWindowState.Minimized,
                        "group can minimize");
                }

                var windowMenu = form.MenuBar.ItemsProvider?.Invoke(2) ?? Array.Empty<MenuEntry>();
                var cascade = windowMenu.FirstOrDefault(e => e.Text.Contains("Cascade", StringComparison.OrdinalIgnoreCase));
                var tile = windowMenu.FirstOrDefault(e => e.Text.Contains("Tile", StringComparison.OrdinalIgnoreCase));
                Check(failures, cascade?.Action != null && tile?.Action != null,
                    "Window menu exposes Cascade and Tile");

                var mainGroup = form.Desktop.Windows.First(w => w.Group.Name == "Main").Group;
                var appsGroup = form.Desktop.Windows.First(w => w.Group.Name == "Applications").Group;
                var mainWindow2 = form.Desktop.FindWindow(mainGroup);
                var appsWindow = form.Desktop.FindWindow(appsGroup);
                Check(failures, mainWindow2 != null && appsWindow != null, "test group windows exist");

                if (mainWindow2 != null && appsWindow != null && cascade?.Action != null && tile?.Action != null)
                {
                    if (mainGroup.WindowState == GroupWindowState.Minimized)
                        form.Desktop.RestoreGroup(mainWindow2);
                    if (appsGroup.WindowState == GroupWindowState.Minimized)
                        form.Desktop.RestoreGroup(appsWindow);
                    Application.DoEvents();

                    cascade.Action();
                    Application.DoEvents();
                    Check(failures, mainWindow2.Bounds.Location != appsWindow.Bounds.Location,
                        "Cascade offsets open group windows");

                    tile.Action();
                    Application.DoEvents();
                    Check(failures, mainWindow2.Bounds != appsWindow.Bounds &&
                                    mainWindow2.Bounds.Width > 0 && appsWindow.Bounds.Width > 0,
                        "Tile sizes open group windows");

                    form.Desktop.ActivateGroup(appsGroup, focusWindow: false);
                    int mainCount = mainGroup.Items.Count;
                    int appsCount = appsGroup.Items.Count;
                    var targetPoint = appsWindow.PointToScreen(new Point(appsWindow.Width / 2, appsWindow.Height / 2));
                    form.Desktop.BeginItemDrag(mainWindow2, 0, targetPoint);
                    form.Desktop.CompleteItemDrag(targetPoint);
                    Application.DoEvents();
                    Check(failures, mainGroup.Items.Count == mainCount - 1 && appsGroup.Items.Count == appsCount + 1,
                        "item drag moves a program item between groups");
                }

                form.Close();
                Application.DoEvents();
            }

            Check(failures, !Directory.Exists(Path.Combine(tempDirectory, "missing.grp")),
                "missing group file is not created");
        }
        catch (Exception ex)
        {
            failures.Add("Unhandled exception: " + ex);
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("SELF-TEST PASSED");
            return 0;
        }

        Console.WriteLine("SELF-TEST FAILED");
        foreach (var failure in failures)
            Console.WriteLine("  - " + failure);
        return 1;
    }

    private static void Check(List<string> failures, bool condition, string description)
    {
        if (condition)
            Console.WriteLine("PASS: " + description);
        else
            failures.Add(description);
    }
}
