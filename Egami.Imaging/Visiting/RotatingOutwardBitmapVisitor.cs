using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RotatingOutwardBitmapVisitor : BitmapVisitorBase
{
    private readonly int width, height, total;
    private readonly double cx, cy;
    private int visitedCount = 0;
    private double radius = 0;
    private const double AngleStep = Math.PI / 180; // 1° Schritte
    private double angle = 0;
    private HashSet<(int, int)> visited = new();

    public RotatingOutwardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        cx = (width - 1) / 2.0;
        cy = (height - 1) / 2.0;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        int maxRadius = (int)Math.Ceiling(Math.Sqrt(cx * cx + cy * cy));
        while (radius <= maxRadius)
        {
            int x = (int)Math.Round(cx + radius * Math.Cos(angle));
            int y = (int)Math.Round(cy + radius * Math.Sin(angle));
            angle += AngleStep;
            if (angle >= 2 * Math.PI)
            {
                angle = 0;
                radius += 1;
            }
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                if (visited.Add((x, y)))
                {
                    visitedCount++;
                    return (x, y);
                }
            }
        }
        return null;
    }
}