using System.Windows.Media.Imaging;

namespace Egami.Imaging.Visiting;

public class RandomBitmapVisitor : BitmapVisitorBase
{
    private readonly List<(int X, int Y)> coordinates;
    private int index;

    public RandomBitmapVisitor(WriteableBitmap bitmap, int? seed = null) : base(bitmap)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        coordinates = new List<(int X, int Y)>(width * height);

        // Alle Koordinaten sammeln
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            coordinates.Add((x, y));

        // Mischen (Fisher-Yates)
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        for (int i = coordinates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (coordinates[i], coordinates[j]) = (coordinates[j], coordinates[i]);
        }

        index = 0;
    }

    protected override (int X, int Y)? GetNextCoordinates()
    {
        if (index >= coordinates.Count)
            return null;

        return coordinates[index++];
    }
}