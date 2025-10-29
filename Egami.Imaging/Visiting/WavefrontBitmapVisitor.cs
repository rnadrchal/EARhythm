using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class WavefrontBitmapVisitor : BitmapVisitorBase
{
    private int x = 0, y = 0;
    private readonly int width, height;

    public WavefrontBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (y >= height) return null;
        var result = (x, y);
        x++;
        if (x >= width)
        {
            x = 0;
            y++;
        }
        return result;
    }
}