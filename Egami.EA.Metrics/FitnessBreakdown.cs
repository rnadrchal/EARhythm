namespace Egami.EA.Metrics;

public readonly record struct FitnessBreakdown(
    double Total,
    double Hits,
    double Pitch,
    double Velocity,
    double Length
);