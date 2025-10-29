using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class CheckerboardBitmapVisitor : BitmapVisitorBase
{
    private readonly List<(int X, int Y)> coords = new();
    private int index = 0;

    public CheckerboardBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        int width = bitmap.PixelWidth, height = bitmap.PixelHeight;
        int layers = Math.Min(width, height) / 2;
        for (int l = 0; l <= layers; l++)
        {
            // Obere und untere Kante
            for (int x = l; x < width - l; x++)
            {
                coords.Add((x, l));
                if (height - l - 1 != l)
                    coords.Add((x, height - l - 1));
            }
            // Linke und rechte Kante
            for (int y = l + 1; y < height - l - 1; y++)
            {
                coords.Add((l, y));
                if (width - l - 1 != l)
                    coords.Add((width - l - 1, y));
            }
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (index >= coords.Count) return null;
        return coords[index++];
    }
}