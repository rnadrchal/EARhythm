namespace Egami.EA.Metrics.Metrics;


/// <summary>
/// F1-Score der Onsets (Hits) mit optionaler zirkulärer Best-Shift-Suche.
/// Sehr schnell und robust. Ergebnis ∈ [0..1].
/// </summary>
public sealed class HitsF1Metric : IFitnessMetric
{
    public string Name => "Hits.F1";

    /// <param name="maxCircularShift">Max. zirkulärer Shift (±K). 0 = kein Shift.</param>
    public int MaxCircularShift { get; set; } = 0;

    public double Evaluate(MetricsSequence start, MetricsSequence target)
    {
        int n = Math.Min(start.Length, target.Length);
        if (n == 0) return 1.0; // zwei leere Sequenzen gelten als identisch

        // Falls Längen differieren, betrachten wir nur die ersten n Schritte
        var a = start.Hits;
        var b = target.Hits;

        if (MaxCircularShift <= 0)
            return F1Core(a, b, n);

        double best = 0.0;
        int K = Math.Min(MaxCircularShift, n - 1);
        for (int k = -K; k <= K; k++)
        {
            // b um k zirkulär shiften
            double f1 = F1Shifted(a, b, n, k);
            if (f1 > best) best = f1;
            if (best >= 1.0) break;
        }
        return best;
    }

    private static double F1Core(bool[] a, bool[] b, int n)
    {
        int tp = 0, fp = 0, fn = 0;
        for (int i = 0; i < n; i++)
        {
            bool ah = a[i];
            bool bh = b[i];
            if (ah & bh) tp++;
            else if (ah & !bh) fp++;
            else if (!ah & bh) fn++;
        }
        if (tp == 0) return (fp == 0 && fn == 0) ? 1.0 : 0.0;

        double p = tp / (double)(tp + fp);
        double r = tp / (double)(tp + fn);
        return (2.0 * p * r) / (p + r);
    }

    private static double F1Shifted(bool[] a, bool[] b, int n, int shift)
    {
        int tp = 0, fp = 0, fn = 0;
        if (shift == 0) return F1Core(a, b, n);

        // b[(i - shift + n) % n]
        for (int i = 0; i < n; i++)
        {
            bool ah = a[i];
            int j = i - shift;
            if (j < 0) j += n;
            else if (j >= n) j -= n;
            bool bh = b[j];

            if (ah & bh) tp++;
            else if (ah & !bh) fp++;
            else if (!ah & bh) fn++;
        }
        if (tp == 0) return (fp == 0 && fn == 0) ? 1.0 : 0.0;
        double p = tp / (double)(tp + fp);
        double r = tp / (double)(tp + fn);
        return (2.0 * p * r) / (p + r);
    }
}
