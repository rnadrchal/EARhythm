namespace Egami.EA.Metrics.Metrics;

public class CombinedBinarySimilarity : IFitnessMetric
{
    public double Evaluate(MetricsSequence start, MetricsSequence target)
    {
        return start.Hits.CombinedRhythmSimilarity(target.Hits);
    }

    public string Name { get; }
}