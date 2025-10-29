using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RotatingInwardBitmapVisitor : BitmapVisitorBase
{
    private readonly int width, height, total;
    private readonly double cx, cy;
    private int visitedCount = 0;
    private double angle = 0;
    private double rx, ry; // Halbachsen der Ellipse
    private const double AngleStep = Math.PI / 180; // 1° Schritte
    private HashSet<(int, int)> visited = new();

    public RotatingInwardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        cx = (width - 1) / 2.0;
        cy = (height - 1) / 2.0;
        rx = cx;
        ry = cy;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        while (visitedCount < total)
        {
            int x = (int)Math.Round(cx + rx * Math.Cos(angle));
            int y = (int)Math.Round(cy + ry * Math.Sin(angle));
            angle += AngleStep;
            if (angle >= 2 * Math.PI)
            {
                angle = 0;
                rx -= 1;
                ry -= 1;
                // Suche den ersten gültigen Pixel auf der neuen Ellipse (meist links oben)
                for (double a = 0; a < 2 * Math.PI; a += AngleStep)
                {
                    int tx = (int)Math.Round(cx + rx * Math.Cos(a));
                    int ty = (int)Math.Round(cy + ry * Math.Sin(a));
                    if (tx >= 0 && tx < width && ty >= 0 && ty < height && visited.Add((tx, ty)))
                    {
                        angle = a;
                        break;
                    }
                }
                if (rx < 0.5 || ry < 0.5) return null;
            }
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                if (visited.Add((x, y)))
                {
                    visitedCount++;
                    return (x, y);
                }
            }
            // Sonst weiter rotieren
        }
        return null;
    }
}