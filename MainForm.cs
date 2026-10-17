using ProgramManager31.BuiltIns;
using ProgramManager31.Controls;
using ProgramManager31.Desktop;
using ProgramManager31.Dialogs;
using ProgramManager31.Models;

namespace ProgramManager31;

internal sealed class MainForm : ClassicForm
{
    private readonly GroupStore _store;
    private readonly ProgramManagerSettings _settings;
    private readonly List<ProgramGroup> _groups;
    private readonly ClassicMenuBar _menuBar;
    private readonly DesktopPanel _desktop;
    private readonly CommandLauncher _launcher = new();
    private readonly Dictionary<int, ProgramItem> _hotkeys = new();
    private readonly Dictionary<string, ClassicForm> _builtInWindows = new(StringComparer.OrdinalIgnoreCase);
    private int _nextHotkeyId = 1;
    private bool _exitConfirmed;
    private bool _warningsShown;
    private bool _startupRun;

    public MainForm(string? dataDirectory = null)
    {
        Text = "Program Manager";
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(ClassicTheme.S(640), ClassicTheme.S(400));
        ShowMinimize = true;
        ShowMaximize = true;
        ShowSystemBox = true;
        Resizable = true;

        _store = new GroupStore(dataDirectory ?? DefaultDataDirectory());
        _settings = _store.LoadOrCreate(out _groups);
        if (_store.CreatedDefaults)
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 720);
            int width = Math.Min(ClassicTheme.S(1024), Math.Max(ClassicTheme.S(640), area.Width - ClassicTheme.S(40)));
            int height = Math.Min(ClassicTheme.S(720), Math.Max(ClassicTheme.S(400), area.Height - ClassicTheme.S(60)));
            _settings.MainBounds = new Rectangle(area.Left + (area.Width - width) / 2,
                area.Top + (area.Height - height) / 2, width, height);
        }
        _settings.MainBounds = ClampToScreen(_settings.MainBounds);
        Bounds = _settings.MainBounds;

        _menuBar = new ClassicMenuBar
        {
            ItemsProvider = BuildMenu
        };
        _menuBar.SetTopLevelItems("&File", "&Options", "&Window", "&Help");

        _desktop = new DesktopPanel
        {
            AutoArrange = _settings.AutoArrange
        };

        Content.Controls.Add(_desktop);
        Content.Controls.Add(_menuBar);
        Content.Resize += (_, _) => LayoutContent();
        Content.Bounds = ContentBounds;
        LayoutContent();

        _desktop.SelectionChanged += (_, _) => UpdateMenuState();
        _desktop.ActiveGroupChanged += (_, _) => OnActiveGroupChanged();
        _desktop.LayoutChanged += (_, _) => SaveLayoutIfEnabled();
        _desktop.GroupChanged += SaveGroup;
        _desktop.ItemLaunchRequested += LaunchItem;

        _launcher.BuiltInLauncher = LaunchBuiltIn;

        foreach (var group in _groups)
            _desktop.AddGroup(group);

        LayoutContent();
        var initial = _groups.FirstOrDefault(g => g.WindowState != GroupWindowState.Minimized) ??
                      _groups.FirstOrDefault();
        if (initial != null)
            _desktop.ActivateGroup(initial, focusWindow: true);

        UpdateTitle();
        UpdateMenuState();
    }

    public DesktopPanel Desktop => _desktop;
    public IReadOnlyList<ProgramGroup> Groups => _groups;
    public ClassicMenuBar MenuBar => _menuBar;
    internal bool SuppressExitPrompt { get; set; }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_settings.MainMaximized)
        {
            MaximizedBounds = Screen.FromControl(this).WorkingArea;
            WindowState = FormWindowState.Maximized;
        }
        RebuildHotkeys();
        BeginInvoke(() =>
        {
            ShowWarnings();
            RunStartupGroup();
            _desktop.Focus();
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!SuppressExitPrompt && !_exitConfirmed &&
            e.CloseReason is not CloseReason.WindowsShutDown and not CloseReason.TaskManagerClosing and
            not CloseReason.ApplicationExitCall)
        {
            if (!ConfirmExit())
            {
                e.Cancel = true;
                return;
            }
        }

        SaveAll();
        UnregisterHotkeys();
        foreach (var window in _builtInWindows.Values.ToList())
        {
            if (!window.IsDisposed)
                window.Close();
        }
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_settings != null)
        {
            if (WindowState == FormWindowState.Normal)
            {
                _settings.MainBounds = Bounds;
                _settings.MainMaximized = false;
            }
            else if (WindowState == FormWindowState.Maximized)
            {
                _settings.MainMaximized = true;
            }
        }
        LayoutContent();
        Invalidate();
    }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        if (_settings != null && WindowState == FormWindowState.Normal)
            _settings.MainBounds = Bounds;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Alt) != 0)
        {
            var key = keyData & Keys.KeyCode;
            if (key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
            {
                if (_menuBar.HandleMnemonic((char)key))
                    return true;
            }
        }

        switch (keyData)
        {
            case Keys.F10:
                _menuBar.Focus();
                return true;
            case Keys.F1:
                ShowHelp();
                return true;
            case Keys.Enter:
                OpenSelected();
                return true;
            case Keys.Delete:
                DeleteSelected();
                return true;
            case Keys.F7:
                ShowMoveCopy(move: true);
                return true;
            case Keys.F8:
                ShowMoveCopy(move: false);
                return true;
            case Keys.Alt | Keys.Enter:
                ShowProperties();
                return true;
            case Keys.Control | Keys.F4:
                MinimizeActiveGroup();
                return true;
            case Keys.Control | Keys.F6:
                _desktop.ActivateNextGroup();
                return true;
            case Keys.Escape:
                _desktop.ActiveWindow?.ClearSelection();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (_hotkeys.TryGetValue(id, out var item))
            {
                LaunchItem(item);
                return;
            }
        }
        base.WndProc(ref m);
    }

    private void LayoutContent()
    {
        if (_menuBar == null || _desktop == null)
            return;
        int menuHeight = ClassicTheme.MenuBar;
        _menuBar.Bounds = new Rectangle(0, 0, Content.ClientSize.Width, menuHeight);
        _desktop.Bounds = new Rectangle(0, menuHeight, Content.ClientSize.Width,
            Math.Max(0, Content.ClientSize.Height - menuHeight));
    }

    private void OnActiveGroupChanged()
    {
        UpdateTitle();
        UpdateMenuState();
    }

    private void UpdateTitle()
    {
        var maximized = _desktop.MaximizedGroup;
        Text = maximized != null ? $"Program Manager - {maximized.Name}" : "Program Manager";
        Invalidate();

        if (maximized != null)
        {
            var window = _desktop.FindWindow(maximized);
            _menuBar.SetChildControls(true,
                () => window?.ShowSystemMenu(_menuBar.PointToScreen(new Point(ClassicTheme.S(2), ClassicTheme.MenuBar))),
                true,
                () =>
                {
                    if (window != null)
                        _desktop.RestoreGroup(window);
                });
        }
        else
        {
            _menuBar.SetChildControls(false, null, false, null);
        }
    }

    private void UpdateMenuState()
    {
        _menuBar.Invalidate();
    }

    private IReadOnlyList<MenuEntry> BuildMenu(int index)
    {
        return index switch
        {
            0 => BuildFileMenu(),
            1 => BuildOptionsMenu(),
            2 => BuildWindowMenu(),
            3 => BuildHelpMenu(),
            _ => Array.Empty<MenuEntry>()
        };
    }

    private IReadOnlyList<MenuEntry> BuildFileMenu()
    {
        bool hasItem = _desktop.SelectedItem != null;
        bool hasGroup = _desktop.ActiveGroup != null;
        return new[]
        {
            MenuEntry.Item("&New...", ShowNewObjectDialog),
            MenuEntry.Item("O&pen", OpenSelected, "Enter", hasItem || hasGroup),
            MenuEntry.Item("&Move...", () => ShowMoveCopy(true), "F7", hasItem),
            MenuEntry.Item("&Copy...", () => ShowMoveCopy(false), "F8", hasItem),
            MenuEntry.Item("&Delete", DeleteSelected, "Del", hasItem || hasGroup),
            MenuEntry.Item("&Properties...", ShowProperties, "Alt+Enter", hasItem || hasGroup),
            MenuEntry.Separator(),
            MenuEntry.Item("&Run...", ShowRunDialog),
            MenuEntry.Separator(),
            MenuEntry.Item("E&xit Windows...", ExitProgram)
        };
    }

    private IReadOnlyList<MenuEntry> BuildOptionsMenu()
    {
        return new[]
        {
            MenuEntry.Item("&Auto Arrange", ToggleAutoArrange, check: _settings.AutoArrange),
            MenuEntry.Item("&Minimize on Use", ToggleMinimizeOnUse, check: _settings.MinimizeOnUse),
            MenuEntry.Item("&Save Settings on Exit", ToggleSaveSettings, check: _settings.SaveSettingsOnExit)
        };
    }

    private IReadOnlyList<MenuEntry> BuildWindowMenu()
    {
        var items = new List<MenuEntry>
        {
            MenuEntry.Item("&Cascade", _desktop.CascadeWindows),
            MenuEntry.Item("&Tile", _desktop.TileWindows),
            MenuEntry.Item("&Arrange Icons", _desktop.ArrangeIcons),
            MenuEntry.Separator()
        };

        for (int i = 0; i < _groups.Count; i++)
        {
            var group = _groups[i];
            string text = i < 9
                ? $"&{i + 1} {EscapeAmpersands(group.Name)}"
                : $"   {EscapeAmpersands(group.Name)}";
            items.Add(MenuEntry.Item(text, () => ActivateGroupFromMenu(group),
                check: group == _desktop.ActiveGroup));
        }
        return items;
    }

    private IReadOnlyList<MenuEntry> BuildHelpMenu()
    {
        return new[]
        {
            MenuEntry.Item("&Contents", ShowHelp, "F1"),
            MenuEntry.Item("&About Program Manager", ShowAbout)
        };
    }

    private void ActivateGroupFromMenu(ProgramGroup group)
    {
        if (group.WindowState == GroupWindowState.Minimized)
        {
            var window = _desktop.FindWindow(group);
            if (window != null)
                _desktop.RestoreGroup(window);
        }
        else
        {
            _desktop.ActivateGroup(group, focusWindow: true);
        }
        UpdateTitle();
    }

    private void ToggleAutoArrange()
    {
        _settings.AutoArrange = !_settings.AutoArrange;
        _desktop.AutoArrange = _settings.AutoArrange;
        foreach (var group in _groups)
            SaveGroup(group);
        _store.SaveSettings(_settings);
        UpdateMenuState();
    }

    private void ToggleMinimizeOnUse()
    {
        _settings.MinimizeOnUse = !_settings.MinimizeOnUse;
        _store.SaveSettings(_settings);
        UpdateMenuState();
    }

    private void ToggleSaveSettings()
    {
        _settings.SaveSettingsOnExit = !_settings.SaveSettingsOnExit;
        _store.SaveSettings(_settings);
        UpdateMenuState();
    }

    private void OpenSelected()
    {
        var item = _desktop.SelectedItem;
        if (item != null)
        {
            LaunchItem(item);
            return;
        }

        var group = _desktop.ActiveGroup;
        if (group == null)
            return;
        if (group.WindowState == GroupWindowState.Minimized)
        {
            var window = _desktop.FindWindow(group);
            if (window != null)
                _desktop.RestoreGroup(window);
        }
        else
        {
            _desktop.ActivateGroup(group, focusWindow: true);
        }
    }

    private void ShowNewObjectDialog()
    {
        bool defaultProgram = _desktop.ActiveGroup is { WindowState: not GroupWindowState.Minimized };
        using var dialog = new NewObjectDialog(defaultProgram);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (dialog.SelectedType == NewObjectType.ProgramGroup)
        {
            ShowGroupProperties(null);
        }
        else
        {
            var group = _desktop.ActiveGroup;
            if (group == null)
            {
                ClassicMessageBox.ShowError(this, "You must open a program group before adding a program item.");
                return;
            }
            if (group.WindowState == GroupWindowState.Minimized)
            {
                var window = _desktop.FindWindow(group);
                if (window != null)
                    _desktop.RestoreGroup(window);
            }
            ShowProgramItemProperties(null, group);
        }
    }

    private void ShowGroupProperties(ProgramGroup? group)
    {
        using var dialog = new GroupPropertiesDialog(group,
            group == null ? _store.NewGroupFileName("GROUP") : null);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string fileName = NormalizeGroupFileName(dialog.GroupFileValue, group);
        bool creating = group == null;
        if (creating)
        {
            var alreadyOpen = _groups.FirstOrDefault(g =>
                string.Equals(g.FileName, fileName, StringComparison.OrdinalIgnoreCase));
            if (alreadyOpen != null)
            {
                ActivateGroupFromMenu(alreadyOpen);
                return;
            }

            if (File.Exists(_store.ResolvePath(fileName)))
            {
                if (!ClassicMessageBox.Confirm(this,
                        $"The group file '{fileName}' already exists. Open it?",
                        "Program Manager"))
                    return;

                var loaded = _store.LoadGroupFile(fileName);
                if (loaded == null)
                {
                    ClassicMessageBox.ShowError(this,
                        $"The group file '{fileName}' could not be opened.", "Program Manager");
                    return;
                }
                _groups.Add(loaded);
                _settings.GroupFiles.Add(fileName);
                _desktop.AddGroup(loaded);
                _desktop.ActivateGroup(loaded, focusWindow: true);
                _store.SaveSettings(_settings);
                UpdateTitle();
                return;
            }

            var newGroup = new ProgramGroup
            {
                Name = dialog.GroupNameValue,
                FileName = fileName,
                WindowState = GroupWindowState.Normal,
                NormalBounds = NextGroupBounds(),
                IconLocation = new Point(ClassicTheme.S(24),
                    Math.Max(ClassicTheme.S(24), _desktop.ClientSize.Height - ClassicTheme.CellHeight - ClassicTheme.S(10)))
            };
            newGroup.NormalBounds = ClampGroupBounds(newGroup.NormalBounds);
            _groups.Add(newGroup);
            _settings.GroupFiles.Add(fileName);
            _desktop.AddGroup(newGroup);
            _desktop.ActivateGroup(newGroup, focusWindow: true);
            SaveGroup(newGroup);
            _store.SaveSettings(_settings);
            UpdateTitle();
            return;
        }

        group!.Name = dialog.GroupNameValue;
        string oldFileName = group.FileName;
        group.FileName = fileName;
        bool fileChanged = !string.Equals(oldFileName, fileName, StringComparison.OrdinalIgnoreCase);
        if (fileChanged && File.Exists(_store.ResolvePath(fileName)))
        {
            if (!ClassicMessageBox.Confirm(this,
                    $"The group file '{fileName}' already exists. Replace it?",
                    "Program Manager"))
            {
                group.FileName = oldFileName;
                return;
            }
        }

        if (fileChanged)
        {
            int index = _settings.GroupFiles.FindIndex(f =>
                string.Equals(f, oldFileName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                _settings.GroupFiles[index] = fileName;
            else
                _settings.GroupFiles.Add(fileName);
        }

        SaveGroup(group);
        if (fileChanged)
        {
            var old = new ProgramGroup { FileName = oldFileName };
            _store.DeleteGroupFileIfManaged(old);
        }
        var window = _desktop.FindWindow(group);
        window?.Invalidate();
        _store.SaveSettings(_settings);
        UpdateTitle();
        RebuildHotkeys();
    }

    private void ShowProgramItemProperties(ProgramItem? item, ProgramGroup group)
    {
        var reserved = AllShortcutKeysExcept(item);
        using var dialog = new ProgramItemPropertiesDialog(item, group.Name, reserved);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        ProgramItem target;
        if (item == null)
        {
            target = new ProgramItem();
            group.Items.Add(target);
        }
        else
        {
            target = item;
        }

        target.Name = dialog.ItemName;
        target.CommandLine = dialog.CommandLine;
        target.WorkingDirectory = dialog.WorkingDirectory;
        target.ShortcutKey = dialog.ShortcutKey;
        target.RunMinimized = dialog.RunMinimized;
        target.IconKey = dialog.IconKey;

        var window = _desktop.FindWindow(group);
        if (window != null)
        {
            window.UpdateLayout();
            window.SelectedIndex = group.Items.IndexOf(target);
            _desktop.ActivateGroup(group, focusWindow: true);
        }
        SaveGroup(group);
        RebuildHotkeys();
        UpdateMenuState();
    }

    private void ShowProperties()
    {
        var item = _desktop.SelectedItem;
        var group = _desktop.ActiveGroup;
        if (item != null && group != null)
            ShowProgramItemProperties(item, group);
        else if (group != null)
            ShowGroupProperties(group);
    }

    private void ShowMoveCopy(bool move)
    {
        var item = _desktop.SelectedItem;
        var from = _desktop.ActiveGroup;
        if (item == null || from == null)
            return;

        var targets = _groups.Where(g => g != from).ToList();
        if (targets.Count == 0)
        {
            ClassicMessageBox.ShowInfo(this, "There are no other program groups.", "Program Manager");
            return;
        }

        using var dialog = new MoveCopyDialog(item, from, targets, move);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.TargetGroup == null)
            return;

        var target = dialog.TargetGroup;
        ProgramItem moved;
        if (move)
        {
            from.Items.Remove(item);
            moved = item;
        }
        else
        {
            moved = item.Clone();
        }
        target.Items.Add(moved);
        moved.X = ClassicTheme.S(4);
        moved.Y = ClassicTheme.S(4) + (target.Items.Count - 1) * ClassicTheme.CellHeight;

        if (target.WindowState == GroupWindowState.Minimized)
        {
            var targetToRestore = _desktop.FindWindow(target);
            if (targetToRestore != null)
                _desktop.RestoreGroup(targetToRestore);
        }

        var sourceWindow = _desktop.FindWindow(from);
        sourceWindow?.ClearSelection();
        sourceWindow?.UpdateLayout();
        var targetWindow = _desktop.FindWindow(target);
        if (targetWindow != null)
        {
            targetWindow.UpdateLayout();
            targetWindow.SelectedIndex = target.Items.Count - 1;
        }
        _desktop.ActivateGroup(target, focusWindow: true);
        SaveGroup(from);
        SaveGroup(target);
        RebuildHotkeys();
        UpdateMenuState();
    }

    private void DeleteSelected()
    {
        var item = _desktop.SelectedItem;
        var group = _desktop.ActiveGroup;
        if (item != null && group != null)
        {
            if (!ClassicMessageBox.Confirm(this, $"Delete program '{item.Name}'?", "Program Manager"))
                return;
            group.Items.Remove(item);
            var window = _desktop.FindWindow(group);
            window?.ClearSelection();
            window?.UpdateLayout();
            SaveGroup(group);
            RebuildHotkeys();
            UpdateMenuState();
            return;
        }

        if (group != null)
        {
            if (!ClassicMessageBox.Confirm(this, $"Delete group '{group.Name}'?", "Program Manager"))
                return;
            _groups.Remove(group);
            _settings.GroupFiles.RemoveAll(f =>
                string.Equals(f, group.FileName, StringComparison.OrdinalIgnoreCase));
            _desktop.RemoveGroup(group);
            _store.DeleteGroupFileIfManaged(group);
            _store.SaveSettings(_settings);
            RebuildHotkeys();
            UpdateTitle();
            UpdateMenuState();
        }
    }

    private void ShowRunDialog()
    {
        using var dialog = new RunDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (!_launcher.TryLaunchCommand(dialog.CommandLine, string.Empty, dialog.RunMinimized, out string error))
        {
            ClassicMessageBox.ShowError(this, error, "Program Manager");
            return;
        }

        if (_settings.MinimizeOnUse)
            WindowState = FormWindowState.Minimized;
    }

    private void ShowAbout()
    {
        using var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }

    private void ShowHelp()
    {
        using var dialog = new HelpWindow();
        dialog.ShowDialog(this);
    }

    private void LaunchItem(ProgramItem item)
    {
        if (!_launcher.TryLaunch(item, out string error))
        {
            ClassicMessageBox.ShowError(this, error, "Program Manager");
            return;
        }
        if (_settings.MinimizeOnUse)
            WindowState = FormWindowState.Minimized;
    }

    private bool LaunchBuiltIn(string key)
    {
        switch (key.ToLowerInvariant())
        {
            case "about":
                ShowAbout();
                return true;
            case "help":
                ShowHelp();
                return true;
            case "clock":
                ShowBuiltIn("clock", () => new ClockWindow());
                return true;
            case "clipboard":
                ShowBuiltIn("clipboard", () => new ClipboardViewerWindow());
                return true;
            case "printmanager":
                ShowBuiltIn("printmanager", () => new PrintManagerWindow());
                return true;
            default:
                return false;
        }
    }

    private void ShowBuiltIn(string key, Func<ClassicForm> factory)
    {
        if (_builtInWindows.TryGetValue(key, out var existing) && !existing.IsDisposed)
        {
            if (existing.WindowState == FormWindowState.Minimized)
                existing.WindowState = FormWindowState.Normal;
            existing.Show();
            existing.BringToFront();
            existing.Activate();
            return;
        }

        var window = factory();
        _builtInWindows[key] = window;
        window.FormClosed += (_, _) => _builtInWindows.Remove(key);
        window.Show();
        window.BringToFront();
        window.Activate();
    }

    private void MinimizeActiveGroup()
    {
        var window = _desktop.ActiveWindow;
        if (window != null)
            _desktop.MinimizeGroup(window);
    }

    private void RunStartupGroup()
    {
        if (_startupRun)
            return;
        _startupRun = true;
        var group = _groups.FirstOrDefault(g =>
            string.Equals(g.Name, _settings.AutoStartGroup, StringComparison.OrdinalIgnoreCase));
        if (group == null)
            return;
        foreach (var item in group.Items.ToList())
            LaunchItem(item);
    }

    private void ShowWarnings()
    {
        if (_warningsShown)
            return;
        _warningsShown = true;
        if (_store.Warnings.Count == 0)
            return;
        ClassicMessageBox.ShowWarning(this, string.Join("\n\n", _store.Warnings), "Program Manager");
    }

    private void SaveGroup(ProgramGroup group)
    {
        _store.SaveGroup(group);
    }

    private void SaveLayoutIfEnabled()
    {
        if (!_settings.SaveSettingsOnExit)
            return;
        SaveMainBounds();
        _store.SaveSettings(_settings);
    }

    private void SaveAll()
    {
        foreach (var group in _groups)
            _store.SaveGroup(group);
        if (_settings.SaveSettingsOnExit)
        {
            SaveMainBounds();
            _store.SaveSettings(_settings);
        }
    }

    private void SaveMainBounds()
    {
        if (WindowState == FormWindowState.Normal)
            _settings.MainBounds = Bounds;
        _settings.MainMaximized = WindowState == FormWindowState.Maximized;
    }

    private bool ConfirmExit()
    {
        var result = ClassicMessageBox.Show(this, "This will end your Windows session.",
            "Exit Windows", ClassicMessageBoxButtons.OKCancel, ClassicMessageBoxIcon.Question);
        if (result != DialogResult.OK)
            return false;
        _exitConfirmed = true;
        return true;
    }

    private void ExitProgram()
    {
        if (!ConfirmExit())
            return;
        Close();
    }

    private void RebuildHotkeys()
    {
        UnregisterHotkeys();
        foreach (var group in _groups)
        {
            foreach (var item in group.Items)
            {
                if (!ShortcutKey.TryParse(item.ShortcutKey, out uint modifiers, out uint virtualKey))
                    continue;
                int id = _nextHotkeyId++;
                if (NativeMethods.RegisterHotKey(Handle, id, modifiers, virtualKey))
                    _hotkeys[id] = item;
            }
        }
    }

    private void UnregisterHotkeys()
    {
        if (Handle == IntPtr.Zero)
            return;
        foreach (int id in _hotkeys.Keys)
            NativeMethods.UnregisterHotKey(Handle, id);
        _hotkeys.Clear();
    }

    private List<string> AllShortcutKeysExcept(ProgramItem? exclude)
    {
        var result = new List<string>();
        foreach (var group in _groups)
        {
            foreach (var item in group.Items)
            {
                if (item == exclude || string.IsNullOrWhiteSpace(item.ShortcutKey))
                    continue;
                result.Add(item.ShortcutKey);
            }
        }
        return result;
    }

    private Rectangle NextGroupBounds()
    {
        int count = _groups.Count;
        int offset = ClassicTheme.S(28) * Math.Max(1, count % 5 + 1);
        var bounds = new Rectangle(offset, offset, ClassicTheme.S(500), ClassicTheme.S(330));
        return ClampGroupBounds(bounds);
    }

    private Rectangle ClampGroupBounds(Rectangle bounds)
    {
        int width = Math.Min(bounds.Width, Math.Max(ClassicTheme.S(200), _desktop.ClientSize.Width));
        int height = Math.Min(bounds.Height, Math.Max(ClassicTheme.S(140), _desktop.ClientSize.Height));
        int x = Math.Clamp(bounds.X, 0, Math.Max(0, _desktop.ClientSize.Width - width));
        int y = Math.Clamp(bounds.Y, 0, Math.Max(0, _desktop.ClientSize.Height - height));
        return new Rectangle(x, y, width, height);
    }

    private Rectangle ClampToScreen(Rectangle bounds)
    {
        var area = Screen.FromPoint(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2)).WorkingArea;
        int width = Math.Min(Math.Max(bounds.Width, ClassicTheme.S(640)), area.Width);
        int height = Math.Min(Math.Max(bounds.Height, ClassicTheme.S(400)), area.Height);
        int x = Math.Clamp(bounds.X, area.Left, Math.Max(area.Left, area.Right - width));
        int y = Math.Clamp(bounds.Y, area.Top, Math.Max(area.Top, area.Bottom - height));
        return new Rectangle(x, y, width, height);
    }

    private static string NormalizeGroupFileName(string value, ProgramGroup? group)
    {
        string name = value.Trim();
        if (name.Length == 0)
            name = group?.FileName ?? "GROUP.GRP";
        if (!Path.HasExtension(name))
            name += ".GRP";
        return name;
    }

    private static string EscapeAmpersands(string text) => text.Replace("&", "&&");

    private static string DefaultDataDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProgramManager31");
}
