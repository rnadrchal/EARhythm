using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class GreedyNeighborBitmapVisitor : BitmapVisitorBase
{
    private readonly int _gridCols;
    private readonly int _gridRows;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private readonly bool[,] visited;
    private readonly int width;
    private readonly int height;
    private readonly int total;

    // Für jede Zelle: Seed und Queue
    private readonly (int X, int Y)[,] seeds;
    private readonly PriorityQueue<(int X, int Y), double>[,] queues;
    private readonly Random rnd = new();

    // Zellen-Iterator
    private int cellStep = 0;
    private readonly List<(int col, int row)> cellOrder;

    private int visitedCount; public GreedyNeighborBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        _gridCols = gridCols;
        _gridRows = gridRows;
        _cellWidth = width / _gridCols;
        _cellHeight = height / _gridRows;

        seeds = new (int X, int Y)[_gridCols, _gridRows];
        queues = new PriorityQueue<(int X, int Y), double>[_gridCols, _gridRows];
        cellOrder = new List<(int col, int row)>();

        // Initialisiere Zellen, Seeds und Queues
        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
        {
            int sx = col * _cellWidth + rnd.Next(_cellWidth);
            int sy = row * _cellHeight + rnd.Next(_cellHeight);
            sx = Math.Min(sx, width - 1);
            sy = Math.Min(sy, height - 1);
            seeds[col, row] = (sx, sy);
            queues[col, row] = new PriorityQueue<(int X, int Y), double>();
            cellOrder.Add((col, row));
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        // Zellen in Z-Order durchlaufen
        var (col, row) = cellOrder[cellStep % cellOrder.Count];
        cellStep++;

        var queue = queues[col, row];

        // Falls Queue leer, Seed einfügen
        if (queue.Count == 0)
        {
            var seed = seeds[col, row];
            if (!visited[seed.X, seed.Y])
            {
                queue.Enqueue(seed, 0);
            }
        }

        // Greedy innerhalb der Zelle
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (!visited[x, y])
            {
                visited[x, y] = true;
                visitedCount++;

                // Nachbarn in der Zelle einfügen
                Color currentColor = Bitmap.GetPixel(x, y);
                foreach (var (nx, ny) in GetCellNeighbors(col, row, x, y))
                {
                    if (!visited[nx, ny])
                    {
                        Color neighborColor = Bitmap.GetPixel(nx, ny);
                        double dist = ColorDistance(currentColor, neighborColor);
                        queue.Enqueue((nx, ny), dist);
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