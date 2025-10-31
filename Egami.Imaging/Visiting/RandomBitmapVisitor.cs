using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RandomBitmapVisitor : BitmapVisitorBase
{
    private readonly int gridCols;
    private readonly int gridRows;
    private readonly int cellWidth;
    private readonly int cellHeight;
    private readonly Random rng;
    private readonly Dictionary<(int gx, int gy), List<(int px, int py)>> cellPixelMap;
    private int currentCellIndex;
    private readonly List<(int gx, int gy)> zOrderCells;

    public RandomBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1, int? seed = null) : base(bitmap)
    {
        this.gridCols = gridCols;
        this.gridRows = gridRows;
        rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        cellWidth = bitmap.PixelWidth / gridCols;
        cellHeight = bitmap.PixelHeight / gridRows;

        // Zellen in Z-Order generieren
        zOrderCells = GetZOrderCells(gridCols, gridRows);
        currentCellIndex = 0;

        // Pixel pro Zelle sammeln
        cellPixelMap = new();
        for (int gx = 0; gx < gridCols; gx++)
        for (int gy = 0; gy < gridRows; gy++)
        {
            var pixels = new List<(int px, int py)>();
            int startX = gx * cellWidth;
            int startY = gy * cellHeight;
            int endX = (gx == gridCols - 1) ? bitmap.PixelWidth : startX + cellWidth;
            int endY = (gy == gridRows - 1) ? bitmap.PixelHeight : startY + cellHeight;
            for (int x = startX; x < endX; x++)
            for (int y = startY; y < endY; y++)
                pixels.Add((x, y));
            cellPixelMap[(gx, gy)] = pixels;
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        // Prüfe, ob noch Pixel übrig sind
        int totalPixelsLeft = 0;
        foreach (var kv in cellPixelMap)
            totalPixelsLeft += kv.Value.Count;
        if (totalPixelsLeft == 0)
            return null;

        // Z-Order Zelle auswählen
        var (gx, gy) = zOrderCells[currentCellIndex];
        var pixels = cellPixelMap[(gx, gy)];

        // Falls Zelle leer, nächste Zelle suchen
        int startCellIndex = currentCellIndex;
        while (pixels.Count == 0)
        {
            currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;
            if (currentCellIndex == startCellIndex)
                return null; // Alle Zellen leer
            (gx, gy) = zOrderCells[currentCellIndex];
            pixels = cellPixelMap[(gx, gy)];
        }

        // Zufälliges Pixel auswählen und entfernen
        int idx = rng.Next(pixels.Count);
        var coord = pixels[idx];
        pixels.RemoveAt(idx);

        // Nächste Zelle (zyklisch)
        currentCellIndex = (currentCellIndex + 1) % zOrderCells.Count;

        return coord;
    }

    // Hilfsmethode für Z-Order Traversierung
    private static List<(int gx, int gy)> GetZOrderCells(int cols, int rows)
    {
        var result = new List<(int gx, int gy)>();
        int maxDim = Math.Max(cols, rows);
        for (int d = 0; d < maxDim * 2; d++)
        {
            for (int gx = 0; gx < cols; gx++)
            for (int gy = 0; gy < rows; gy++)
            {
                if ((gx ^ gy) == d)
                    result.Add((gx, gy));
            }
        }
        // Falls Z-Order nicht alle Zellen abdeckt, ergänzen
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