using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class SnakeColumnBitmapVisitor : BitmapVisitorBase
{
    private int x = 0, y = 0;
    private bool _topDown;

    public SnakeColumnBitmapVisitor(WriteableBitmap bitmap, bool topDown = true) : base(bitmap)
    {
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        var width = Bitmap.PixelWidth;
        var height = Bitmap.PixelHeight;

        var coord = (x, _topDown ? y : height - 1 - y);
        y++;
        if (y >= height)
        {
            y = 0;
            x++;
            _topDown = !_topDown;
        }
        if (x >= width) throw new InvalidOperationException("Ende des Bitmaps erreicht.");
        return coord;
    }
}