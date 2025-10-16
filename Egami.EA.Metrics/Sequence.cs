namespace Egami.EA.Metrics;

public sealed class Sequence
{
    // Grid-basiert: Alle Arrays haben die gleiche Länge N.
    public bool[] Hits { get; }
    public int[] Pitches { get; }      // 0..127 (nur relevant, wo Hits[i]==true)
    public int[] Velocities { get; }   // 0..127
    public int[] Lengths { get; }      // in Grid-Schritten (>=0)

    public int Length => Hits.Length;

    public Sequence(bool[] hits, int[] pitches, int[] velocities, int[] lengths)
    {
        ArgumentNullException.ThrowIfNull(hits);
        ArgumentNullException.ThrowIfNull(pitches);
        ArgumentNullException.ThrowIfNull(velocities);
        ArgumentNullException.ThrowIfNull(lengths);

        if (pitches.Length != hits.Length || velocities.Length != hits.Length || lengths.Length != hits.Length)
            throw new ArgumentException("All arrays must have identical length.");

        // leichte Validierung, clampen überlassen wir den Metriken
        Hits = hits;
        Pitches = pitches;
        Velocities = velocities;
        Lengths = lengths;
    }
}