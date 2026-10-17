using System.Drawing.Imaging;
using ProgramManager31.BuiltIns;
using ProgramManager31.Dialogs;
using ProgramManager31.Models;

namespace ProgramManager31;

internal static class ScreenshotHarness
{
    public static void Run(string? mainShot, string? dialogDirectory, string dataDirectory, bool ownsDataDirectory)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(mainShot))
            {
                using var form = new MainForm(dataDirectory) { SuppressExitPrompt = true };
                form.Show();
                Application.DoEvents();
                form.MenuBar.OpenMenu(0);
                Application.DoEvents();
                Thread.Sleep(100);
                Application.DoEvents();
                var menu = Application.OpenForms.OfType<Controls.ClassicMenuDropDown>().FirstOrDefault();
                if (menu != null)
                    CaptureExisting(menu, Path.Combine(Path.GetDirectoryName(mainShot) ?? ".",
                        Path.GetFileNameWithoutExtension(mainShot) + "-file-menu.png"));
                form.MenuBar.CloseMenu();
                Application.DoEvents();
                CaptureExisting(form, mainShot);
            }

            if (!string.IsNullOrWhiteSpace(dialogDirectory))
            {
                Directory.CreateDirectory(dialogDirectory);
                var groupA = new ProgramGroup { Name = "Main" };
                var groupB = new ProgramGroup { Name = "Accessories" };
                var item = new ProgramItem { Name = "Notepad", CommandLine = "notepad.exe", IconKey = "notepad" };
                var demoGroup = new ProgramGroup { Name = "Accessories", FileName = "ACCESSORIES.GRP" };
                var demoItem = new ProgramItem
                {
                    Name = "Notepad",
                    CommandLine = @"C:\WINDOWS\NOTEPAD.EXE",
                    WorkingDirectory = @"C:\WINDOWS",
                    IconKey = "notepad",
                    ShortcutKey = "Ctrl+Alt+N"
                };

                Capture(new NewObjectDialog(false), Path.Combine(dialogDirectory, "new-object.png"));
                Capture(new GroupPropertiesDialog(demoGroup), Path.Combine(dialogDirectory, "group-properties.png"));
                Capture(new ProgramItemPropertiesDialog(demoItem, "Main", Array.Empty<string>()),
                    Path.Combine(dialogDirectory, "program-item-properties.png"));
                Capture(new MoveCopyDialog(item, groupA, new[] { groupB }, true),
                    Path.Combine(dialogDirectory, "move-program.png"));
                Capture(new RunDialog("notepad.exe"), Path.Combine(dialogDirectory, "run.png"));
                Capture(new ChangeIconDialog("notepad"), Path.Combine(dialogDirectory, "change-icon.png"));
                Capture(new AboutDialog(), Path.Combine(dialogDirectory, "about.png"));
                Capture(new HelpWindow(), Path.Combine(dialogDirectory, "help.png"));
                Capture(new ClassicFileDialog("Browse",
                        "Programs (*.exe;*.com;*.bat;*.cmd)|*.exe;*.com;*.bat;*.cmd|All files (*.*)|*.*"),
                    Path.Combine(dialogDirectory, "browse.png"));
                Capture(new ClockWindow(), Path.Combine(dialogDirectory, "clock.png"));
                Capture(new ClipboardViewerWindow(), Path.Combine(dialogDirectory, "clipboard.png"));
                Capture(new PrintManagerWindow(), Path.Combine(dialogDirectory, "print-manager.png"));
            }
        }
        finally
        {
            if (ownsDataDirectory)
            {
                try { Directory.Delete(dataDirectory, recursive: true); } catch { }
            }
        }
    }

    private static void Capture(Form form, string path)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(20, 20);
        form.Show();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();
        CaptureExisting(form, path);
        form.Close();
        form.Dispose();
        Application.DoEvents();
    }

    private static void CaptureExisting(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(path, ImageFormat.Png);
    }
}
