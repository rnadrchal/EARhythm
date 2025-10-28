using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class SnakeRowBitmapVisitor : BitmapVisitorBase
{
    private int x = 0, y = 0;
    private bool _ltr;

    public SnakeRowBitmapVisitor(WriteableBitmap bitmap, bool ltr = true) : base(bitmap)
    {
        _ltr = ltr;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        var width = Bitmap.PixelWidth;
        var height = Bitmap.PixelHeight;

        var coord = (_ltr ? x : width - 1 - x, y);
        x++;
        if (x >= width)
        {
            x = 0;
            y++;
            _ltr = !_ltr;
        }
        if (y >= height) return null;
        return coord;
    }
}