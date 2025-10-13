using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Extensions;

public static class MetricsExtensions
{
    private const int MaxLift = 16384; // Sicherheitskappe gegen riesige LCMs

    // <summary>
    /// Rotationsinvariante Hamming-Ähnlichkeit zwischen zwei binären Patterns.
    /// Gibt einen Wert in [0..1] zurück; 1 == identisch (bis auf Rotation).
    /// </summary>
    /// <summary>
    /// Rotationsinvariante Hamming-Ähnlichkeit in [0..1]. Arbeitet via LCM-Lift.
    /// 1 = identisch bis auf Rotation, 0 = maximal verschieden.
    /// </summary>
    public static double HammingSimilarityRot(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        var (A, B) = LiftToCommonCycle(pattern, reference, out int L);
        if (L == 0) return 1.0;

        int minHamming = L;
        for (int shift = 0; shift < L; shift++)
        {
            int d = 0;
            for (int i = 0; i < L; i++)
                if (A[(i + shift) % L] != B[i]) d++;
            if (d < minHamming) minHamming = d;
            if (minHamming == 0) break;
        }
        return 1.0 - (double)minHamming / L;
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
    /// Jaccard-Ähnlichkeit der Onset-Mengen in [0..1]. Arbeitet via LCM-Lift.
    /// </summary>
    public static double JaccardSimilarity(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        var (A, B) = LiftToCommonCycle(pattern, reference, out int L);
        if (L == 0) return 1.0;

        var setA = A.GetOnsetPositions();
        var setB = B.GetOnsetPositions();
        if (setA.Count == 0 && setB.Count == 0) return 1.0;

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
    /// IOI-MAE (0 = perfekt, 1 = schlecht). Via LCM-Lift.
    /// Wenn Onset-Anzahlen ungleich bleiben, wird 1.0 zurückgegeben.
    /// </summary>
    public static double IoiMae(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        var (A, B) = LiftToCommonCycle(pattern, reference, out int L);
        if (L == 0) return 0.0;

        var ioisA = A.GetIois();
        var ioisB = B.GetIois();

        bool trivialA = ioisA.Count == 0;
        bool trivialB = ioisB.Count == 0;
        if (trivialA && trivialB) return 0.0;
        if (trivialA ^ trivialB) return 1.0;
        if (ioisA.Count != ioisB.Count) return 1.0;

        double bestMae = double.PositiveInfinity;
        int k = ioisA.Count;
        for (int s = 0; s < k; s++)
        {
            double sum = 0;
            for (int i = 0; i < k; i++)
                sum += Math.Abs(ioisA[i] - ioisB[(i + s) % k]);
            bestMae = Math.Min(bestMae, sum / k);
        }
        double norm = L / 2.0; // zirkulare Maxabweichung
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
    /// Cosine-Ähnlichkeit der zirkularen Autokorrelationen (0..1; 1 = sehr ähnlich).
    /// Arbeitet via LCM-Lift; optional Lag 0 ausschließen.
    /// </summary>
    public static double AutocorrCosineSimilarity(this bool[] pattern, bool[] reference, bool excludeZeroLag = true)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();
        var (A, B) = LiftToCommonCycle(pattern, reference, out int L);
        if (L == 0) return 1.0;

        var acA = A.Autocorr();
        var acB = B.Autocorr();

        int start = excludeZeroLag ? 1 : 0;
        double dot = 0, na = 0, nb = 0;

        for (int i = start; i < L; i++)
        {
            double a = acA[i], b = acB[i];
            dot += a * b; na += a * a; nb += b * b;
        }

        if (na == 0 && nb == 0) return 1.0;
        if (na == 0 || nb == 0) return 0.0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    /// <summary>
    /// Zirkulare Wasserstein-1-Distanz (EMD) in [0..1]; 0 = identisch, 1 = schlecht.
    /// Arbeitet auf der Einheitskreis-Phase; Längen und Onset-Zahlen dürfen abweichen.
    /// </summary>
    public static double WassersteinCircular(this bool[] pattern, bool[] reference)
    {
        if (pattern is null || reference is null) throw new ArgumentNullException();

        var PhA = ToPhases(pattern);   // sortierte Phasen in [0,1)
        var PhB = ToPhases(reference);

        if (PhA.Count == 0 && PhB.Count == 0) return 0.0;
        if (PhA.Count == 0 ^ PhB.Count == 0) return 1.0;

        PhA.Sort(); PhB.Sort();
        int kA = PhA.Count, kB = PhB.Count;
        int k = Math.Min(kA, kB);

        // Bestes zyklisches Matching der kleineren Menge über Rotation
        double best = double.PositiveInfinity;
        for (int shift = 0; shift < k; shift++)
        {
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                double a = PhA[i];
                double b = PhB[(i + shift) % k];
                double d = Math.Abs(a - b);
                sum += Math.Min(d, 1.0 - d);
            }
            // Surplus-Onsets bestrafen mit maximaler Kreisdistanz 0.5 je Überschuss
            int surplus = Math.Abs(kA - kB);
            sum += surplus * 0.5;
            best = Math.Min(best, sum);
        }

        double avg = best / Math.Max(k, 1);
        return Math.Min(1.0, avg / 0.5); // 0.5 ist maximale Kreisdistanz
    }
    /// <summary>
    /// Zirkulare Wasserstein-1-Distanz (Earth Mover's Distance) zwischen Onsets, normalisiert auf [0..1].
    /// 0 == identisch (bis auf Rotation). Erwartet gleiche Onset-Anzahl; sonst 1.0.
    /// </summary>
    public static double WassersteinCircular(this RhythmPattern pattern, RhythmPattern reference)
    {
        return pattern.Hits.WassersteinCircular(reference.Hits);
    }

    private static List<double> ToPhases(bool[] pattern)
    {
        int n = pattern.Length;
        var phases = new List<double>();
        for (int i = 0; i < n; i++)
            if (pattern[i]) phases.Add((double)i / n); // in [0,1)
        return phases;
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

    private static (bool[] A, bool[] B) LiftToCommonCycle(bool[] a, bool[] b, out int L)
    {
        int n = a.Length, m = b.Length;
        if (n == 0 && m == 0) { L = 0; return (Array.Empty<bool>(), Array.Empty<bool>()); }
        if (n == 0) { L = m; return (new bool[m], b.ToArray()); }
        if (m == 0) { L = n; return (a.ToArray(), new bool[n]); }

        int lcm = LcmSafe(n, m);
        if (lcm > MaxLift)
        {
            // Fallback: begrenzter Lift auf MaxLift per nächstem gemeinsamen Vielfachen
            // (Approximation durch periodische Sample-Hold-Skalierung)
            lcm = MaxLift;
        }
        L = lcm;

        var A = ScaleRepeat(a, L);
        var B = ScaleRepeat(b, L);
        return (A, B);
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0) { int t = a % b; a = b; b = t; }
        return Math.Abs(a);
    }

    private static int LcmSafe(int a, int b)
    {
        if (a == 0 || b == 0) return 0;
        long g = Gcd(a, b);
        long l = (a / g) * (long)b;
        return (int)Math.Min(int.MaxValue, Math.Abs(l));
    }

    private static List<int> GetOnsetPositions(this bool[] pattern)
    {
        var pos = new List<int>();
        for (int i = 0; i < pattern.Length; i++)
            if (pattern[i]) pos.Add(i);
        return pos;
    }

    private static bool[] ScaleRepeat(bool[] src, int L)
    {
        int n = src.Length;
        var dst = new bool[L];
        // Weist jedem Zielindex den Quell-Step via floor(i * n / L) zu (nearest-left)
        for (int i = 0; i < L; i++)
        {
            int idx = (int)((long)i * n / L);
            dst[i] = src[idx];
        }
        return dst;
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
}