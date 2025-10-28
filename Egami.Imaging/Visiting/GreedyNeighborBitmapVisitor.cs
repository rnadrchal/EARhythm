using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class GreedyNeighborBitmapVisitor : BitmapVisitorBase
{
    private readonly bool[,] visited;
    private readonly int width, height, total;
    private int visitedCount;
    private (int X, int Y) current;
    private readonly PriorityQueue<(int X, int Y), double> queue;

    public GreedyNeighborBitmapVisitor(WriteableBitmap bitmap, int startX = 0, int startY = 0) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        current = (startX, startY);
        queue = new PriorityQueue<(int X, int Y), double>();
        visitedCount = 0;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        var (x, y) = current;
        visited[x, y] = true;
        visitedCount++;

        // Nachbarn in die Queue einfügen
        Color currentColor = Bitmap.GetPixel(x, y);
        foreach (var (nx, ny) in GetNeighbors(x, y))
        {
            if (!visited[nx, ny])
            {
                Color neighborColor = Bitmap.GetPixel(nx, ny);
                double dist = ColorDistance(currentColor, neighborColor);
                queue.Enqueue((nx, ny), dist);
            }
        }

        // Nächsten ähnlichsten Nachbarn aus der Queue holen
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (!visited[next.X, next.Y])
            {
                current = next;
                return (x, y);
            }
        }

        // Falls keine Nachbarn mehr vorhanden, beliebigen unbesuchten Pixel suchen
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
            if (!visited[i, j])
            {
                current = (i, j);
                return (x, y);
            }

        return null;
    }

    private IEnumerable<(int X, int Y)> GetNeighbors(int x, int y)
    {
        if (x > 0) yield return (x - 1, y);
        if (x < width - 1) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y < height - 1) yield return (x, y + 1);
    }

    private static double ColorDistance(Color a, Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }
}