namespace Egami.EA.Metrics;

public interface IFitnessService
{
    double Evaluate(Sequence start, Sequence target);
    FitnessBreakdown EvaluateDetailed(Sequence start, Sequence target); // neu
    void ApplyOptions();
}