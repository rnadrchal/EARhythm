using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Extensions;

public static class MetricsExtensions
{
    // <summary>
    /// Rotationsinvariante Hamming-Ähnlichkeit zwischen zwei binären Patterns.
    /// Gibt einen Wert in [0..1] zurück; 1 == identisch (bis auf Rotation).
    /// </summary>
    public static double HammingSimilarityRot(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        if (pattern.Length != reference.Length) throw new ArgumentException("Lengths must match.");
        int n = pattern.Length;
        if (n == 0) return 1.0;

        int minHamming = n;
        for (int shift = 0; shift < n; shift++)
        {
            int d = 0;
            for (int i = 0; i < n; i++)
            {
                if (pattern[(i + shift) % n] != reference[i]) d++;
            }
            if (d < minHamming) minHamming = d;
            if (minHamming == 0) break;
        }
        return 1.0 - (double)minHamming / n;
    }

    // <summary>
    /// Rotationsinvariante Hamming-Ähnlichkeit zwischen zwei binären Patterns.
    /// Gibt einen Wert in [0..1] zurück; 1 == identisch (bis auf Rotation).
    /// </summary>
    public static double HammingSimilarityRot(this RhythmPattern pattern, RhythmPattern reference)
    {
        return pattern.Hits.HammingSimilarityRot(reference.Hits);
    }

    /// <summary>
    /// Jaccard-Ähnlichkeit der Onset-Mengen (Positionsmengen der 1en).
    /// Wertebereich [0..1]; 1 == gleiche Onset-Positionen.
    /// </summary>
    public static double JaccardSimilarity(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        if (pattern.Length != reference.Length) throw new ArgumentException("Lengths must match.");
        var A = pattern.GetOnsetPositions();
        var B = reference.GetOnsetPositions();

        if (A.Count == 0 && B.Count == 0) return 1.0;
        var setA = new HashSet<int>(A);
        var setB = new HashSet<int>(B);

        int inter = setA.Intersect(setB).Count();
        int union = setA.Union(setB).Count();
        return union == 0 ? 1.0 : (double)inter / union;
    }

    /// <summary>
    /// Jaccard-Ähnlichkeit der Onset-Mengen (Positionsmengen der 1en).
    /// Wertebereich [0..1]; 1 == gleiche Onset-Positionen.
    /// </summary>
    public static double JaccardSimilarity(this RhythmPattern pattern, RhythmPattern reference)
    {
        return pattern.Hits.JaccardSimilarity(reference.Hits);
    }

    /// <summary>
    /// IOI-MAE: Mittlerer absoluter Fehler der Inter-Onset-Intervalle (zirkular).
    /// Rotationsinvariant, auf [0..1] normalisiert (0 == perfekte Übereinstimmung).
    /// Bei k&lt;2 (zu wenige Onsets) wird 0 zurückgegeben, wenn beide k&lt;2 sind, sonst 1.
    /// </summary>
    public static double IoiMae(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        if (pattern.Length != reference.Length) throw new ArgumentException("Lengths must match.");
        int n = pattern.Length;
        var ioisA = pattern.GetIois();
        var ioisB = reference.GetIois();

        // Weniger als zwei Onsets => keine sinnvolle IOI-Sequenz
        bool trivialA = ioisA.Count == 0;
        bool trivialB = ioisB.Count == 0;
        if (trivialA && trivialB) return 0.0;
        if (trivialA ^ trivialB) return 1.0;

        if (ioisA.Count != ioisB.Count)
        {
            // Unterschiedliche Anzahl Onsets => IOI-Vergleich unfair -> maximaler Fehler
            return 1.0;
        }

        // Rotationsinvarianz: beste zyklische Rotation der IOI-Sequenzen wählen
        double bestMae = double.PositiveInfinity;
        int k = ioisA.Count;
        for (int shift = 0; shift < k; shift++)
        {
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                sum += Math.Abs(ioisA[i] - ioisB[(i + shift) % k]);
            }
            double mae = sum / k;
            if (mae < bestMae) bestMae = mae;
        }

