namespace Egami.EA.Metrics.Metrics;

public class HitCountMetric : IFitnessMetric
{
    public double Evaluate(MetricsSequence start, MetricsSequence target)
    {
        var c1 = start.Hits.Count(h => h);
        var c2 = target.Hits.Count(h => h);
        return (double)Math.Min(c1, c2) / Math.Max(c1, c2);
    }

    public string Name => "Hits.Count";
}