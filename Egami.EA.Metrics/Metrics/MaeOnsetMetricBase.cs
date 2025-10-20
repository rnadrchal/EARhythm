using Egami.EA.Metrics.Util;

namespace Egami.EA.Metrics.Metrics;

/// <summary>
/// Basis für MAE-Similarities auf Pitches/Velocities/Lengths.
/// Vergleicht nur Positionen, an denen in beiden Sequenzen Hits=true sind (Onset-Match).
/// </summary>
public abstract class MaeOnsetMetricBase : IFitnessMetric
{
    public abstract string Name { get; }

    /// <summary>Maximalwert für die Normalisierung (z.B. 127 für MIDI).</summary>
    protected abstract int MaxValue { get; set; }

    /// <summary>Extrahiert den Wert des Features an Position i.</summary>
    protected abstract int GetValue(MetricsSequence s, int i);

    public double Evaluate(MetricsSequence start, MetricsSequence target)
    {
        int n = Math.Min(start.Length, target.Length);
        if (n == 0) return 1.0;

        long sumAbs = 0;
        int count = 0;
        var aHits = start.Hits;
        var bHits = target.Hits;

        for (int i = 0; i < n; i++)
        {
            if (aHits[i] && bHits[i])
            {
                int va = GetValue(start, i);
                int vb = GetValue(target, i);
                int d = MathUtil.Abs(va - vb);
                sumAbs += d;
                count++;
            }
        }

        if (count == 0)
        {
            // Kein gemeinsamer Onset: Wenn beide komplett „stumm“ wären, wäre Hits.F1=1,
            // aber hier ohne Kontext: neutral → 0.0 (oder 1.0 je nach Policy).
            return 0.0;
        }

        double mae = sumAbs / (double)count;
        double sim = 1.0 - (mae / Math.Max(1, MaxValue));
        return MathUtil.Clamp01(sim);
    }
}

public sealed class PitchMaeMetric : MaeOnsetMetricBase
{
    public override string Name => "Pitch.MAE";
    protected override int MaxValue { get; set; } = 127;
    protected override int GetValue(MetricsSequence s, int i) => s.Pitches[i];
}

public sealed class VelocityMaeMetric : MaeOnsetMetricBase
{
    public override string Name => "Velocity.MAE";
    protected override int MaxValue { get; set; } = 127;
    protected override int GetValue(MetricsSequence s, int i) => s.Velocities[i];
}

public sealed class LengthMaeMetric : MaeOnsetMetricBase
{
    public override string Name => "Length.MAE";

    /// <summary>
    /// Normierungsobergrenze für Längen.
    /// Setze dies in der FastBundleFitness sinnvoll, z.B. Gridlänge oder erwartetes Lmax.
    /// </summary>
    public int LMax { get; set; } = 32;

    protected override int MaxValue { get; set; } = 32;
    protected override int GetValue(MetricsSequence s, int i) => s.Lengths[i];
}
