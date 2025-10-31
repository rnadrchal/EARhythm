using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class RowWiseBitmapVisitor : BitmapVisitorBase
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

    // Für jede Zelle: aktueller Zeilen- und Spaltenindex
    private readonly int[,] localX;
    private readonly int[,] localY;
    private readonly List<(int col, int row)> cellOrder;
    private int cellStep = 0;

    public RowWiseBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
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

        localX = new int[_gridCols, _gridRows];
        localY = new int[_gridCols, _gridRows];
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

            int x = startX + localX[col, row];
            int y = startY + localY[col, row];

            // Suche nächsten gültigen Pixel in dieser Zelle
            while (y < endY)
            {
                while (x < endX)
                {
                    if (!visited[x, y])
                    {
                        visited[x, y] = true;
                        visitedCount++;
                        localX[col, row]++;
                        return (x, y);
                    }
                    x++;
                    localX[col, row]++;
                }
                localX[col, row] = 0;
                y++;
                localY[col, row]++;
                x = startX;
            }
            // Zelle ist fertig, nichts mehr zu tun
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