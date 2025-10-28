using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class ColumnWiseBitmapVisitor : BitmapVisitorBase
{
    private int x = 0, y = 0;

    public ColumnWiseBitmapVisitor(WriteableBitmap bitmap) : base(bitmap)
    {
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        var width = Bitmap.PixelWidth;
        var height = Bitmap.PixelHeight;

        var coord = (x, y);
        y++;
        if (y >= height)
        {
            y = 0;
            x++;
        }
        if (x >= width) return null;
        return coord;
    }
}