using Egami.Sequencer.Pcs;
using Melanchall.DryWetMidi.MusicTheory;

namespace Egami.Sequencer.Extensions;

public static class IntervalConversionExtensions
{
    /// <summary>
    /// Wandelt ein PitchClassSet in eine Intervallfolge um, die mit DryWetMIDI-kompatibel ist.
    /// Beispiel: {0,2,3,7} → M2, m2, P4
    /// Hinweis: 
    ///     - Reihenfolge basiert auf der sortierten Form des PCS.
    ///     - Es wird KEIN zyklischer Abschluss (zurück zum Root) erzeugt.
    /// </summary>
    public static IEnumerable<Interval> ToIntervals(this PitchClassSet pcs)
    {
        var ordered = pcs.PitchClasses.OrderBy(pc => pc).ToArray();

        for (int i = 0; i < ordered.Length - 1; i++)
        {
            int diff = (ordered[i + 1] - ordered[i] + 12) % 12;
            yield return Interval.FromHalfSteps(diff);
        }
    }

    /// <summary>
    /// Wandelt eine Intervallfolge in ein PitchClassSet um.
    /// Startpunkt ist Pitch Class 0.
    /// Beispiel: M2, M2, m2 → {0,2,4,5}
    /// </summary>
    public static PitchClassSet ToPitchClassSet(this IEnumerable<Interval> intervals)
    {
        var pcs = new HashSet<int>();
        int runningPc = 0;
        pcs.Add(0);

        foreach (var interval in intervals)
        {
            runningPc = (runningPc + interval.HalfSteps) % 12;
            pcs.Add(runningPc);
        }

        return new PitchClassSet(pcs);
    }

    /// <summary>
    /// Wandelt eine beliebige Menge an Intervallen in ein PCS um,
    /// interpretierend als Halbtonwerte (Intervalle müssen nicht zusammenhängend sein).
    /// Beispiel: {M3, P5} → {0,4,7}
    /// </summary>
    public static PitchClassSet ToPitchClassSetFromIndependentIntervals(
        this IEnumerable<Interval> intervals,
        int root = 0)
    {
        var pcs = new HashSet<int>();
        pcs.Add(((root % 12) + 12) % 12);

        foreach (var interval in intervals)
        {
            int pc = (root + interval.HalfSteps) % 12;
            if (pc < 0) pc += 12;
            pcs.Add(pc);
        }

        return new PitchClassSet(pcs);
    }
}