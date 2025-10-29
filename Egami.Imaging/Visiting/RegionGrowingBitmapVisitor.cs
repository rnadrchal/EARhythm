using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RegionGrowingBitmapVisitor : BitmapVisitorBase
{
    private readonly int _gridCols;
    private readonly int _gridRows;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private readonly int _tolerance;
    private readonly bool[,] visited;
    private readonly int width;
    private readonly int height;
    private readonly int total;
    private int visitedCount;

    private readonly Queue<(int X, int Y)>[,] queues;
    private readonly (int X, int Y)[,] centers;
    private int cellStep = 0;
    private readonly List<(int col, int row)> cellOrder;

    public RegionGrowingBitmapVisitor(WriteableBitmap bitmap, int gridCols = 4, int gridRows = 4, int tolerance = 32) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        _gridCols = gridCols;
        _gridRows = gridRows;
        _cellWidth = width / _gridCols;
        _cellHeight = height / _gridRows;
        _tolerance = tolerance;

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

        // Falls Queue leer, Zentrum einfügen
        if (queue.Count == 0)
        {
            var center = centers[col, row];
            if (!visited[center.X, center.Y])
            {
                queue.Enqueue(center);
            }
        }

        // Region Growing innerhalb der Zelle
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (!visited[x, y])
            {
                visited[x, y] = true;
                visitedCount++;

                Color seedColor = Bitmap.GetPixel(centers[col, row].X, centers[col, row].Y);
                foreach (var (nx, ny) in GetCellNeighbors(col, row, x, y))
                {
                    if (!visited[nx, ny])
                    {
                        Color neighborColor = Bitmap.GetPixel(nx, ny);
                        if (ColorDistance(seedColor, neighborColor) <= _tolerance * _tolerance)
                        {
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
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

    private IEnumerable<(int X, int Y)> GetCellNeighbors(int col, int row, int x, int y)
    {
        int startX = col * _cellWidth;
        int endX = Math.Min(startX + _cellWidth, width);
        int startY = row * _cellHeight;
        int endY = Math.Min(startY + _cellHeight, height);

        if (x > startX) yield return (x - 1, y);
        if (x < endX - 1) yield return (x + 1, y);
        if (y > startY) yield return (x, y - 1);
        if (y < endY - 1) yield return (x, y + 1);
    }

    private static double ColorDistance(Color a, Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }
}