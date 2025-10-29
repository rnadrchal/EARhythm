using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public static class BitmapVisitorFactory
{
    public static IBitmapVisitor Create(BitmapVisitorType type, WriteableBitmap bitmap,
        int startX = 0, int startY = 0, bool topDown = true, int tolerance = 32,
        int gridRows = 4, int gridCols = 4)
    {
        return type switch
        {
            BitmapVisitorType.RowWise => new RowWiseBitmapVisitor(bitmap),
            BitmapVisitorType.SnakeRow => new SnakeRowBitmapVisitor(bitmap),
            BitmapVisitorType.ColumnWise => new ColumnWiseBitmapVisitor(bitmap),
            BitmapVisitorType.SnakeColumn => new SnakeColumnBitmapVisitor(bitmap, topDown),
            BitmapVisitorType.SpiralInward => new SpiralInwardBitmapVisitor(bitmap),
            BitmapVisitorType.SpiralOutward => new SpiralOutwardBitmapVisitor(bitmap),
            BitmapVisitorType.GreedyNeighbor => new GreedyNeighborBitmapVisitor(bitmap, startX, startY),
            BitmapVisitorType.Random => new RandomBitmapVisitor(bitmap),
            BitmapVisitorType.Wavefront => new WavefrontBitmapVisitor(bitmap),
            BitmapVisitorType.Checkerboard => new CheckerboardBitmapVisitor(bitmap),
            BitmapVisitorType.RegionGrowing => new RegionGrowingBitmapVisitor(bitmap, startX, startY, tolerance),
            BitmapVisitorType.GridInterleaved => new GridInterleavedBitmapVisitor(bitmap, gridRows, gridCols),
            BitmapVisitorType.RotatingInward => new RotatingInwardBitmapVisitor(bitmap),
            BitmapVisitorType.RotatingOutward => new RotatingOutwardBitmapVisitor(bitmap),
            _ => throw new ArgumentException("Unbekannter Visitor-Typ", nameof(type))
        };
    }
}