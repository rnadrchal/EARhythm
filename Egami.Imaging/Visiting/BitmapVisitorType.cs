namespace Egami.Imaging.Visiting;

public enum BitmapVisitorType
{
    RowWise,
    SnakeRow,
    ColumnWise,
    SnakeColumn,
    SpiralInward,
    SpiralOutward,
    GreedyNeighbor,
    Random
}