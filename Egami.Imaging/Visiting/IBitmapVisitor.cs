using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class VisitorEventArgs : EventArgs
{
    public VisitorEventArgs(Color color, int x, int y)
    {
        Color = color;
        X = x;
        Y = y;
    }

    public Color Color { get; }
    public int X { get; }
    public int Y { get; }
    
}

public interface IBitmapVisitor
{
    Color? Next();
    EventHandler<VisitorEventArgs> Visited { get; set; }
}