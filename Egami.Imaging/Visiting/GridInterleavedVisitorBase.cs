using System.Drawing;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace Egami.Imaging.Visiting;

public abstract class GridInterleavedVisitorBase : BitmapVisitorBase
{
    protected int gridCols;
    protected int gridRows;
    protected int currentIndex;

    public GridInterleavedVisitorBase(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1)
    : base(bitmap)
    {
        this.gridCols = gridCols;
        this.gridRows = gridRows;
        this.currentIndex = 0;
    }

    public override Color? Next()
    {
        // Interleaved-Logik: z.B. Spaltenweise, dann Zeile, oder nach Muster
        int x = currentIndex % gridCols;
        int y = currentIndex / gridCols;
        Visit(x, y);
        currentIndex++;
        return base.Next();
    }

    protected abstract void Visit(int x, int y);
}