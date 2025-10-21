namespace Egami.EA.Metrics;

public interface IFitnessService
{
    double Evaluate(MetricsSequence start, MetricsSequence target);
    FitnessBreakdown EvaluateDetailed(MetricsSequence start, MetricsSequence target); // neu
    void ApplyOptions();
}