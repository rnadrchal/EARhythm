using Egami.EA.Metrics.Util;

namespace Egami.EA.Metrics.Weighting;

public enum CombineMode
{
    Arithmetic,
    Geometric
}

public sealed class WeightedCombiner
{
    public CombineMode Mode { get; init; } = CombineMode.Arithmetic;

    /// <summary>
    /// Kombiniert bereits normalisierte Teilmetriken (0..1) mit Gewichten w[i] ≥ 0.
    /// </summary>
    public double Combine(ReadOnlySpan<double> sims, ReadOnlySpan<double> weights)
    {
        if (sims.Length != weights.Length) throw new ArgumentException("Length mismatch.");
        if (sims.Length == 0) return 0.0;

        switch (Mode)
        {
            case CombineMode.Arithmetic:
            {
                double ws = 0.0, acc = 0.0;
                for (int i = 0; i < sims.Length; i++)
                {
                    double w = weights[i];
                    if (w <= 0) continue;
                    ws += w;
                    acc += w * MathUtil.Clamp01(sims[i]);
                }
                return ws > 0 ? acc / ws : 0.0;
            }
            case CombineMode.Geometric:
            {
                double ws = 0.0, acc = 0.0;
                const double eps = 1e-9;
                for (int i = 0; i < sims.Length; i++)
                {
                    double w = weights[i];
                    if (w <= 0) continue;
                    ws += w;
                    acc += w * Math.Log(eps + MathUtil.Clamp01(sims[i]));
                }
                return ws > 0 ? Math.Exp(acc / ws) : 0.0;
            }
            default:
                return 0.0;
        }
    }
}