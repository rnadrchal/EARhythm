using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class GridInterleavedBitmapVisitor : BitmapVisitorBase
{
    private readonly int width, height, gridRows, gridCols;
    private readonly List<(int X, int Y)>[,] fieldQueues;
    private int totalVisited = 0;
    private int fieldIndex = 0;

    public GridInterleavedBitmapVisitor(WriteableBitmap bitmap, int gridRows = 4, int gridCols = 4) : base(bitmap)
    {
        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        this.gridRows = gridRows;
        this.gridCols = gridCols;
        fieldQueues = new List<(int X, int Y)>[gridRows, gridCols];

        int cellWidth = width / gridCols;
        int cellHeight = height / gridRows;

        // Felder initialisieren und Pixel zeilenweise einfügen
        for (int r = 0; r < gridRows; r++)
        for (int c = 0; c < gridCols; c++)
        {
            fieldQueues[r, c] = new List<(int X, int Y)>();
            int x0 = c * cellWidth;
            int y0 = r * cellHeight;
            int x1 = (c == gridCols - 1) ? width : x0 + cellWidth;
            int y1 = (r == gridRows - 1) ? height : y0 + cellHeight;
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                fieldQueues[r, c].Add((x, y));
        }
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        int fields = gridRows * gridCols;
        for (int tries = 0; tries < fields; tries++)
        {
            int r = fieldIndex / gridCols;
            int c = fieldIndex % gridCols;
            fieldIndex = (fieldIndex + 1) % fields;
            if (fieldQueues[r, c].Count > 0)
            {
                var coord = fieldQueues[r, c][0];
                fieldQueues[r, c].RemoveAt(0);
                totalVisited++;
                return coord;
            }
        }
        return null;
    }
}