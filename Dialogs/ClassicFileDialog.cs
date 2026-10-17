using ProgramManager31.Controls;

namespace ProgramManager31.Dialogs;

internal sealed class ClassicFileDialog : ClassicDialog
{
    private static string _lastDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    private readonly ClassicTextBox _pathBox;
    private readonly ClassicListBox _directoryList;
    private readonly ClassicListBox _fileList;
    private readonly ClassicComboBox _filterBox;
    private readonly List<string[]> _filterPatterns = new();
    private string _currentDirectory;
    private string[] _patterns = { "*.*" };

    public string? SelectedPath { get; private set; }

    public ClassicFileDialog(string title, string filter)
    {
        Text = title;
        SetContentSize(ClassicTheme.S(540), ClassicTheme.S(330));
        _currentDirectory = Directory.Exists(_lastDirectory) ? _lastDirectory : Environment.CurrentDirectory;

        var directoriesLabel = new ClassicLabel
        {
            Text = "&Directories:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(18), ClassicTheme.S(120), ClassicTheme.S(16))
        };
        _directoryList = new ClassicListBox
        {
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(38), ClassicTheme.S(210), ClassicTheme.S(190)),
            TabIndex = 0
        };

        var filesLabel = new ClassicLabel
        {
            Text = "&Files:",
            Bounds = new Rectangle(ClassicTheme.S(232), ClassicTheme.S(18), ClassicTheme.S(120), ClassicTheme.S(16))
        };
        directoriesLabel.Target = _directoryList;
        filesLabel.Target = _fileList;
        _fileList = new ClassicListBox
        {
            Bounds = new Rectangle(ClassicTheme.S(232), ClassicTheme.S(38), ClassicTheme.S(296), ClassicTheme.S(190)),
            TabIndex = 1
        };

        var pathLabel = new ClassicLabel
        {
            Text = "File &name:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(242), ClassicTheme.S(72), ClassicTheme.S(16))
        };
        _pathBox = new ClassicTextBox
        {
            Bounds = new Rectangle(ClassicTheme.S(88), ClassicTheme.S(240), ClassicTheme.S(300), ClassicTheme.S(21)),
            Text = _currentDirectory,
            TabIndex = 2
        };

        var filterLabel = new ClassicLabel
        {
            Text = "List Files of &Type:",
            Bounds = new Rectangle(ClassicTheme.S(12), ClassicTheme.S(272), ClassicTheme.S(110), ClassicTheme.S(16))
        };
        pathLabel.Target = _pathBox;
        filterLabel.Target = _filterBox;
        _filterBox = new ClassicComboBox
        {
            Bounds = new Rectangle(ClassicTheme.S(128), ClassicTheme.S(270), ClassicTheme.S(260), ClassicTheme.S(21)),
            TabIndex = 3
        };

