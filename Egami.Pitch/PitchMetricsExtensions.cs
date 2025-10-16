namespace Egami.Pitch;

public static class PitchMetricsExtensions
{
    /// <summary>
    /// Gebänderte Levenshtein-Ähnlichkeit (Ukkonen) mit maximaler Distanz k.
    /// Rückgabe 0..1 (1 = identisch). O(k * min(n,m)) Zeit, O(min(n,m)) Speicher.
    /// </summary>
    public static double BandedLevenshteinSimilarity(this IList<int?> a, IList<int?> b, int k = 3)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0;
        if (a.Count == 0 || b.Count == 0) return 0.0;

        // Kürzere Sequenz nach 'shorter'
        if (a.Count > b.Count) (a, b) = (b, a);
        int n = a.Count, m = b.Count;

        // Wenn Längenunterschied > k, kann Distanz nie <= k werden (früher Abbruch)
        int lenDiff = m - n;
        if (lenDiff > k) return 0.0;

        // Dynamischer Bandradius: mindestens lenDiff
        int band = Math.Max(k, lenDiff);

        // Zwei Zeilen (rolling), Länge n+1
        Span<int> prev = n + 1 <= 256 ? stackalloc int[n + 1] : new int[n + 1];
        Span<int> curr = n + 1 <= 256 ? stackalloc int[n + 1] : new int[n + 1];

        for (int i = 0; i <= n; i++) prev[i] = i;

        // Hauptschleife über b (Spalten)
        for (int j = 1; j <= m; j++)
        {
            // Bandgrenzen in a (Zeilen)
            int iStart = Math.Max(1, j - band);
            int iEnd = Math.Min(n, j + band);

            // Vor dem Band: setze Curr auf "unendlich" (hier: k+1 als harte Schranke)
            // und initialisiere curr[0], falls Band bei i=1 startet.
            for (int t = 0; t < iStart; t++) curr[t] = k + 1;
            if (iStart == 1) curr[0] = j;

            int minInRow = int.MaxValue;

            for (int i = iStart; i <= iEnd; i++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                // Drei Nachbarn: Löschen (prev[i] + 1), Einfügen (curr[i-1] + 1), Ersetzen (prev[i-1] + cost)
                int del = prev[i] + 1;
                int ins = curr[i - 1] + 1;
                int sub = prev[i - 1] + cost;

                int val = del < ins ? del : ins;
                if (sub < val) val = sub;

                // Hard-Cap auf k+1 für schnelleren Early-Abandon
                if (val > k + 1) val = k + 1;

                curr[i] = val;
                if (val < minInRow) minInRow = val;
            }

            // Nach dem Band: setze Rest auf k+1, damit kein Einfluss
            for (int t = iEnd + 1; t <= n; t++) curr[t] = k + 1;

            // Early abandon: gesamte Spalte > k ⇒ keine Chance mehr
            if (minInRow > k) return 0.0;

            // Roll
            var tmp = prev; prev = curr; curr = tmp;
        }

        int dist = prev[n];                 // finale Distanz
        if (dist > k) return 0.0;           // falls Limit überschritten

        // Normalisierung: worst case ~ max(n,m); hier m (längere Sequenz)
        double sim = 1.0 - (double)dist / Math.Max(1, m);
        return sim < 0 ? 0 : (sim > 1 ? 1 : sim);
    }
}