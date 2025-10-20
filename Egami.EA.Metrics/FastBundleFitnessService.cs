using Egami.EA.Metrics.Metrics;
using Egami.EA.Metrics.Weighting;

namespace Egami.EA.Metrics;

public sealed class FastBundleFitnessService : IFitnessService
{
    private readonly IFitnessServiceOptions _options;
    private readonly HitsF1Metric _hits = new();
    private readonly PitchMaeMetric _pitch = new();
    private readonly VelocityMaeMetric _vel = new();
    private readonly LengthMaeMetric _len = new();
    private CombineMode _mode;

    public FastBundleFitnessService(IFitnessServiceOptions options)
    {
        _options = options;
        ApplyOptions();
    }

    public void ApplyOptions()
    {
        _hits.MaxCircularShift = _options.MaxCircularShift;
        _len.LMax = _options.LMax;
        _mode = _options.CombineMode;
    }

    public double Evaluate(MetricsSequence start, MetricsSequence target)
        => EvaluateDetailed(start, target).Total;

    public FitnessBreakdown EvaluateDetailed(MetricsSequence start, MetricsSequence target)
    {
        // Einzelmetriken berechnen
        double sHits = _hits.Evaluate(start, target);
        double sPitch = _pitch.Evaluate(start, target);
        double sVel = _vel.Evaluate(start, target);
        double sLen = _len.Evaluate(start, target);

        // Lethal-Regel
        if (_options.UseLethalThreshold && sHits < _options.HitsLethalThreshold)
            return new FitnessBreakdown(0.0, sHits, sPitch, sVel, sLen);

        // Kombinieren
        var sims = new ReadOnlySpan<double>(new[] { sHits, sPitch, sVel, sLen });
        var w = new ReadOnlySpan<double>(new[] { _options.W_Hits, _options.W_Pitch, _options.W_Vel, _options.W_Len });

        double total = _mode switch
        {
            CombineMode.Arithmetic => CombineArithmetic(sims, w),
            CombineMode.Geometric => CombineGeometric(sims, w),
            _ => 0.0
        };

        return new FitnessBreakdown(total, sHits, sPitch, sVel, sLen);
    }

    private static double CombineArithmetic(ReadOnlySpan<double> sims, ReadOnlySpan<double> w)
    {
        double acc = 0, ws = 0;
        for (int i = 0; i < sims.Length; i++)
        {
            if (w[i] <= 0) continue;
            double s = sims[i];
            if (s < 0) s = 0; else if (s > 1) s = 1;
            acc += w[i] * s;
            ws += w[i];
        }
        return ws > 0 ? acc / ws : 0.0;
    }

    private static double CombineGeometric(ReadOnlySpan<double> sims, ReadOnlySpan<double> w)
    {
        const double eps = 1e-9;
        double acc = 0, ws = 0;
        for (int i = 0; i < sims.Length; i++)
        {
            if (w[i] <= 0) continue;
            double s = sims[i];
            if (s < 0) s = 0; else if (s > 1) s = 1;
            acc += w[i] * Math.Log(eps + s);
            ws += w[i];
        }
        return ws > 0 ? Math.Exp(acc / ws) : 0.0;
    }
}
