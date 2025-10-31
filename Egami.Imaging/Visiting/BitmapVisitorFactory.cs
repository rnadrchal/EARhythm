using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public static class BitmapVisitorFactory
{
    public static IBitmapVisitor Create(BitmapVisitorType type, WriteableBitmap bitmap,
        int tolerance = 32, int gridRows = 4, int gridCols = 4)
    {
        return type switch
        {
            BitmapVisitorType.RowWise => new RowWiseBitmapVisitor(bitmap, gridRows, gridCols),
            BitmapVisitorType.SnakeRow => new SnakeRowBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.ColumnWise => new ColumnWiseBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.SnakeColumn => new SnakeColumnBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.SpiralInward => new SpiralInwardBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.SpiralOutward => new SpiralOutwardBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.GreedyNeighbor => new GreedyNeighborBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.Random => new RandomBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.Wavefront => new WavefrontBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.Checkerboard => new CheckerboardBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.RegionGrowing => new RegionGrowingBitmapVisitor(bitmap, gridCols, gridRows, tolerance),
            BitmapVisitorType.GridInterleaved => new GridInterleavedBitmapVisitor(bitmap, gridRows, gridCols),
            BitmapVisitorType.RotatingInward => new RotatingInwardBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.RotatingOutward => new RotatingOutwardBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.HilbertCurve => new HilbertCurveBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.SierpinskiTriangle => new SierpinskiTriangleBitmapVisitor(bitmap, gridCols, gridRows), 
            BitmapVisitorType.PeanoCurve => new PeanoCurveBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.FractalMandelbrot => new FractalSetBitmapVisitor(bitmap, gridCols, gridRows),
            BitmapVisitorType.FractalJulia => new FractalSetBitmapVisitor(bitmap, gridCols, gridRows, FractalSetType.Julia),
            _ => throw new ArgumentException("Unbekannter Visitor-Typ", nameof(type))
        };
    }
}