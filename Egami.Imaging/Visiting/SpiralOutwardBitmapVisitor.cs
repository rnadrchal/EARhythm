using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SpiralOutwardBitmapVisitor : BitmapVisitorBase
{
    private readonly int width, height, total;
    private readonly int cx, cy;
    private int x, y, count;
    private int dir = 0; // 0: rechts, 1: unten, 2: links, 3: oben
    private int stepsInCurrentLeg = 0;
    private int legLength = 1;
    private int legsDone = 0;

    public SpiralOutwardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        cx = width / 2;
        cy = height / 2;
        x = cx;
        y = cy;
        count = 0;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (count == 0)
        {
            count++;
            return (x, y);
        }

        while (count < total)
        {
            // Richtung: 0=rechts, 1=unten, 2=links, 3=oben
            switch (dir)
            {
                case 0: x++; break;
                case 1: y++; break;
                case 2: x--; break;
                case 3: y--; break;
            }
            stepsInCurrentLeg++;

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                count++;
                if (stepsInCurrentLeg == legLength)
                {
                    dir = (dir + 1) % 4;
                    stepsInCurrentLeg = 0;
                    legsDone++;
                    if (legsDone % 2 == 0)
                        legLength++;
                }
                return (x, y);
            }
            else
            {
                // Auch wenn der Pixel außerhalb ist, müssen wir die Spiral-Logik weiterführen
                if (stepsInCurrentLeg == legLength)
                {
                    dir = (dir + 1) % 4;
                    stepsInCurrentLeg = 0;
                    legsDone++;
                    if (legsDone % 2 == 0)
                        legLength++;
                }
            }
        }
        return null;
    }
}