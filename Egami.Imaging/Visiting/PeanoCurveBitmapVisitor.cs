using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public sealed class PeanoCurveBitmapVisitor : BitmapVisitorBase
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

    private readonly List<(int x, int y)>[,] peanoCurves;
    private readonly int[,] peanoIndex;
    private readonly List<(int col, int row)> cellOrder;
    private int cellStep = 0;

    public PeanoCurveBitmapVisitor(WriteableBitmap bitmap, int gridCols = 1, int gridRows = 1) : base(bitmap)
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

        peanoCurves = new List<(int x, int y)>[_gridCols, _gridRows];
        peanoIndex = new int[_gridCols, _gridRows];

        for (int col = 0; col < _gridCols; col++)
        for (int row = 0; row < _gridRows; row++)
        {
            peanoCurves[col, row] = GeneratePeanoCurve(_cellWidth, _cellHeight);
            peanoIndex[col, row] = 0;
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

            var curve = peanoCurves[col, row];
            int idx = peanoIndex[col, row];

            int step = 2; // z.B. nur jeden zweiten Punkt
            while (idx < curve.Count)
            {
                int x = col * _cellWidth + curve[idx].x;
                int y = row * _cellHeight + curve[idx].y;
                peanoIndex[col, row] += step;
                if (x < width && y < height && !visited[x, y])
                {
                    visited[x, y] = true;
                    visitedCount++;
                    return (x, y);
                }
                idx = peanoIndex[col, row];
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

    // Peano-Kurven-Generator (rekursiv, für quadratische Zellen mit Seitenlänge als Potenz von 3)
    private List<(int x, int y)> GeneratePeanoCurve(int w, int h)
    {
        var result = new List<(int x, int y)>();
        int order = (int)Math.Log(Math.Min(w, h), 3);
        int size = (int)Math.Pow(3, order);
        PeanoRecursive(0, 0, size, result);
        return result;
    }

    private void PeanoRecursive(int x0, int y0, int size, List<(int x, int y)> result)
    {
        if (size == 1)
        {
            result.Add((x0, y0));
            return;
        }
        int step = size / 3;
        for (int dy = 0; dy < 3; dy++)
        for (int dx = 0; dx < 3; dx++)
            PeanoRecursive(x0 + dx * step, y0 + dy * step, step, result);
    }
}