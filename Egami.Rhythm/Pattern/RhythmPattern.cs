namespace Egami.Rhythm.Pattern;

public sealed class RhythmPattern
{
    public int StepsTotal { get; }
    public bool[] Hits { get; }          // Länge = StepsTotal
    public byte[] Velocity { get; }      // optional akzentuierte Werte, Länge = StepsTotal
    public int[] Lengths { get; }        // optional Längen in Steps (per Index), Länge = StepsTotal

    public RhythmPattern(int stepsTotal)
    {
        StepsTotal = stepsTotal;
        Hits = new bool[stepsTotal];
        Velocity = new byte[stepsTotal];     // 0 => keine Note
        Lengths = new int[stepsTotal];       // 0 => Default 1
    }

    public IEnumerable<RhythmEvent> ToEvents()
    {
        for (int i = 0; i < StepsTotal; i++)
        {
            if (!Hits[i]) continue;
            var vel = Velocity[i] == 0 ? (byte)100 : Velocity[i];
            var len = Lengths[i] <= 0 ? 1 : Lengths[i];
            yield return new RhythmEvent(i, vel, len);
        }
    }

    public RhythmPattern Clone()
    {
        var p = new RhythmPattern(StepsTotal);
        Array.Copy(Hits, p.Hits, StepsTotal);
        Array.Copy(Velocity, p.Velocity, StepsTotal);
        Array.Copy(Lengths, p.Lengths, StepsTotal);
        return p;
    }
}