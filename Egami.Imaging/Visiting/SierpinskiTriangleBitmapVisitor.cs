using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class SierpinskiTriangleBitmapVisitor : BitmapVisitorBase
{
    private readonly int _gridCols;
    private readonly int _gridRows;
    private readonly int _cellWidth;
    private readonly int _cellHeight;
    private readonly int width;
    private readonly int height;
    private readonly bool[,] visited;
    private readonly int total;
    private int visitedCount;

    private readonly List<(int x, int y)>[,] sierpinskiCurves;
    private readonly int[,] sierpinskiIndex;
    private readonly List<(int col, int row)> cellOrder;
    private int cellStep = 0;

    public SierpinskiTriangleBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        total = width * height;
        visited = new bool[width, height];
        _gridCols = gridCols;
        _gridRows = gridRows;
        _cellWidth = width / _gridCols;
        _cellHeight = height / _gridRows;

        cellOrder = new List<(int col, int row)>();
        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
            cellOrder.Add((col, row));

        sierpinskiCurves = new List<(int x, int y)>[_gridCols, _gridRows];
        sierpinskiIndex = new int[_gridCols, _gridRows];

        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
        {
            sierpinskiCurves[col, row] = GenerateSierpinskiCurve(_cellWidth, _cellHeight);
            sierpinskiIndex[col, row] = 0;
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (visitedCount >= total)
            return null;

        int checkedCells = 0;
        while (checkedCells < cellOrder.Count)
        {
            var (col, row) = cellOrder[cellStep % cellOrder.Count];
            cellStep++;

            var curve = sierpinskiCurves[col, row];
            int idx = sierpinskiIndex[col, row];

            while (idx < curve.Count)
            {
                int x = col * _cellWidth + curve[idx].x;
                int y = row * _cellHeight + curve[idx].y;
                sierpinskiIndex[col, row]++;
                if (x < width && y < height && !visited[x, y])
                {
                    visited[x, y] = true;
                    visitedCount++;
                    return (x, y);
                }
                idx = sierpinskiIndex[col, row];
            }
            checkedCells++;
        }

        // Fallback
        //for (int i = 0; i < width; i++)
        //for (int j = 0; j < height; j++)
        //    if (!visited[i, j])
        //    {
        //        visited[i, j] = true;
        //        visitedCount++;
        //        return (i, j);
        //    }
        return null;
    }

    // Sierpinski-Dreieck-Generator
    private List<(int x, int y)> GenerateSierpinskiCurve(int w, int h)
    {
        var result = new List<(int x, int y)>();
        int order = (int)Math.Log(Math.Min(w, h), 2);
        int size = 1 << order;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if ((x & y) == 0) // Sierpinski-Muster
                result.Add((x, y));
        }
        return result;
    }
}