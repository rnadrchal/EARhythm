using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SpiralOutwardBitmapVisitor : BitmapVisitorBase
{
    private readonly int gridCols;
    private readonly int gridRows;
    private readonly int cellWidth;
    private readonly int cellHeight;
    private readonly List<(int gx, int gy)> zOrderCells;
    private int currentCellIndex;
    private readonly Dictionary<(int gx, int gy), SpiralState> cellStates;

    private class SpiralState
    {
        public int left, right, top, bottom;
        public int x, y, dir, visitedCount, total;
        public int centerX, centerY;
        public int layer;
        public SpiralState(int startX, int startY, int endX, int endY)
        {
            int w = endX - startX;
            int h = endY - startY;
            centerX = w / 2;
            centerY = h / 2;
            x = centerX;
            y = centerY;
            left = centerX;
            right = centerX;
            top = centerY;
            bottom = centerY;
            dir = 0;
            visitedCount = 0;
            total = w * h;
            layer = 1;
        }
    }

    public SpiralOutwardBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
    {
        this.gridCols = gridCols;
        this.gridRows = gridRows;
        cellWidth = bitmap.PixelWidth / gridCols;
        cellHeight = bitmap.PixelHeight / gridRows;
        zOrderCells = GetZOrderCells(gridCols, gridRows);
        currentCellIndex = 0;
        cellStates = new();

        foreach (var cell in zOrderCells)
        {
            int startX = cell.gx * cellWidth;
            int startY = cell.gy * cellHeight;
            int endX = (cell.gx == gridCols - 1) ? bitmap.PixelWidth : startX + cellWidth;
            int endY = (cell.gy == gridRows - 1) ? bitmap.PixelHeight : startY + cellHeight;
            cellStates[cell] = new SpiralState(startX, startY, endX, endY);
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        int checkedCells = 0;
        while (checkedCells < zOrderCells.Count)
        {
            var cell = zOrderCells[currentCellIndex];
            var state = cellStates[cell];

            if (state.visitedCount < state.total)
            {
                int startX = cell.gx * cellWidth;
                int startY = cell.gy * cellHeight;
                var coord = (startX + state.x, startY + state.y);
                state.visitedCount++;

                // Spiral-Outward-Logik: nach jedem Layer die Grenzen erweitern
                switch (state.dir)
                {
                    case 0: // rechts
                        if (state.x < state.right)
                            state.x++;
                        else { state.dir = 1; state.top--; state.y--; }
                        break;
                    case 1: // oben
                        if (state.y > state.top)
                            state.y--;
                        else { state.dir = 2; state.left--; state.x--; }
                        break;
                    case 2: // links
                        if (state.x > state.left)
                            state.x--;
                        else { state.dir = 3; state.bottom++; state.y++; }
                        break;
                    case 3: // unten
                        if (state.y < state.bottom)
                            state.y++;
                        else
                        {
                            // Nächster Layer
                            state.dir = 0;
                            state.right++;
                            state.top--;
                            state.left--;
                            state.bottom++;
                            state.x = state.centerX + state.layer;
                            state.y = state.centerY;
                            state.layer++;
                        }
                        break;
                }

                currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;
                return coord;
            }
            currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;
            checkedCells++;
        }
        return null;
    }

    // Hilfsmethode für Z-Order Traversierung
    private static List<(int gx, int gy)> GetZOrderCells(int cols, int rows)
    {
        var result = new List<(int gx, int gy)>();
        int maxDim = System.Math.Max(cols, rows);
        for (int d = 0; d < maxDim * 2; d++)
        {
            for (int gx = 0; gx < cols; gx++)
                for (int gy = 0; gy < rows; gy++)
                {
                    if ((gx ^ gy) == d)
                        result.Add((gx, gy));
                }
        }
        // Ergänze fehlende Zellen
        if (result.Count < cols * rows)
        {
            for (int gx = 0; gx < cols; gx++)
                for (int gy = 0; gy < rows; gy++)
                    if (!result.Contains((gx, gy)))
                        result.Add((gx, gy));
        }
        return result;
    }
}