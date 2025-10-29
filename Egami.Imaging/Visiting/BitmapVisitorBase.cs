using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public abstract class BitmapVisitorBase : IBitmapVisitor
{
    public WriteableBitmap Bitmap { get; }

    protected BitmapVisitorBase(WriteableBitmap bitmap)
    {
        Bitmap = bitmap;
    }

    public virtual Color? Next()
    {
        var coord = GetNextCoordinates();
        if (coord == null)
        {
            return null;
        }
        var pixel = Bitmap.GetPixel(coord.Value.X, coord.Value.Y);
        if (Visited != null)
        {
            Visited(this, new VisitorEventArgs(pixel, coord.Value.X, coord.Value.Y));
        }

        return pixel;
    }

    protected abstract (int X, int Y)? GetNextCoordinates();

    public EventHandler<VisitorEventArgs> Visited { get; set; }
}