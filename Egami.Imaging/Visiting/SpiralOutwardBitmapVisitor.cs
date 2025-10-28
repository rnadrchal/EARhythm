using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SpiralOutwardBitmapVisitor : BitmapVisitorBase
{
    private int step = 1, dir = 0, x, y, count = 0, leg = 0;
    private readonly int width, height, total;
    private readonly int cx, cy;

    public SpiralOutwardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        cx = width / 2;
        cy = height / 2;
        x = cx;
        y = cy;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (count == 0)
        {
            count++;
            return (x, y);
        }

        for (; count < total;)
        {
            for (int i = 0; i < step; i++)
            {
                switch (dir)
                {
                    case 0: x++; break; // rechts
                    case 1: y++; break; // unten
                    case 2: x--; break; // links
                    case 3: y--; break; // oben
                }
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    count++;
                    return (x, y);
                }
            }
            dir = (dir + 1) % 4;
            leg++;
            if (leg % 2 == 0) step++;
        }
        return null;
    }
}