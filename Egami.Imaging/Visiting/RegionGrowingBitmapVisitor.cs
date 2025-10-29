using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RegionGrowingBitmapVisitor : BitmapVisitorBase
{
    private readonly bool[,] visited;
    private readonly Queue<(int X, int Y)> queue = new();
    private readonly int width, height;
    private readonly Color seedColor;
    private readonly int tolerance;

    public RegionGrowingBitmapVisitor(WriteableBitmap bitmap, int startX = 0, int startY = 0, int tolerance = 32) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        visited = new bool[width, height];
        seedColor = bitmap.GetPixel(startX, startY);
        this.tolerance = tolerance;
        queue.Enqueue((startX, startY));
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (visited[x, y]) continue;
            visited[x, y] = true;

            foreach (var (nx, ny) in GetNeighbors(x, y))
            {
                if (!visited[nx, ny] && IsSimilar(Bitmap.GetPixel(nx, ny), seedColor))
                    queue.Enqueue((nx, ny));
            }
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

    private bool IsSimilar(Color a, Color b)
    {
        int dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return dr * dr + dg * dg + db * db <= tolerance * tolerance;
    }
}