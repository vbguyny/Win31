namespace ProgramManager31.Models;

internal sealed record ItemLayout(ProgramItem Item, Rectangle Cell, Rectangle Icon, Rectangle Label);

internal static class GroupLayout
{
    private const int PaddingBase = 4;

    public static (List<ItemLayout> Items, Size ContentSize) Compute(ProgramGroup group, Size clientSize,
        bool autoArrange, Point scroll)
    {
        var layouts = new List<ItemLayout>(group.Items.Count);
        int pad = ClassicTheme.S(PaddingBase);
        int cellWidth = ClassicTheme.CellWidth;
        int cellHeight = ClassicTheme.CellHeight;
        int iconSize = ClassicTheme.IconSize;
        int labelHeight = ClassicTheme.LabelHeight;

        if (autoArrange)
        {
            int usableHeight = Math.Max(cellHeight, clientSize.Height - pad * 2);
            int rows = Math.Max(1, usableHeight / cellHeight);
            int columns = group.Items.Count == 0 ? 1 : (group.Items.Count + rows - 1) / rows;

            for (int i = 0; i < group.Items.Count; i++)
            {
                int column = i / rows;
                int row = i % rows;
                int cellX = pad + column * cellWidth - scroll.X;
                int cellY = pad + row * cellHeight - scroll.Y;
                layouts.Add(CreateLayout(group.Items[i], cellX, cellY, cellWidth, cellHeight, iconSize, labelHeight));
            }

            int visibleRows = Math.Min(rows, Math.Max(1, group.Items.Count));
            int height = group.Items.Count == 0
                ? pad * 2
                : pad * 2 + visibleRows * cellHeight;
            int width = pad * 2 + columns * cellWidth;
            return (layouts, new Size(width, height));
        }
        else
        {
            int maxX = pad * 2;
            int maxY = pad * 2;
            foreach (var item in group.Items)
            {
                int cellX = item.X - scroll.X;
                int cellY = item.Y - scroll.Y;
                layouts.Add(CreateLayout(item, cellX, cellY, cellWidth, cellHeight, iconSize, labelHeight));
                maxX = Math.Max(maxX, item.X + cellWidth + pad);
                maxY = Math.Max(maxY, item.Y + cellHeight + pad);
            }
            return (layouts, new Size(maxX, maxY));
        }
    }

    public static void ArrangeItems(ProgramGroup group, int clientHeight)
    {
        int pad = ClassicTheme.S(PaddingBase);
        int cellHeight = ClassicTheme.CellHeight;
        int rows = Math.Max(1, (Math.Max(clientHeight, cellHeight + pad * 2) - pad * 2) / cellHeight);
        for (int i = 0; i < group.Items.Count; i++)
        {
            int column = i / rows;
            int row = i % rows;
            group.Items[i].X = pad + column * ClassicTheme.CellWidth;
            group.Items[i].Y = pad + row * cellHeight;
        }
    }

    public static int HitTest(IReadOnlyList<ItemLayout> layouts, Point point)
    {
        for (int i = layouts.Count - 1; i >= 0; i--)
            if (layouts[i].Cell.Contains(point))
                return i;
        return -1;
    }

    public static int FindNearest(IReadOnlyList<ItemLayout> layouts, int currentIndex, int dx, int dy)
    {
        if (currentIndex < 0 || currentIndex >= layouts.Count)
            return layouts.Count > 0 ? 0 : -1;

        var current = layouts[currentIndex].Cell;
        int cx = current.Left + current.Width / 2;
        int cy = current.Top + current.Height / 2;
        int best = -1;
        int bestScore = int.MaxValue;

        for (int i = 0; i < layouts.Count; i++)
        {
            if (i == currentIndex)
                continue;
            var cell = layouts[i].Cell;
            int ox = cell.Left + cell.Width / 2 - cx;
            int oy = cell.Top + cell.Height / 2 - cy;

            if (dx > 0 && ox <= 0) continue;
            if (dx < 0 && ox >= 0) continue;
            if (dy > 0 && oy <= 0) continue;
            if (dy < 0 && oy >= 0) continue;

            int score;
            if (dx != 0)
                score = Math.Abs(ox) + Math.Abs(oy) * 3;
            else
                score = Math.Abs(oy) + Math.Abs(ox) * 3;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    private static ItemLayout CreateLayout(ProgramItem item, int cellX, int cellY, int cellWidth,
        int cellHeight, int iconSize, int labelHeight)
    {
        var cell = new Rectangle(cellX, cellY, cellWidth, cellHeight);
        var icon = new Rectangle(cellX + (cellWidth - iconSize) / 2, cellY + ClassicTheme.S(2), iconSize, iconSize);
        var label = new Rectangle(cellX + ClassicTheme.S(2), icon.Bottom + ClassicTheme.S(2),
            cellWidth - ClassicTheme.S(4), labelHeight);
        return new ItemLayout(item, cell, icon, label);
    }
}
