using System.Drawing.Drawing2D;
using ProgramManager31.Controls;
using ProgramManager31.Icons;
using ProgramManager31.Models;

namespace ProgramManager31.Desktop;

internal sealed class GroupWindow : Control
{
    private enum DragMode
    {
        None,
        Move,
        Resize,
        ItemCandidate,
        ItemDrag,
        KeyboardMove,
        KeyboardSize
    }

    private readonly DesktopPanel _desktop;
    private readonly ClassicScrollBar _vScroll;
    private readonly ClassicScrollBar _hScroll;
    private List<ItemLayout> _layout = new();
    private Size _contentSize;
    private Rectangle _itemArea;
    private int _scrollX;
    private int _scrollY;
    private int _selectedIndex = -1;
    private DragMode _dragMode;
    private Point _dragStartScreen;
    private Rectangle _dragStartBounds;
    private Rectangle _keyboardStartBounds;
    private int _resizeDirection;
    private bool _systemPressed;
    private bool _minPressed;
    private bool _maxPressed;
    private bool _dropHighlight;
    private ClassicMenuDropDown? _systemMenu;

    public ProgramGroup Group { get; }

    public bool DropHighlight
    {
        get => _dropHighlight;
        set
        {
            if (_dropHighlight == value)
                return;
            _dropHighlight = value;
            Invalidate();
        }
    }

    public GroupWindow(ProgramGroup group, DesktopPanel desktop)
    {
        Group = group;
        _desktop = desktop;
        SetStyle(ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = ClassicTheme.Window;
        Font = ClassicTheme.UI;
        TabStop = true;

        _vScroll = new ClassicScrollBar { Orientation = Orientation.Vertical, Visible = false };
        _hScroll = new ClassicScrollBar { Orientation = Orientation.Horizontal, Visible = false };
        _vScroll.ValueChanged += (_, _) =>
        {
            _scrollY = _vScroll.Value;
            Invalidate();
        };
        _hScroll.ValueChanged += (_, _) =>
        {
            _scrollX = _hScroll.Value;
            Invalidate();
        };
        Controls.Add(_vScroll);
        Controls.Add(_hScroll);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = value >= 0 && value < Group.Items.Count ? value : -1;
            if (_selectedIndex == clamped)
                return;
            _selectedIndex = clamped;
            Invalidate();
            _desktop.NotifySelectionChanged();
        }
    }

    public ProgramItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Group.Items.Count
        ? Group.Items[_selectedIndex]
        : null;

    public bool IsActive
    {
        get
        {
            if (_desktop.ActiveGroup != Group)
                return false;
            var form = FindForm();
            return form != null && (Form.ActiveForm == form || ClassicMenuDropDown.HasOpenMenuFor(form));
        }
    }

    public bool IsMaximized => Group.WindowState == GroupWindowState.Maximized;

    private Rectangle ContentArea
    {
        get
        {
            if (IsMaximized)
                return ClientRectangle;
            int x = ClassicTheme.Frame;
            int y = ClassicTheme.Frame + ClassicTheme.TitleBar + ClassicTheme.S(1);
            int width = Math.Max(0, Width - ClassicTheme.Frame * 2);
            int height = Math.Max(0, Height - ClassicTheme.Frame * 2 - ClassicTheme.TitleBar - ClassicTheme.S(1));
            return new Rectangle(x, y, width, height);
        }
    }

    private Rectangle SystemBoxRect => new(ClassicTheme.Frame + ClassicTheme.S(1), ClassicTheme.Frame + ClassicTheme.S(1),
        ClassicTheme.SystemBox, ClassicTheme.TitleBar - ClassicTheme.S(2));

    private Rectangle MinButtonRect
    {
        get
        {
            int right = Width - ClassicTheme.Frame;
            return new Rectangle(right - ClassicTheme.S(1) - ClassicTheme.TitleButton * 2,
                ClassicTheme.Frame, ClassicTheme.TitleButton, ClassicTheme.TitleBar);
        }
    }

    private Rectangle MaxButtonRect
    {
        get
        {
            int right = Width - ClassicTheme.Frame;
            return new Rectangle(right - ClassicTheme.TitleButton, ClassicTheme.Frame,
                ClassicTheme.TitleButton, ClassicTheme.TitleBar);
        }
    }

    public void ClearSelection()
    {
        if (_selectedIndex == -1)
            return;
        _selectedIndex = -1;
        Invalidate();
        _desktop.NotifySelectionChanged();
    }

