using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RotatingInwardBitmapVisitor : BitmapVisitorBase
{
    private readonly int _gridCols;
    private readonly int _gridRows;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private readonly bool[,] visited;
    private readonly int width;
    private readonly int height;
    private readonly int total;
    private int visitedCount;

    private readonly Queue<(int X, int Y)>[,] queues;
    private readonly (int X, int Y)[,] centers;
    private int cellStep = 0;
    private readonly List<(int col, int row)> cellOrder;

    public RotatingInwardBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        _gridCols = gridCols;
        _gridRows = gridRows;
        _cellWidth = width / _gridCols;
        _cellHeight = height / _gridRows;

        queues = new Queue<(int X, int Y)>[_gridCols, _gridRows];
        centers = new (int X, int Y)[_gridCols, _gridRows];
        cellOrder = new List<(int col, int row)>();

        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
        {
            int centerX = col * _cellWidth + _cellWidth / 2;
            int centerY = row * _cellHeight + _cellHeight / 2;
            centerX = Math.Min(centerX, width - 1);
            centerY = Math.Min(centerY, height - 1);
            centers[col, row] = (centerX, centerY);
            queues[col, row] = new Queue<(int X, int Y)>();
            cellOrder.Add((col, row));
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        var (col, row) = cellOrder[cellStep % cellOrder.Count];
        cellStep++;

        var queue = queues[col, row];

        // Falls Queue leer, Spiral von Zentrum erzeugen
        if (queue.Count == 0)
        {
            var center = centers[col, row];
            foreach (var coord in GetSpiralInwardCoordinates(col, row, center.X, center.Y))
            {
                if (!visited[coord.X, coord.Y])
                    queue.Enqueue(coord);
            }
        }

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (!visited[x, y])
            {
                visited[x, y] = true;
                visitedCount++;
                return (x, y);
            }
        }

        // Falls alle Zellen abgearbeitet, suche beliebigen unbesuchten Pixel
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

    private IEnumerable<(int X, int Y)> GetSpiralInwardCoordinates(int col, int row, int centerX, int centerY)
    {
        int startX = col * _cellWidth;
        int endX = Math.Min(startX + _cellWidth, width);
        int startY = row * _cellHeight;
        int endY = Math.Min(startY + _cellHeight, height);

        int maxRadius = Math.Max(_cellWidth, _cellHeight) / 2 + 1;
        var visitedSpiral = new HashSet<(int, int)>();

        // Von außen (maxRadius) nach innen (0)
        for (int r = maxRadius; r >= 0; r--)
        {
            for (int d = 0; d < 360; d += 8)
            {
                double rad = Math.PI * d / 180.0;
                int x = centerX + (int)Math.Round(r * Math.Cos(rad));
                int y = centerY + (int)Math.Round(r * Math.Sin(rad));
                if (x >= startX && x < endX && y >= startY && y < endY)
                {
                    var coord = (x, y);
                    if (!visitedSpiral.Contains(coord))
                    {
                        visitedSpiral.Add(coord);
                        yield return coord;
                    }
                }
            }
        }
    }
}