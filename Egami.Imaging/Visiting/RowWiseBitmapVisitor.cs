using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class RowWiseBitmapVisitor : BitmapVisitorBase
{
    public RowWiseBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
    }

    private int x = 0, y = 0;

    protected override (int X, int Y)? GetNextCoordinates()
    {
        var width = Bitmap.PixelWidth;
        var height = Bitmap.PixelHeight;

        var coord = (x, y);
        x++;
        if (x >= width)
        {
            x = 0;
            y++;
        }
        if (y >= height) return null;
        return coord;
    }
}