using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SpiralInwardBitmapVisitor : BitmapVisitorBase
{
    private int left, right, top, bottom;
    private int x, y;
    private int dir; // 0: rechts, 1: unten, 2: links, 3: oben
    private int visitedCount;
    private readonly int total;

    public SpiralInwardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        left = 0;
        top = 0;
        right = bitmap.PixelWidth - 1;
        bottom = bitmap.PixelHeight - 1;
        x = 0;
        y = 0;
        dir = 0;
        visitedCount = 0;
        total = bitmap.PixelWidth * bitmap.PixelHeight;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        var coord = (x, y);
        visitedCount++;

        // Richtung wechseln, wenn Rand erreicht
        switch (dir)
        {
            case 0: // rechts
                if (x < right) x++;
                else { dir = 1; top++; y++; }
                break;
            case 1: // unten
                if (y < bottom) y++;
                else { dir = 2; right--; x--; }
                break;
            case 2: // links
                if (x > left) x--;
                else { dir = 3; bottom--; y--; }
                break;
            case 3: // oben
                if (y > top) y--;
                else { dir = 0; left++; x++; }
                break;
        }

        return coord;
    }
}
