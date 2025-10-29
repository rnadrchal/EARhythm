using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SnakeRowBitmapVisitor : BitmapVisitorBase
{
    private readonly int gridCols;
    private readonly int gridRows;
    private readonly int cellWidth;
    private readonly int cellHeight;
    private readonly List<(int gx, int gy)> zOrderCells;
    private int currentCellIndex;
    private readonly Dictionary<(int gx, int gy), (int x, int y, bool ltr)> cellStates;

    public SnakeRowBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1, int? seed = null) : base(bitmap)
    {
        this.gridCols = gridCols;
        this.gridRows = gridRows;
        cellWidth = bitmap.PixelWidth / gridCols;
        cellHeight = bitmap.PixelHeight / gridRows;
        zOrderCells = GetZOrderCells(gridCols, gridRows);
        currentCellIndex = 0;
        cellStates = new();

        // Initialisiere jede Zelle mit Startkoordinaten und Richtung
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        foreach (var cell in zOrderCells)
            cellStates[cell] = (0, 0, rng.NextDouble() > 0.5);
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        // Suche nach einer Zelle mit noch verfügbaren Pixeln
        int checkedCells = 0;
        while (checkedCells < zOrderCells.Count)
        {
            var cell = zOrderCells[currentCellIndex];
            var (cx, cy, ltr) = cellStates[cell];

            int startX = cell.gx * cellWidth;
            int startY = cell.gy * cellHeight;
            int endX = (cell.gx == gridCols - 1) ? Bitmap.PixelWidth : startX + cellWidth;
            int endY = (cell.gy == gridRows - 1) ? Bitmap.PixelHeight : startY + cellHeight;

            if (cy < endY - startY)
            {
                int x = ltr ? cx : (endX - startX - 1 - cx);
                int y = cy;
                var coord = (startX + x, startY + y);

                // Nächster Schritt in der Zelle
                cx++;
                if (cx >= endX - startX)
                {
                    cx = 0;
                    cy++;
                    ltr = !ltr;
                }
                cellStates[cell] = (cx, cy, ltr);

                // Zirkuliere zur nächsten Zelle für den nächsten Aufruf
                currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;
                return coord;
            }
            // Zelle ist fertig, nächste Zelle prüfen
            currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;
            checkedCells++;
        }
        // Alle Zellen sind fertig
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