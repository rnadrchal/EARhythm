using Egami.EA.Metrics.Weighting;

namespace Egami.EA.Metrics.Metrics;

/// <summary>
/// "Fast Bundle": Hits(F1 mit optionalem zirkulären Best-Shift),
/// MAE(Pitch), MAE(Velocity), MAE(Length) – nur auf gemeinsamen Onsets.
/// </summary>
public sealed class FastBundleFitness : IFitnessMetric
{
    public string Name => "Bundle.Fast";

    private readonly IFitnessMetric _hits;
    public IFitnessMetric Hits => _hits;

    public FastBundleFitness(IFitnessMetric hits)
    {
        _hits = hits;
    }

    // Teilmetriken (können extern ausgetauscht / parametrisiert werden)
    public PitchMaeMetric Pitch { get; init; } = new();
    public VelocityMaeMetric Velocity { get; init; } = new();
    public LengthMaeMetric Length { get; init; } = new() { LMax = 32 };

    // Gewichte (Default wie vorgeschlagen)
    public double W_Hits { get; init; } = 0.40;
    public double W_Pitch { get; init; } = 0.25;
    public double W_Vel { get; init; } = 0.20;
    public double W_Len { get; init; } = 0.15;

    public CombineMode Mode { get; init; } = CombineMode.Arithmetic;

    /// <summary>Lethal-Regel: Wenn Hits-Similarity unter Schwelle fällt, setze Gesamtfitness auf 0.</summary>
    public bool UseLethalThreshold { get; init; } = true;
    public double HitsLethalThreshold { get; init; } = 0.15;

    public double Evaluate(MetricsSequence start, MetricsSequence target)
    {
        double sHits = Hits.Evaluate(start, target);

        if (UseLethalThreshold && sHits < HitsLethalThreshold)
            return 0.0;

        double sPitch = Pitch.Evaluate(start, target);
        double sVel = Velocity.Evaluate(start, target);
        double sLen = Length.Evaluate(start, target);

        var sims = new double[] { sHits, sPitch, sVel, sLen };
        var ws = new double[] { W_Hits, W_Pitch, W_Vel, W_Len };

        var combiner = new WeightedCombiner { Mode = Mode };
        return combiner.Combine(sims, ws);
    }
}