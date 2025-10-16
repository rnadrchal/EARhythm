namespace Egami.EA.Metrics;

public interface IFitnessMetric
{
    /// <summary>Berechnet Ähnlichkeit in [0..1], 1 = identisch.</summary>
    double Evaluate(Sequence start, Sequence target);
    string Name { get; }
}