        // Normierung: IOIs summieren sich zu n; max. Abweichung pro Intervall <= n
        // Sinnvoller ist Normierung durch (n/2): pro Intervall ist die „zirkulare“ Maxabweichung n/2.
        double norm = n / 2.0;
        return Math.Min(1.0, bestMae / norm);
    }

    /// <summary>
    /// IOI-MAE: Mittlerer absoluter Fehler der Inter-Onset-Intervalle (zirkular).
    /// Rotationsinvariant, auf [0..1] normalisiert (0 == perfekte Übereinstimmung).
    /// Bei k&lt;2 (zu wenige Onsets) wird 0 zurückgegeben, wenn beide k&lt;2 sind, sonst 1.
    /// </summary>
    public static double IoiMae(this RhythmPattern pattern, RhythmPattern reference)
    {
        return pattern.Hits.IoiMae(reference.Hits);
    }

    /// <summary>
    /// Zirkulare Wasserstein-1-Distanz (Earth Mover's Distance) zwischen Onsets, normalisiert auf [0..1].
    /// 0 == identisch (bis auf Rotation). Erwartet gleiche Onset-Anzahl; sonst 1.0.
    /// </summary>
    public static double WassersteinCircular(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        if (pattern.Length != reference.Length) throw new ArgumentException("Lengths must match.");
        int n = pattern.Length;
        var A = pattern.GetOnsetPositions();
        var B = reference.GetOnsetPositions();

        int kA = A.Count, kB = B.Count;
        if (kA == 0 && kB == 0) return 0.0;
        if (kA == 0 ^ kB == 0) return 1.0;
        if (kA != kB) return 1.0;

        A.Sort();
        B.Sort();
        int k = kA;

        // Für alle zyklischen Alignments von B zu A die Summe der minimalen Kreisabstände berechnen
        double best = double.PositiveInfinity;
        for (int shift = 0; shift < k; shift++)
        {
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                int a = A[i];
                int b = B[(i + shift) % k];
                int d = Math.Abs(a - b);
                int circ = Math.Min(d, n - d);
                sum += circ;
            }
            if (sum < best) best = sum;
        }

        // Durchschnitt pro Onset und Normierung: maximale Kreisdistanz pro Paar ist n/2
        double avg = best / k;
        double norm = n / 2.0;
        return Math.Min(1.0, avg / norm);
    }

    /// <summary>
    /// Zirkulare Wasserstein-1-Distanz (Earth Mover's Distance) zwischen Onsets, normalisiert auf [0..1].
    /// 0 == identisch (bis auf Rotation). Erwartet gleiche Onset-Anzahl; sonst 1.0.
    /// </summary>
    public static double WassersteinCircular(this RhythmPattern pattern, RhythmPattern reference)
    {
        return pattern.Hits.WassersteinCircular(reference.Hits);
    }

    /// <summary>
    /// Cosine-Ähnlichkeit der zirkularen Autokorrelationsvektoren (optional ohne Lag 0).
    /// Wertebereich [0..1]; 1 == sehr ähnliche Pulsstruktur.
    /// </summary>
    public static double AutocorrCosineSimilarity(this bool[] pattern, bool[] reference, bool excludeZeroLag = true)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        if (pattern.Length != reference.Length) throw new ArgumentException("Lengths must match.");
        int n = pattern.Length;
        if (n == 0) return 1.0;

        var acA = pattern.Autocorr();
        var acB = reference.Autocorr();

        int start = excludeZeroLag ? 1 : 0;
        int len = n - start;

        double dot = 0, na = 0, nb = 0;
        for (int i = start; i < n; i++)
        {
            double a = acA[i];
            double b = acB[i];
            dot += a * b;
            na += a * a;
            nb += b * b;
        }

        if (na == 0 && nb == 0) return 1.0; // beide völlig leer/gleichmäßig
        if (na == 0 || nb == 0) return 0.0;

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    /// <summary>
    /// Cosine-Ähnlichkeit der zirkularen Autokorrelationsvektoren (optional ohne Lag 0).
    /// Wertebereich [0..1]; 1 == sehr ähnliche Pulsstruktur.
    /// </summary>
    public static double AutocorrCosineSimilarity(this RhythmPattern pattern, RhythmPattern reference, bool excludeZeroLag = true)
    {
        return pattern.Hits.AutocorrCosineSimilarity(reference.Hits, excludeZeroLag);
    }

    // ------------------------
    // Hilfsfunktionen
    // ------------------------

    /// <summary>Positionsliste der Onsets (Indices mit Wert true).</summary>
    public static List<int> GetOnsetPositions(this bool[] pattern)
    {
        var pos = new List<int>(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
            if (pattern[i]) pos.Add(i);
        return pos;
    }

    /// <summary>Inter-Onset-Intervalle (zirkular, in Steps). Gibt leere Liste zurück, wenn weniger als 2 Onsets.</summary>
    public static List<int> GetIois(this bool[] pattern)
    {
        var pos = pattern.GetOnsetPositions();
        int k = pos.Count;
        if (k < 2) return new List<int>();

        pos.Sort();
        int n = pattern.Length;
        var iois = new List<int>(k);
        for (int i = 0; i < k; i++)
        {
            int a = pos[i];
            int b = pos[(i + 1) % k];
            int d = (b - a + n) % n;
            if (d == 0) d = n; // falls alle Onsets gleich (theoretisch)
            iois.Add(d);
        }
        return iois;
    }

    /// <summary>Zirkulare Autokorrelation (binär): für jedes Lag τ die Anzahl von 1-Überlappungen.</summary>
    public static int[] Autocorr(this bool[] pattern)
    {
        int n = pattern.Length;
        var ac = new int[n];
        for (int tau = 0; tau < n; tau++)
        {
            int sum = 0;
            for (int i = 0; i < n; i++)
            {
                if (pattern[i] && pattern[(i + tau) % n]) sum++;
            }
            ac[tau] = sum;
        }
        return ac;
    }

    /// <summary>
    /// (Optional) Erzeugt das euklidische Pattern E(n,k) via Bjorklund (gleichmäßige Verteilung).
    /// </summary>
    public static bool[] EuclideanPattern(int n, int k)
    {
        if (n <= 0) return Array.Empty<bool>();
        if (k <= 0) return new bool[n];
        if (k >= n) return Enumerable.Repeat(true, n).ToArray();

        // Bjorklund-Algorithmus (kompakt)
        var counts = new List<int>();
        var remainders = new List<int> { k };
        int div = n - k;
        int level = 0;
        while (true)
        {
            counts.Add(div / remainders[level]);
            remainders.Add(div % remainders[level]);
            div = remainders[level];
            level++;
            if (remainders[level] <= 1) break;
        }
        counts.Add(div);

        List<int> Build(int l)
        {
            if (l == -1) return new List<int> { 0 };          // Rest-Gruppe: 0 (Pause)
            if (l == -2) return new List<int> { 1 };          // Primär-Gruppe: 1 (Onset)
            var seq = new List<int>();
            var a = Build(l - 1);
            var b = Build(l - 2);
            for (int i = 0; i < counts[l]; i++) seq.AddRange(a);
            if (remainders[l] != 0) seq.AddRange(b);
            return seq;
        }

        var pattern = Build(level).Select(x => x == 1).ToList();
        // Auf Länge n trimmen (sicherheitshalber)
        if (pattern.Count > n) pattern = pattern.Take(n).ToList();
        return pattern.ToArray();
    }
}