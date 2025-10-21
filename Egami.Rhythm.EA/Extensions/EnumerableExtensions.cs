namespace Egami.Rhythm.EA.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Gibt n zufällige, unterschiedliche Elemente aus einer Sequenz zurück.
    /// </summary>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int n, Random? rng = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        rng ??= new Random();

        var list = source.ToList();
        int count = list.Count;
        if (n >= count) return list.OrderBy(_ => rng.Next());

        // Fisher-Yates Shuffle für die ersten n Elemente
        for (int i = 0; i < n; i++)
        {
            int j = rng.Next(i, count);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list.Take(n);
    }
}