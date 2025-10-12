namespace Egami.Rhythm.Pattern;

public sealed class RhythmPattern
{
    public int StepsTotal { get; set; }
    public bool[] Hits { get; set; }          // Länge = StepsTotal
    public byte[] Velocities { get; set; }      // optional akzentuierte Werte, Länge = StepsTotal
    public int[] Lengths { get; set; }        // optional Längen in Steps (per Index), Länge = StepsTotal
    public int?[] Pitches { get; set; }    // optional Pitch pro Step (per Index), Länge = StepsTotal}

    public RhythmPattern(int stepsTotal)
    {
        StepsTotal = stepsTotal;
        Hits = new bool[stepsTotal];
        Velocities = new byte[stepsTotal];     // 0 => keine Note
        Lengths = new int[stepsTotal];       // 0 => Default 1
        Pitches = new int?[stepsTotal]; // null => kein Pitch
    }

    public IEnumerable<RhythmEvent> ToEvents()
    {
        for (int i = 0; i < StepsTotal; i++)
        {
            if (!Hits[i]) continue;
            var vel = Velocities[i] == 0 ? (byte)100 : Velocities[i];
            var len = Lengths[i] <= 0 ? 1 : Lengths[i];
            var pitch = Pitches[i];
            yield return new RhythmEvent(i, vel, len, pitch);
        }
    }

    public RhythmPattern Clone()
    {
        var p = new RhythmPattern(StepsTotal);
        Array.Copy(Hits, p.Hits, StepsTotal);
        Array.Copy(Velocities, p.Velocities, StepsTotal);
        Array.Copy(Lengths, p.Lengths, StepsTotal);
        Array.Copy(Pitches, p.Pitches, StepsTotal);
        return p;
    }
}