    public void SelectFirstItem()
    {
        if (Group.Items.Count > 0)
            SelectedIndex = 0;
    }

    public void ApplyAutoArrangeChange(bool autoArrange)
    {
        if (!autoArrange)
            GroupLayout.ArrangeItems(Group, _itemArea.Height);
        _scrollX = 0;
        _scrollY = 0;
        UpdateLayout();
        Invalidate();
    }

    public void ArrangeIcons()
    {
        GroupLayout.ArrangeItems(Group, _itemArea.Height);
        _vScroll.Value = 0;
        _hScroll.Value = 0;
        _scrollX = _scrollY = 0;
        UpdateLayout();
        _desktop.NotifyGroupChanged(Group);
        Invalidate();
    }

    public void SyncBoundsFromGroup()
    {
        if (Group.WindowState == GroupWindowState.Maximized)
        {
            Bounds = _desktop.ClientRectangle;
        }
        else
        {
            Bounds = ClampBounds(Group.NormalBounds);
        }
        UpdateLayout();
    }

    public void DrawGroupIcon(Graphics g, Rectangle cell, bool selected)
    {
        var iconRect = new Rectangle(cell.Left + (cell.Width - ClassicTheme.IconSize) / 2,
            cell.Top + ClassicTheme.S(2), ClassicTheme.IconSize, ClassicTheme.IconSize);
        IconPainter.DrawIcon(g, "group", iconRect);

        var labelRect = new Rectangle(cell.Left + ClassicTheme.S(1), iconRect.Bottom + ClassicTheme.S(2),
            cell.Width - ClassicTheme.S(2), ClassicTheme.LabelHeight + ClassicTheme.S(2));
        if (selected)
        {
            using var brush = new SolidBrush(ClassicTheme.Highlight);
            g.FillRectangle(brush, labelRect);
        }
        TextRenderer.DrawText(g, Group.Name, Font, labelRect,
            selected ? ClassicTheme.HighlightText : ClassicTheme.Black,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(ClassicTheme.Face);

        if (!IsMaximized)
        {
            ClassicTheme.DrawWindowFrame(g, ClientRectangle);
            DrawTitleBar(g);
        }

        var content = ContentArea;
        using (var white = new SolidBrush(ClassicTheme.Window))
            g.FillRectangle(white, content);

        DrawItems(g);

        if (_vScroll.Visible && _hScroll.Visible)
        {
            var corner = new Rectangle(_vScroll.Left, _hScroll.Top, _vScroll.Width, _hScroll.Height);
            using var face = new SolidBrush(ClassicTheme.Face);
            g.FillRectangle(face, corner);
            ClassicTheme.DrawRaisedBorder(g, corner);
        }

        if (_dropHighlight)
        {
            using var pen = new Pen(ClassicTheme.Highlight, Math.Max(1, ClassicTheme.S(2)))
            {
                DashStyle = DashStyle.Dash
            };
            g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
        }
    }

    private void DrawTitleBar(Graphics g)
    {
        int left = ClassicTheme.Frame;
        int right = Width - ClassicTheme.Frame;
        int top = ClassicTheme.Frame;
        int height = ClassicTheme.TitleBar;

        using (var face = new SolidBrush(ClassicTheme.Face))
            g.FillRectangle(face, left, top, Math.Max(0, right - left), height);

        bool active = IsActive;
        var titleColor = active ? ClassicTheme.ActiveTitle : ClassicTheme.InactiveTitle;
        var titleTextColor = active ? ClassicTheme.ActiveTitleText : ClassicTheme.InactiveTitleText;

        int systemWidth = ClassicTheme.SystemBox + ClassicTheme.S(2);
        int blueLeft = left + systemWidth + ClassicTheme.S(1);
        int blueRight = MinButtonRect.Left - ClassicTheme.S(1);

        using (var black = new Pen(ClassicTheme.Black))
        {
            g.DrawLine(black, left, top + height, right - 1, top + height);
            g.DrawLine(black, left + systemWidth, top, left + systemWidth, top + height - 1);
            g.DrawLine(black, MinButtonRect.Left - 1, top, MinButtonRect.Left - 1, top + height - 1);
            g.DrawLine(black, MaxButtonRect.Left - 1, top, MaxButtonRect.Left - 1, top + height - 1);
        }

        ClassicTheme.DrawSystemBox(g, SystemBoxRect, _systemPressed);

        var blueRect = new Rectangle(blueLeft, top, Math.Max(0, blueRight - blueLeft), height);
        using (var brush = new SolidBrush(titleColor))
            g.FillRectangle(brush, blueRect);
        if (blueRect.Width > 0)
        {
            TextRenderer.DrawText(g, Group.Name, ClassicTheme.Title, blueRect, titleTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        ClassicTheme.DrawTitleButton(g, MinButtonRect, _minPressed, TitleButtonGlyph.Minimize);
        ClassicTheme.DrawTitleButton(g, MaxButtonRect, _maxPressed,
            IsMaximized ? TitleButtonGlyph.Restore : TitleButtonGlyph.Maximize);
    }

    private void DrawItems(Graphics g)
    {
        var state = g.Save();
        g.SetClip(_itemArea);
        try
        {
            foreach (var layout in _layout)
            {
                var iconRect = new Rectangle(_itemArea.Left + layout.Icon.X, _itemArea.Top + layout.Icon.Y,
                    layout.Icon.Width, layout.Icon.Height);
                IconPainter.DrawIcon(g, layout.Item.IconKey, iconRect);

                var labelRect = new Rectangle(_itemArea.Left + layout.Label.X, _itemArea.Top + layout.Label.Y,
                    layout.Label.Width, layout.Label.Height + ClassicTheme.S(2));
                bool selected = IsActive && layout.Item == SelectedItem;
                if (selected)
                {
                    using var brush = new SolidBrush(ClassicTheme.Highlight);
                    g.FillRectangle(brush, labelRect);
                }
                TextRenderer.DrawText(g, layout.Item.Name, Font, labelRect,
                    selected ? ClassicTheme.HighlightText : ClassicTheme.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                if (selected)
                    ClassicTheme.DrawFocusRect(g, labelRect);
            }
        }
        finally
        {
            g.Restore(state);
        }
    }

    public void UpdateLayout()
    {
        if (IsDisposed)
            return;

        var content = ContentArea;
        int scrollSize = ClassicTheme.ScrollBar;
        bool needV = false;
        bool needH = false;
        Size contentSize = Size.Empty;
        List<ItemLayout> layouts = new();
        Rectangle itemArea = content;

        for (int pass = 0; pass < 4; pass++)
        {
            itemArea = new Rectangle(content.Left, content.Top,
                Math.Max(0, content.Width - (needV ? scrollSize : 0)),
                Math.Max(0, content.Height - (needH ? scrollSize : 0)));
            var result = GroupLayout.Compute(Group, itemArea.Size, _desktop.AutoArrange, new Point(_scrollX, _scrollY));
            layouts = result.Items;
            contentSize = result.ContentSize;
            bool newV = contentSize.Height > itemArea.Height;
            bool newH = contentSize.Width > itemArea.Width;
            if (newV == needV && newH == needH)
                break;
            needV = newV;
            needH = newH;
        }

        _layout = layouts;
        _contentSize = contentSize;
        _itemArea = itemArea;

        _vScroll.Visible = needV;
        _hScroll.Visible = needH;
        _vScroll.SetRange(Math.Max(1, contentSize.Height), Math.Max(1, itemArea.Height));
        _hScroll.SetRange(Math.Max(1, contentSize.Width), Math.Max(1, itemArea.Width));

        if (!needV)
            _scrollY = 0;
        if (!needH)
            _scrollX = 0;
        _vScroll.Value = Math.Clamp(_scrollY, 0, Math.Max(0, contentSize.Height - Math.Max(1, itemArea.Height)));
        _hScroll.Value = Math.Clamp(_scrollX, 0, Math.Max(0, contentSize.Width - Math.Max(1, itemArea.Width)));
        if (_scrollY != _vScroll.Value || _scrollX != _hScroll.Value)
        {
            _scrollY = _vScroll.Value;
            _scrollX = _hScroll.Value;
            var adjusted = GroupLayout.Compute(Group, itemArea.Size, _desktop.AutoArrange, new Point(_scrollX, _scrollY));
            _layout = adjusted.Items;
        }

        _vScroll.Bounds = new Rectangle(content.Right - scrollSize, content.Top, scrollSize,
            Math.Max(0, content.Height - (needH ? scrollSize : 0)));
        _hScroll.Bounds = new Rectangle(content.Left, content.Bottom - scrollSize,
            Math.Max(0, content.Width - (needV ? scrollSize : 0)), scrollSize);

        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayout();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            base.OnMouseDown(e);
            return;
        }

        _desktop.ActivateGroup(Group, focusWindow: true);
        Focus();

        if (!IsMaximized)
        {
            var title = new Rectangle(ClassicTheme.Frame, ClassicTheme.Frame,
                Width - ClassicTheme.Frame * 2, ClassicTheme.TitleBar);
            if (SystemBoxRect.Contains(e.Location))
            {
                _systemPressed = true;
                Capture = true;
                Invalidate();
                base.OnMouseDown(e);
                return;
            }
            if (MinButtonRect.Contains(e.Location))
            {
                _minPressed = true;
                Capture = true;
                Invalidate();
                base.OnMouseDown(e);
                return;
            }
            if (MaxButtonRect.Contains(e.Location))
            {
                _maxPressed = true;
                Capture = true;
                Invalidate();
                base.OnMouseDown(e);
                return;
            }
            if (title.Contains(e.Location))
            {
                _dragMode = DragMode.Move;
                _dragStartScreen = PointToScreen(e.Location);
                _dragStartBounds = Bounds;
                Capture = true;
                base.OnMouseDown(e);
                return;
            }

            int direction = FrameHitTest(e.Location);
            if (direction != 0)
            {
                _dragMode = DragMode.Resize;
                _resizeDirection = direction;
                _dragStartScreen = PointToScreen(e.Location);
                _dragStartBounds = Bounds;
                Capture = true;
                base.OnMouseDown(e);
                return;
            }
        }

        if (ContentArea.Contains(e.Location))
        {
            var point = new Point(e.Location.X - _itemArea.Left, e.Location.Y - _itemArea.Top);
            int index = GroupLayout.HitTest(_layout, point);
            if (index >= 0)
            {
                SelectedIndex = index;
                _dragMode = DragMode.ItemCandidate;
                _dragStartScreen = PointToScreen(e.Location);
            }
            else
            {
                ClearSelection();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragMode == DragMode.Move)
        {
            var screen = PointToScreen(e.Location);
            int dx = screen.X - _dragStartScreen.X;
            int dy = screen.Y - _dragStartScreen.Y;
            var bounds = new Rectangle(_dragStartBounds.X + dx, _dragStartBounds.Y + dy,
                _dragStartBounds.Width, _dragStartBounds.Height);
            Bounds = ClampBounds(bounds);
        }
        else if (_dragMode == DragMode.Resize)
        {
            var screen = PointToScreen(e.Location);
            Bounds = ResizeBounds(_dragStartBounds, _resizeDirection,
                screen.X - _dragStartScreen.X, screen.Y - _dragStartScreen.Y);
        }
        else if (_dragMode == DragMode.ItemCandidate && e.Button == MouseButtons.Left)
        {
            var screen = PointToScreen(e.Location);
            if (Math.Abs(screen.X - _dragStartScreen.X) > ClassicTheme.S(3) ||
                Math.Abs(screen.Y - _dragStartScreen.Y) > ClassicTheme.S(3))
            {
                _dragMode = DragMode.ItemDrag;
                _desktop.BeginItemDrag(this, SelectedIndex, screen);
            }
        }
        else if (_dragMode == DragMode.ItemDrag)
        {
            _desktop.UpdateItemDrag(PointToScreen(e.Location));
        }
        else if (_dragMode == DragMode.None && !IsMaximized)
        {
            int direction = FrameHitTest(e.Location);
            Cursor = direction switch
            {
                1 => Cursors.SizeWE,
                2 => Cursors.SizeWE,
                3 => Cursors.SizeNS,
                4 => Cursors.SizeNS,
                5 => Cursors.SizeNWSE,
                6 => Cursors.SizeNESW,
                7 => Cursors.SizeNESW,
                8 => Cursors.SizeNWSE,
                _ => Cursors.Default
            };
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            bool wasSystem = _systemPressed;
            bool wasMin = _minPressed;
            bool wasMax = _maxPressed;
            _systemPressed = _minPressed = _maxPressed = false;
            Capture = false;

            if (wasSystem && SystemBoxRect.Contains(e.Location))
            {
                ShowSystemMenu();
            }
            else if (wasMin && MinButtonRect.Contains(e.Location))
            {
                _desktop.MinimizeGroup(this);
            }
            else if (wasMax && MaxButtonRect.Contains(e.Location))
            {
                _desktop.ToggleMaximizeGroup(this);
            }
            else if (_dragMode == DragMode.ItemDrag)
            {
                _desktop.CompleteItemDrag(PointToScreen(e.Location));
            }
            else if (_dragMode is DragMode.Move or DragMode.Resize)
            {
                Group.NormalBounds = Bounds;
                _desktop.NotifyGroupLayoutChanged();
            }

            _dragMode = DragMode.None;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (!IsMaximized)
            {
                if (SystemBoxRect.Contains(e.Location))
                {
                    _desktop.MinimizeGroup(this);
                    return;
                }

                var title = new Rectangle(ClassicTheme.Frame, ClassicTheme.Frame,
                    Width - ClassicTheme.Frame * 2, ClassicTheme.TitleBar);
                if (title.Contains(e.Location) &&
                    !SystemBoxRect.Contains(e.Location) &&
                    !MinButtonRect.Contains(e.Location) &&
                    !MaxButtonRect.Contains(e.Location))
                {
                    _desktop.ToggleMaximizeGroup(this);
                    return;
                }
            }

            if (ContentArea.Contains(e.Location))
            {
                var point = new Point(e.Location.X - _itemArea.Left, e.Location.Y - _itemArea.Top);
                int index = GroupLayout.HitTest(_layout, point);
                if (index >= 0)
                {
                    SelectedIndex = index;
                    _desktop.LaunchItem(Group.Items[index]);
                    return;
                }
            }
        }
        base.OnMouseDoubleClick(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (_vScroll.Visible)
        {
            int step = ClassicTheme.CellHeight * 3;
            _vScroll.Value -= e.Delta > 0 ? step : -step;
        }
        base.OnMouseWheel(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && _dragMode == DragMode.ItemDrag)
        {
            _desktop.CancelItemDrag();
            _dragMode = DragMode.None;
            Capture = false;
            Invalidate();
            e.Handled = true;
            return;
        }

        if (_dragMode is DragMode.KeyboardMove or DragMode.KeyboardSize)
        {
            HandleKeyboardGeometry(e);
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
                MoveSelection(-1, 0);
                e.Handled = true;
                break;
            case Keys.Right:
                MoveSelection(1, 0);
                e.Handled = true;
                break;
            case Keys.Up:
                MoveSelection(0, -1);
                e.Handled = true;
                break;
            case Keys.Down:
                MoveSelection(0, 1);
                e.Handled = true;
                break;
            case Keys.Tab:
                MoveSelection(0, 1, cycle: true);
                e.Handled = true;
                break;
            case Keys.Home:
                if (Group.Items.Count > 0)
                    SelectedIndex = 0;
                e.Handled = true;
                break;
            case Keys.End:
                if (Group.Items.Count > 0)
                    SelectedIndex = Group.Items.Count - 1;
                e.Handled = true;
                break;
            case Keys.Enter:
                if (SelectedItem != null)
                {
                    _desktop.LaunchItem(SelectedItem);
                    e.Handled = true;
                }
                break;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    public void ShowSystemMenu(Point? screenLocation = null)
    {
        if (_systemMenu is { Visible: true })
            return;

        var items = new List<MenuEntry>();
        items.Add(MenuEntry.Item("&Restore", () => _desktop.RestoreGroup(this),
            enabled: IsMaximized));
        items.Add(MenuEntry.Item("&Move", BeginKeyboardMove));
        items.Add(MenuEntry.Item("&Size", BeginKeyboardSize));
        items.Add(MenuEntry.Separator());
        items.Add(MenuEntry.Item("Mi&nimize", () => _desktop.MinimizeGroup(this)));
        items.Add(MenuEntry.Item("Ma&ximize", () => _desktop.MaximizeGroup(this),
            enabled: !IsMaximized));
        items.Add(MenuEntry.Separator());
        items.Add(MenuEntry.Item("&Close", () => _desktop.MinimizeGroup(this)));
        items.Add(MenuEntry.Item("&Next", () => _desktop.ActivateNextGroup()));

        var drop = new ClassicMenuDropDown(items);
        _systemMenu = drop;
        drop.FormClosed += (_, _) => _systemMenu = null;
        var location = screenLocation ??
            PointToScreen(new Point(SystemBoxRect.Left, SystemBoxRect.Bottom + ClassicTheme.S(1)));
        drop.ShowAt(this, location);
    }

    public void BeginKeyboardMove()
    {
        if (IsMaximized)
            _desktop.RestoreGroup(this);
        _keyboardStartBounds = Bounds;
        _dragMode = DragMode.KeyboardMove;
        Focus();
    }

    public void BeginKeyboardSize()
    {
        if (IsMaximized)
            _desktop.RestoreGroup(this);
        _keyboardStartBounds = Bounds;
        _dragMode = DragMode.KeyboardSize;
        Focus();
    }

    private void HandleKeyboardGeometry(KeyEventArgs e)
    {
        bool move = _dragMode == DragMode.KeyboardMove;
        if (e.KeyCode == Keys.Escape)
        {
            Bounds = _keyboardStartBounds;
            _dragMode = DragMode.None;
            Group.NormalBounds = Bounds;
            UpdateLayout();
            _desktop.NotifyGroupLayoutChanged();
            e.Handled = true;
            return;
        }
        if (e.KeyCode == Keys.Enter)
        {
            Group.NormalBounds = Bounds;
            _dragMode = DragMode.None;
            _desktop.NotifyGroupLayoutChanged();
            e.Handled = true;
            return;
        }

        int step = e.Shift ? ClassicTheme.S(8) : ClassicTheme.S(1);
        var bounds = Bounds;
        switch (e.KeyCode)
        {
            case Keys.Left:
                if (move) bounds.X -= step;
                else bounds.Width = Math.Max(ClassicTheme.S(160), bounds.Width - step);
                break;
            case Keys.Right:
                if (move) bounds.X += step;
                else bounds.Width += step;
                break;
            case Keys.Up:
                if (move) bounds.Y -= step;
                else bounds.Height = Math.Max(ClassicTheme.S(100), bounds.Height - step);
                break;
            case Keys.Down:
                if (move) bounds.Y += step;
                else bounds.Height += step;
                break;
            default:
                base.OnKeyDown(e);
                return;
        }

        Bounds = ClampBounds(bounds);
        Group.NormalBounds = Bounds;
        UpdateLayout();
        e.Handled = true;
    }

    private void MoveSelection(int dx, int dy, bool cycle = false)
    {
        if (_layout.Count == 0)
            return;

        if (_selectedIndex < 0)
        {
            SelectedIndex = 0;
            return;
        }

        if (cycle)
        {
            SelectedIndex = (_selectedIndex + 1) % _layout.Count;
            return;
        }

        int next = GroupLayout.FindNearest(_layout, _selectedIndex, dx, dy);
        if (next >= 0)
            SelectedIndex = next;
    }

    private Rectangle ClampBounds(Rectangle bounds)
    {
        int minWidth = ClassicTheme.S(160);
        int minHeight = ClassicTheme.S(100);
        bounds.Width = Math.Max(minWidth, bounds.Width);
        bounds.Height = Math.Max(minHeight, bounds.Height);
        bounds.Width = Math.Min(bounds.Width, Math.Max(minWidth, _desktop.ClientSize.Width));
        bounds.Height = Math.Min(bounds.Height, Math.Max(minHeight, _desktop.ClientSize.Height));
        bounds.X = Math.Clamp(bounds.X, 0, Math.Max(0, _desktop.ClientSize.Width - bounds.Width));
        bounds.Y = Math.Clamp(bounds.Y, 0, Math.Max(0, _desktop.ClientSize.Height - bounds.Height));
        return bounds;
    }

    private Rectangle ResizeBounds(Rectangle start, int direction, int dx, int dy)
    {
        int left = start.Left;
        int top = start.Top;
        int right = start.Right;
        int bottom = start.Bottom;
        int minWidth = ClassicTheme.S(160);
        int minHeight = ClassicTheme.S(100);

        if (direction is 1 or 5 or 7) left += dx;
        if (direction is 2 or 6 or 8) right += dx;
        if (direction is 3 or 5 or 6) top += dy;
        if (direction is 4 or 7 or 8) bottom += dy;

        if (right - left < minWidth)
        {
            if (direction is 1 or 5 or 7) left = right - minWidth;
            else right = left + minWidth;
        }
        if (bottom - top < minHeight)
        {
            if (direction is 3 or 5 or 6) top = bottom - minHeight;
            else bottom = top + minHeight;
        }

        var bounds = Rectangle.FromLTRB(left, top, right, bottom);
        return ClampBounds(bounds);
    }

    private int FrameHitTest(Point point)
    {
        int grip = ClassicTheme.S(5);
        bool left = point.X <= grip;
        bool right = point.X >= Width - grip;
        bool top = point.Y <= grip;
        bool bottom = point.Y >= Height - grip;
        if (left && top) return 5;
        if (right && top) return 6;
        if (left && bottom) return 7;
        if (right && bottom) return 8;
        if (left) return 1;
        if (right) return 2;
        if (top) return 3;
        if (bottom) return 4;
        return 0;
    }
}
