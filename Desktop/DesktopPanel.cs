using System.Drawing.Drawing2D;
using ProgramManager31.Controls;
using ProgramManager31.Models;

namespace ProgramManager31.Desktop;

internal sealed class DesktopPanel : Control
{
    private readonly List<GroupWindow> _windows = new();
    private ProgramGroup? _activeGroup;
    private bool _autoArrange = true;

    private bool _iconDragCandidate;
    private bool _iconDragging;
    private ProgramGroup? _iconDragGroup;
    private Point _iconDragStart;
    private Point _iconStartLocation;

    private bool _itemDragging;
    private GroupWindow? _dragSource;
    private int _dragIndex;
    private ProgramItem? _dragItem;
    private Point _dragPoint;
    private ProgramGroup? _dragTarget;
    private bool _dragTargetIsIcon;
    private DragGhostForm? _dragGhost;
    private GroupWindow? _highlightWindow;

    public event EventHandler? SelectionChanged;
    public event EventHandler? ActiveGroupChanged;
    public event EventHandler? LayoutChanged;
    public event Action<ProgramGroup>? GroupChanged;
    public event Action<ProgramItem>? ItemLaunchRequested;

    public DesktopPanel()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.Desktop;
        TabStop = true;
    }

    public IReadOnlyList<GroupWindow> Windows => _windows;
    public ProgramGroup? ActiveGroup => _activeGroup;
    public GroupWindow? ActiveWindow => _activeGroup == null
        ? null
        : _windows.FirstOrDefault(w => w.Group == _activeGroup && w.Group.WindowState != GroupWindowState.Minimized);
    public ProgramGroup? MaximizedGroup => _windows.FirstOrDefault(w => w.Group.WindowState == GroupWindowState.Maximized)?.Group;
    public ProgramItem? SelectedItem => ActiveWindow?.SelectedItem;
    public bool IsItemDragging => _itemDragging;

    public bool AutoArrange
    {
        get => _autoArrange;
        set
        {
            if (_autoArrange == value)
                return;
            _autoArrange = value;
            foreach (var window in _windows)
                window.ApplyAutoArrangeChange(value);
            if (value)
                ArrangeGroupIcons();
            RefreshGroupVisibility();
            Invalidate();
        }
    }

    public void AddGroup(ProgramGroup group)
    {
        var window = new GroupWindow(group, this);
        _windows.Add(window);
        Controls.Add(window);
        window.SyncBoundsFromGroup();
        window.Visible = group.WindowState != GroupWindowState.Minimized;
        if (group.WindowState == GroupWindowState.Minimized)
            EnsureIconLocation(group);
    }

    public void RemoveGroup(ProgramGroup group)
    {
        var window = _windows.FirstOrDefault(w => w.Group == group);
        if (window != null)
        {
            _windows.Remove(window);
            window.Dispose();
        }
        if (_activeGroup == group)
        {
            _activeGroup = _windows.FirstOrDefault()?.Group;
            RaiseActiveGroupChanged();
        }
        RefreshGroupVisibility();
        Invalidate();
    }

    public GroupWindow? FindWindow(ProgramGroup group) => _windows.FirstOrDefault(w => w.Group == group);

    public void ActivateGroup(ProgramGroup group, bool focusWindow)
    {
        if (_activeGroup != group)
        {
            var maximized = MaximizedGroup;
            if (maximized != null && maximized != group)
            {
                var maxWindow = FindWindow(maximized);
                if (maxWindow != null)
                    RestoreGroup(maxWindow, activate: false);
            }
            _activeGroup = group;
            RaiseActiveGroupChanged();
        }

        var window = FindWindow(group);
        if (group.WindowState == GroupWindowState.Minimized)
        {
            if (focusWindow)
                Focus();
        }
        else if (window != null)
        {
            window.Visible = true;
            window.BringToFront();
            if (group.WindowState == GroupWindowState.Maximized)
                window.Bounds = ClientRectangle;
            if (focusWindow)
                window.Focus();
        }
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ActivateNextGroup()
    {
        if (_windows.Count == 0)
            return;
        int index = _activeGroup == null ? -1 : _windows.FindIndex(w => w.Group == _activeGroup);
        int next = (index + 1 + _windows.Count) % _windows.Count;
        var group = _windows[next].Group;
        if (group.WindowState == GroupWindowState.Minimized)
            RestoreGroup(_windows[next], activate: true);
        else
            ActivateGroup(group, focusWindow: true);
    }

    public void MinimizeGroup(GroupWindow window)
    {
        if (window.Group.WindowState != GroupWindowState.Maximized)
            window.Group.NormalBounds = window.Bounds;
        else
        {
            window.Visible = true;
        }

        window.Group.WindowState = GroupWindowState.Minimized;
        window.Visible = false;
        EnsureIconLocation(window.Group);
        _activeGroup = window.Group;
        RefreshGroupVisibility();
        if (AutoArrange)
            ArrangeGroupIcons();
        GroupChanged?.Invoke(window.Group);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        RaiseActiveGroupChanged();
        Invalidate();
    }

    public void MaximizeGroup(GroupWindow window)
    {
        if (window.Group.WindowState == GroupWindowState.Maximized)
            return;
        if (window.Group.WindowState == GroupWindowState.Normal)
            window.Group.NormalBounds = window.Bounds;
        window.Group.WindowState = GroupWindowState.Maximized;
        _activeGroup = window.Group;
        RefreshGroupVisibility();
        window.Visible = true;
        window.Bounds = ClientRectangle;
        window.BringToFront();
        window.Focus();
        GroupChanged?.Invoke(window.Group);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        RaiseActiveGroupChanged();
        Invalidate();
    }

    public void RestoreGroup(GroupWindow window, bool activate = true)
    {
        window.Group.WindowState = GroupWindowState.Normal;
        window.Visible = true;
        window.Bounds = ClampToDesktop(window.Group.NormalBounds);
        if (activate)
        {
            _activeGroup = window.Group;
            window.BringToFront();
            window.Focus();
            RaiseActiveGroupChanged();
        }
        RefreshGroupVisibility();
        GroupChanged?.Invoke(window.Group);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void ToggleMaximizeGroup(GroupWindow window)
    {
        if (window.Group.WindowState == GroupWindowState.Maximized)
            RestoreGroup(window);
        else
            MaximizeGroup(window);
    }

    public void NotifyGroupChanged(ProgramGroup group)
    {
        GroupChanged?.Invoke(group);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyGroupLayoutChanged()
    {
        if (_activeGroup != null)
            GroupChanged?.Invoke(_activeGroup);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void NotifySelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void LaunchItem(ProgramItem item)
    {
        ItemLaunchRequested?.Invoke(item);
    }

    public void CascadeWindows()
    {
        var open = _windows.Where(w => w.Group.WindowState != GroupWindowState.Minimized).ToList();
        if (open.Count == 0)
        {
            var first = _windows.FirstOrDefault();
            if (first == null)
                return;
            RestoreGroup(first);
            open.Add(first);
        }

        foreach (var window in open)
        {
            if (window.Group.WindowState == GroupWindowState.Maximized)
                window.Group.WindowState = GroupWindowState.Normal;
            window.Visible = true;
        }

        int areaWidth = ClientSize.Width;
        int areaHeight = ClientSize.Height;
        int offset = ClassicTheme.S(22);
        int maxOffset = Math.Max(0, Math.Min(offset * (open.Count - 1), Math.Max(0, areaHeight - ClassicTheme.S(140))));
        int step = open.Count > 1 ? maxOffset / (open.Count - 1) : 0;

        for (int i = 0; i < open.Count; i++)
        {
            var group = open[i].Group;
            int width = Math.Min(group.NormalBounds.Width, Math.Max(ClassicTheme.S(200), areaWidth - step * i));
            int height = Math.Min(group.NormalBounds.Height, Math.Max(ClassicTheme.S(140), areaHeight - step * i));
            int x = Math.Min(step * i, Math.Max(0, areaWidth - width));
            int y = Math.Min(step * i, Math.Max(0, areaHeight - height));
            var bounds = new Rectangle(x, y, width, height);
            group.WindowState = GroupWindowState.Normal;
            group.NormalBounds = bounds;
            open[i].Bounds = bounds;
            open[i].BringToFront();
        }

        RefreshGroupVisibility();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void TileWindows()
    {
        var open = _windows.Where(w => w.Group.WindowState != GroupWindowState.Minimized).ToList();
        if (open.Count == 0)
        {
            var first = _windows.FirstOrDefault();
            if (first == null)
                return;
            RestoreGroup(first);
            open.Add(first);
        }

        int count = open.Count;
        int width = Math.Max(ClassicTheme.S(160), ClientSize.Width / Math.Max(1, count));
        for (int i = 0; i < count; i++)
        {
            int x = i * width;
            int w = i == count - 1 ? Math.Max(ClassicTheme.S(160), ClientSize.Width - x) : width;
            var bounds = new Rectangle(x, 0, w, ClientSize.Height);
            var group = open[i].Group;
            group.WindowState = GroupWindowState.Normal;
            group.NormalBounds = bounds;
            open[i].Visible = true;
            open[i].Bounds = bounds;
            open[i].BringToFront();
        }

        RefreshGroupVisibility();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void ArrangeIcons()
    {
        var activeWindow = ActiveWindow;
        if (activeWindow != null)
        {
            activeWindow.ArrangeIcons();
            return;
        }
        ArrangeGroupIcons(moveFreeIcons: true);
    }

    public void BeginItemDrag(GroupWindow source, int index, Point screenPoint)
    {
        if (index < 0 || index >= source.Group.Items.Count)
            return;
        _itemDragging = true;
        _dragSource = source;
        _dragIndex = index;
        _dragItem = source.Group.Items[index];
        _dragPoint = PointToClient(screenPoint);
        _dragTarget = null;
        (_dragGhost ??= new DragGhostForm()).ShowItem(_dragItem.IconKey, screenPoint);
        UpdateItemDrag(screenPoint);
    }

    public void UpdateItemDrag(Point screenPoint)
    {
        if (!_itemDragging)
            return;
        _dragPoint = PointToClient(screenPoint);
        _dragTarget = null;
        _dragTargetIsIcon = false;
        _dragGhost?.MoveTo(screenPoint);

        var maximized = MaximizedGroup;
        if (maximized != null)
        {
            var window = FindWindow(maximized);
            if (window != null && window.Bounds.Contains(_dragPoint))
            {
                _dragTarget = maximized;
                _dragTargetIsIcon = false;
            }
        }
        else
        {
            foreach (var window in _windows.OrderBy(w => Controls.GetChildIndex(w)))
            {
                if (!window.Visible || window.Group.WindowState == GroupWindowState.Minimized)
                    continue;
                if (window.Bounds.Contains(_dragPoint))
                {
                    _dragTarget = window.Group;
                    _dragTargetIsIcon = false;
                    break;
                }
            }
        }

        if (_dragTarget == null)
        {
            var iconGroup = HitTestGroupIcon(_dragPoint);
            if (iconGroup != null)
            {
                _dragTarget = iconGroup;
                _dragTargetIsIcon = true;
            }
        }

        SetHighlightWindow(_dragTarget != null && !_dragTargetIsIcon ? FindWindow(_dragTarget) : null);
        Invalidate();
    }

    public void CancelItemDrag()
    {
        if (_itemDragging)
            ResetItemDrag();
    }

    public void CompleteItemDrag(Point screenPoint)
    {
        if (!_itemDragging)
            return;
        UpdateItemDrag(screenPoint);
        var source = _dragSource;
        var item = _dragItem;
        var target = _dragTarget;
        int index = _dragIndex;
        bool copy = (ModifierKeys & Keys.Control) != 0;
        ResetItemDrag();

        if (source == null || item == null || target == null)
            return;

        if (target == source.Group)
        {
            if (!AutoArrange)
            {
                var window = FindWindow(target);
                if (window != null)
                {
                    var point = PointToClient(screenPoint);
                    var local = new Point(point.X - window.Left, point.Y - window.Top);
                    item.X = Math.Max(4, local.X - ClassicTheme.CellWidth / 2);
                    item.Y = Math.Max(4, local.Y - ClassicTheme.CellHeight / 2);
                }
                source.SelectedIndex = index;
                GroupChanged?.Invoke(target);
                source.UpdateLayout();
            }
            Invalidate();
            return;
        }

        var targetWindow = FindWindow(target);
        if (targetWindow == null)
            return;

        ProgramItem newItem;
        if (copy)
        {
            newItem = item.Clone();
        }
        else
        {
            if (index >= 0 && index < source.Group.Items.Count && source.Group.Items[index] == item)
                source.Group.Items.RemoveAt(index);
            newItem = item;
            if (source.SelectedIndex == index)
                source.ClearSelection();
        }

        if (!AutoArrange && !_dragTargetIsIcon)
        {
            var point = PointToClient(screenPoint);
            var local = new Point(point.X - targetWindow.Left, point.Y - targetWindow.Top);
            newItem.X = Math.Max(4, local.X - ClassicTheme.CellWidth / 2);
            newItem.Y = Math.Max(4, local.Y - ClassicTheme.CellHeight / 2);
        }
        else
        {
            newItem.X = 4;
            newItem.Y = 4 + target.Items.Count * ClassicTheme.CellHeight;
        }

        target.Items.Add(newItem);
        targetWindow.SelectedIndex = target.Items.Count - 1;
        targetWindow.UpdateLayout();
        source.UpdateLayout();
        ActivateGroup(target, focusWindow: true);
        GroupChanged?.Invoke(source.Group);
        GroupChanged?.Invoke(target);
        Invalidate();
    }

    public void RefreshGroupVisibility()
    {
        var maximized = MaximizedGroup;
        foreach (var window in _windows)
        {
            if (maximized != null)
            {
                window.Visible = window.Group == maximized;
                if (window.Group == maximized)
                    window.Bounds = ClientRectangle;
            }
            else
            {
                window.Visible = window.Group.WindowState != GroupWindowState.Minimized;
                if (window.Visible)
                    window.Bounds = ClampToDesktop(window.Group.NormalBounds);
            }
            window.UpdateLayout();
        }
        if (AutoArrange)
            ArrangeGroupIcons();
        Invalidate();
    }

    public void ArrangeGroupIcons(bool moveFreeIcons = false)
    {
        var minimized = _windows.Where(w => w.Group.WindowState == GroupWindowState.Minimized).ToList();
        if (minimized.Count == 0)
            return;

        if (AutoArrange || moveFreeIcons)
        {
            int cellWidth = ClassicTheme.CellWidth;
            int cellHeight = ClassicTheme.CellHeight + ClassicTheme.S(2);
            int x = ClassicTheme.S(8);
            int y = ClientSize.Height - cellHeight - ClassicTheme.S(8);
            foreach (var window in minimized)
            {
                if (x + cellWidth > ClientSize.Width - ClassicTheme.S(8) && x > ClassicTheme.S(8))
                {
                    x = ClassicTheme.S(8);
                    y -= cellHeight;
                }
                window.Group.IconLocation = new Point(x, Math.Max(ClassicTheme.S(8), y));
                x += cellWidth;
            }
        }
        else
        {
            foreach (var window in minimized)
                EnsureIconLocation(window.Group);
        }
        Invalidate();
    }

    public Rectangle GetGroupIconRect(ProgramGroup group)
    {
        int cellWidth = ClassicTheme.CellWidth;
        int cellHeight = ClassicTheme.CellHeight + ClassicTheme.S(2);
        return new Rectangle(group.IconLocation.X, group.IconLocation.Y, cellWidth, cellHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.Desktop);

        if (MaximizedGroup == null)
        {
            foreach (var window in _windows)
            {
                if (window.Group.WindowState != GroupWindowState.Minimized)
                    continue;
                var rect = GetGroupIconRect(window.Group);
                window.DrawGroupIcon(g, rect, window.Group == _activeGroup);
            }
        }

        if (_itemDragging && _dragTarget != null && _dragTargetIsIcon)
        {
            var highlight = GetGroupIconRect(_dragTarget);
            using var pen = new Pen(ClassicTheme.Highlight, Math.Max(1, ClassicTheme.S(2)))
            {
                DashStyle = DashStyle.Dash
            };
            g.DrawRectangle(pen, Rectangle.Inflate(highlight, -2, -2));
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var group = HitTestGroupIcon(e.Location);
            if (group != null)
            {
                ActivateGroup(group, focusWindow: false);
                _iconDragCandidate = true;
                _iconDragging = false;
                _iconDragGroup = group;
                _iconDragStart = e.Location;
                _iconStartLocation = group.IconLocation;
                Capture = true;
                Invalidate();
                base.OnMouseDown(e);
                return;
            }

            _activeGroup = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            ActiveGroupChanged?.Invoke(this, EventArgs.Empty);
            Focus();
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_iconDragCandidate && e.Button == MouseButtons.Left && _iconDragGroup != null)
        {
            if (!_iconDragging &&
                (Math.Abs(e.X - _iconDragStart.X) > ClassicTheme.S(3) ||
                 Math.Abs(e.Y - _iconDragStart.Y) > ClassicTheme.S(3)))
                _iconDragging = true;

            if (_iconDragging && !AutoArrange)
            {
                int dx = e.X - _iconDragStart.X;
                int dy = e.Y - _iconDragStart.Y;
                var cell = GetGroupIconRect(_iconDragGroup);
                int x = Math.Clamp(_iconStartLocation.X + dx, 0, Math.Max(0, ClientSize.Width - cell.Width));
                int y = Math.Clamp(_iconStartLocation.Y + dy, 0, Math.Max(0, ClientSize.Height - cell.Height));
                _iconDragGroup.IconLocation = new Point(x, y);
                Invalidate();
            }
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_iconDragging && _iconDragGroup != null)
            {
                GroupChanged?.Invoke(_iconDragGroup);
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
            _iconDragCandidate = false;
            _iconDragging = false;
            _iconDragGroup = null;
            Capture = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnDoubleClick(EventArgs e)
    {
        var group = HitTestGroupIcon(PointToClient(Cursor.Position));
        if (group != null)
        {
            var window = FindWindow(group);
            if (window != null)
                RestoreGroup(window);
        }
        base.OnDoubleClick(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (MaximizedGroup != null)
        {
            var window = FindWindow(MaximizedGroup);
            if (window != null)
                window.Bounds = ClientRectangle;
        }
        else
        {
            foreach (var window in _windows)
            {
                if (window.Visible)
                    window.Bounds = ClampToDesktop(window.Group.NormalBounds);
            }
        }
        if (AutoArrange)
            ArrangeGroupIcons();
        Invalidate();
    }

    private void ResetItemDrag()
    {
        _itemDragging = false;
        _dragSource = null;
        _dragIndex = -1;
        _dragItem = null;
        _dragTarget = null;
        _dragTargetIsIcon = false;
        _dragGhost?.Hide();
        SetHighlightWindow(null);
        Invalidate();
    }

    private void SetHighlightWindow(GroupWindow? window)
    {
        if (_highlightWindow == window)
            return;
        if (_highlightWindow != null)
            _highlightWindow.DropHighlight = false;
        _highlightWindow = window;
        if (_highlightWindow != null)
            _highlightWindow.DropHighlight = true;
    }

    private ProgramGroup? HitTestGroupIcon(Point point)
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            var window = _windows[i];
            if (window.Group.WindowState != GroupWindowState.Minimized)
                continue;
            if (GetGroupIconRect(window.Group).Contains(point))
                return window.Group;
        }
        return null;
    }

    private void EnsureIconLocation(ProgramGroup group)
    {
        var rect = GetGroupIconRect(group);
        if (rect.Right > ClientSize.Width || rect.Bottom > ClientSize.Height || rect.X < 0 || rect.Y < 0)
        {
            int cellHeight = ClassicTheme.CellHeight + ClassicTheme.S(2);
            int index = _windows.Count(w => w.Group.WindowState == GroupWindowState.Minimized);
            int x = ClassicTheme.S(8) + index * ClassicTheme.CellWidth;
            int y = ClientSize.Height - cellHeight - ClassicTheme.S(8);
            if (x + ClassicTheme.CellWidth > ClientSize.Width)
            {
                x = ClassicTheme.S(8);
                y = Math.Max(ClassicTheme.S(8), y - cellHeight);
            }
            group.IconLocation = new Point(x, y);
        }
    }

    private Rectangle ClampToDesktop(Rectangle bounds)
    {
        int minWidth = ClassicTheme.S(160);
        int minHeight = ClassicTheme.S(100);
        bounds.Width = Math.Clamp(bounds.Width, minWidth, Math.Max(minWidth, ClientSize.Width));
        bounds.Height = Math.Clamp(bounds.Height, minHeight, Math.Max(minHeight, ClientSize.Height));
        bounds.X = Math.Clamp(bounds.X, 0, Math.Max(0, ClientSize.Width - bounds.Width));
        bounds.Y = Math.Clamp(bounds.Y, 0, Math.Max(0, ClientSize.Height - bounds.Height));
        return bounds;
    }

    private void RaiseActiveGroupChanged()
    {
        ActiveGroupChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _dragGhost?.Dispose();
        base.Dispose(disposing);
    }
}
