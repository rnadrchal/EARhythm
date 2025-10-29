namespace Egami.Imaging.Visiting;

public enum BitmapVisitorType
{
    RowWise,
    ColumnWise,
    SnakeRow,
    SnakeColumn,
    Wavefront,
    SpiralInward,
    SpiralOutward,
    RotatingInward,
    RotatingOutward,
    Checkerboard,
    GridInterleaved,
    GreedyNeighbor,
    RegionGrowing,
    Random
}