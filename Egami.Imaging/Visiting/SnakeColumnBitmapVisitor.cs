using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class SnakeColumnBitmapVisitor : BitmapVisitorBase
{
    private readonly int _gridCols;
    private readonly int _gridRows;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private readonly int width;
    private readonly int height;
    private readonly bool[,] visited;
    private readonly int total;
    private int visitedCount;

    // Für jede Zelle: aktueller Spalten- und Zeilenindex
    private readonly int[,] localCol;
    private readonly int[,] localRow;
    private readonly List<(int col, int row)> cellOrder;
    private int cellStep = 0;

    public SnakeColumnBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        _gridCols = gridCols;
        _gridRows = gridRows;
        _cellWidth = width / _gridCols;
        _cellHeight = height / _gridRows;

        cellOrder = new List<(int col, int row)>();
        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
            cellOrder.Add((col, row));

        localCol = new int[_gridCols, _gridRows];
        localRow = new int[_gridCols, _gridRows];
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        int checkedCells = 0;
        while (checkedCells < cellOrder.Count)
        {
            var (col, row) = cellOrder[cellStep % cellOrder.Count];
            cellStep++;

            int startX = col * _cellWidth;
            int endX = Math.Min(startX + _cellWidth, width);
            int startY = row * _cellHeight;
            int endY = Math.Min(startY + _cellHeight, height);

            int c = localCol[col, row];
            int r = localRow[col, row];

            int x = startX + c;
            int y;

            // Snake-Muster: gerade Spalte von oben nach unten, ungerade von unten nach oben
            if (c % 2 == 0)
                y = startY + r;
            else
                y = endY - 1 - r;

            // Prüfe, ob Koordinaten gültig sind
            if (x < endX && y >= startY && y < endY)
            {
                if (!visited[x, y])
                {
                    visited[x, y] = true;
                    visitedCount++;
                    localRow[col, row]++;
                    // Wenn die aktuelle Spalte in der Zelle fertig ist, zur nächsten Spalte
                    if (localRow[col, row] >= _cellHeight)
                    {
                        localRow[col, row] = 0;
                        localCol[col, row]++;
                    }
                    return (x, y);
                }
                else
                {
                    // Pixel schon besucht, zur nächsten Zeile/Spalte
                    localRow[col, row]++;
                    if (localRow[col, row] >= _cellHeight)
                    {
                        localRow[col, row] = 0;
                        localCol[col, row]++;
                    }
                }
            }
            else
            {
                // Spalte in der Zelle fertig, zur nächsten Spalte
                localRow[col, row] = 0;
                localCol[col, row]++;
            }

            checkedCells++;
        }

        // Fallback: falls noch Pixel übrig sind
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                if (!visited[i, j])
                {
                    visited[i, j] = true;
                    visitedCount++;
                    return (i, j);
                }

        return null;
    }
}