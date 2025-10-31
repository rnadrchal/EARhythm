using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class HilbertCurveBitmapVisitor : BitmapVisitorBase
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

    private readonly List<(int x, int y)>[,] hilbertCurves;
    private readonly int[,] hilbertIndex;
    private readonly List<(int col, int row)> cellOrder;
    private int cellStep = 0;

    public HilbertCurveBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
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

        hilbertCurves = new List<(int x, int y)>[_gridCols, _gridRows];
        hilbertIndex = new int[_gridCols, _gridRows];

        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
        {
            hilbertCurves[col, row] = GenerateHilbertCurve(_cellWidth, _cellHeight);
            hilbertIndex[col, row] = 0;
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

            var curve = hilbertCurves[col, row];
            int idx = hilbertIndex[col, row];

            int step = 2; // z.B. nur jeden zweiten Punkt
            while (idx < curve.Count)
            {
                int x = col * _cellWidth + curve[idx].x;
                int y = row * _cellHeight + curve[idx].y;
                hilbertIndex[col, row] += step;
                if (x < width && y < height && !visited[x, y])
                {
                    visited[x, y] = true;
                    visitedCount++;
                    return (x, y);
                }
                idx = hilbertIndex[col, row];
            }
            checkedCells++;
        }

        // Fallback
        //for (int i = 0; i < width; i++)
        //    for (int j = 0; j < height; j++)
        //        if (!visited[i, j])
        //        {
        //            visited[i, j] = true;
        //            visitedCount++;
        //            return (i, j);
        //        }
        return null;
    }

    // Hilbert-Kurven-Generator für quadratische Zellen (Potenz von 2)
    private List<(int x, int y)> GenerateHilbertCurve(int w, int h)
    {
        int n = Math.Min(w, h);
        int order = (int)Math.Log(n, 2);
        var result = new List<(int x, int y)>();
        int size = 1 << order;
        for (int i = 0; i < size * size; i++)
        {
            var (x, y) = HilbertIndexToXY(i, order);
            if (x < w && y < h)
                result.Add((x, y));
        }
        return result;
    }

    // Hilbert-Index zu XY (rekursiv)
    private (int x, int y) HilbertIndexToXY(int index, int order)
    {
        int x = 0, y = 0;
        for (int s = 1, t = index; s < (1 << order); s <<= 1)
        {
            int rx = 1 & (t / 2);
            int ry = 1 & (t ^ rx);
            Rotate(s, ref x, ref y, rx, ry);
            x += s * rx;
            y += s * ry;
            t /= 4;
        }
        return (x, y);
    }

    private void Rotate(int n, ref int x, ref int y, int rx, int ry)
    {
        if (ry == 0)
        {
            if (rx == 1)
            {
                x = n - 1 - x;
                y = n - 1 - y;
            }
            // Swap x and y
            (x, y) = (y, x);
        }
    }
}