        ParseFilter(filter);
        _filterBox.SetItems(_filterPatterns.Select((_, i) => _filterDescriptions[i]));
        _filterBox.SelectedIndex = 0;
        _filterBox.SelectedIndexChanged += (_, _) =>
        {
            if (_filterBox.SelectedIndex >= 0 && _filterBox.SelectedIndex < _filterPatterns.Count)
            {
                _patterns = _filterPatterns[_filterBox.SelectedIndex];
                RefreshFiles();
            }
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(452), ClassicTheme.S(8), ClassicTheme.S(76), ClassicTheme.ButtonHeight),
            TabIndex = 4
        };
        var cancel = new ClassicButton
        {
            Text = "Cancel",
            Bounds = new Rectangle(ClassicTheme.S(452), ClassicTheme.S(32), ClassicTheme.S(76), ClassicTheme.ButtonHeight),
            TabIndex = 5
        };
        var help = new ClassicButton
        {
            Text = "&Help",
            Bounds = new Rectangle(ClassicTheme.S(452), ClassicTheme.S(56), ClassicTheme.S(76), ClassicTheme.ButtonHeight),
            TabIndex = 6
        };

        ok.Click += (_, _) => Accept();
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        help.Click += (_, _) => ClassicMessageBox.ShowInfo(this,
            "Choose a folder from the Directories list and a file from the Files list, " +
            "or type a path in the File name box.",
            Text);

        _directoryList.DoubleClick += (_, _) => NavigateSelectedDirectory();
        _fileList.DoubleClick += (_, _) =>
        {
            if (_fileList.SelectedItem != null)
            {
                _pathBox.Text = _fileList.SelectedItem.ToString() ?? string.Empty;
                Accept();
            }
        };
        _fileList.SelectedIndexChanged += (_, _) =>
        {
            if (_fileList.SelectedItem != null)
                _pathBox.Text = _fileList.SelectedItem.ToString() ?? string.Empty;
        };
        _pathBox.Inner.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Accept();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        Body.Controls.Add(directoriesLabel);
        Body.Controls.Add(_directoryList);
        Body.Controls.Add(filesLabel);
        Body.Controls.Add(_fileList);
        Body.Controls.Add(pathLabel);
        Body.Controls.Add(_pathBox);
        Body.Controls.Add(filterLabel);
        Body.Controls.Add(_filterBox);
        Body.Controls.Add(ok);
        Body.Controls.Add(cancel);
        Body.Controls.Add(help);
        DefaultButton = ok;
        CancelButton = cancel;

        RefreshLists();
    }

    private readonly List<string> _filterDescriptions = new();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _fileList.Inner.Focus();
    }

    private void ParseFilter(string filter)
    {
        var parts = filter.Split('|');
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            _filterDescriptions.Add(parts[i]);
            var patterns = parts[i + 1]
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            _filterPatterns.Add(patterns.Length == 0 ? new[] { "*.*" } : patterns);
        }
        if (_filterPatterns.Count == 0)
        {
            _filterDescriptions.Add("All files (*.*)");
            _filterPatterns.Add(new[] { "*.*" });
        }
    }

    private void RefreshLists()
    {
        _directoryList.Items.Clear();
        _fileList.Items.Clear();
        _pathBox.Text = _currentDirectory;

        try
        {
            var parent = Directory.GetParent(_currentDirectory);
            if (parent != null)
                _directoryList.Items.Add("..");

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                    _directoryList.Items.Add($"[{drive.Name[..2]}]");
            }

            foreach (var directory in Directory.GetDirectories(_currentDirectory)
                         .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                _directoryList.Items.Add(Path.GetFileName(directory));
            }
        }
        catch (Exception ex)
        {
            ClassicMessageBox.ShowWarning(this, $"The folder could not be listed:\n{ex.Message}", Text);
        }

        RefreshFiles();
    }

    private void RefreshFiles()
    {
        _fileList.Items.Clear();
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in _patterns)
        {
            try
            {
                foreach (var file in Directory.GetFiles(_currentDirectory, pattern))
                    files.Add(Path.GetFileName(file));
            }
            catch
            {
                // Ignore patterns the file system rejects.
            }
        }

        foreach (var file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            _fileList.Items.Add(file);
    }

    private void NavigateSelectedDirectory()
    {
        if (_directoryList.SelectedItem is not string selected)
            return;

        string target;
        if (selected == "..")
        {
            var parent = Directory.GetParent(_currentDirectory);
            if (parent == null)
                return;
            target = parent.FullName;
        }
        else if (selected.StartsWith('[') && selected.EndsWith(']'))
        {
            target = selected[1..^1] + Path.DirectorySeparatorChar;
        }
        else
        {
            target = Path.Combine(_currentDirectory, selected);
        }

        Navigate(target);
    }

    private void Navigate(string directory)
    {
        if (!Directory.Exists(directory))
            return;
        _currentDirectory = Path.GetFullPath(directory);
        _lastDirectory = _currentDirectory;
        RefreshLists();
        _fileList.Inner.Focus();
    }

    private void Accept()
    {
        string text = _pathBox.Text.Trim();
        if (text.Length == 0)
            return;

        string candidate;
        if (Path.IsPathRooted(text))
            candidate = text;
        else
            candidate = Path.Combine(_currentDirectory, text);

        if (Directory.Exists(candidate))
        {
            Navigate(candidate);
            return;
        }

        if (!File.Exists(candidate))
        {
            ClassicMessageBox.ShowWarning(this, $"'{text}' was not found.", Text);
            _pathBox.Focus();
            _pathBox.SelectAll();
            return;
        }

        SelectedPath = Path.GetFullPath(candidate);
        _lastDirectory = Path.GetDirectoryName(SelectedPath) ?? _lastDirectory;
        DialogResult = DialogResult.OK;
        Close();
    }
}
