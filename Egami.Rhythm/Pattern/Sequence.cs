namespace Egami.Rhythm.Pattern;

public sealed class Sequence
{
    public int StepsTotal { get; set; }
    public List<Step> Steps { get; set; }          // Länge = StepsTotal

    public Sequence(int stepsTotal)
    {
        StepsTotal = stepsTotal;
        Steps = Enumerable.Range(0, stepsTotal).Select(_ => new Step()).ToList();
    }

    public bool[] Hits => Steps.Select(s => s.Hit).ToArray();

    public IEnumerable<RhythmEvent> ToEvents()
    {
        for (int i = 0; i < StepsTotal; i++)
        {
            if (!Steps[i].Hit) continue;
            var vel = Steps[i].Velocity == 0 ? (byte)100 : Steps[i].Velocity;
            var len = Steps[i].Length <= 0 ? 1 : Steps[i].Length;
            var pitch = Steps[i].Pitch;
            yield return new RhythmEvent(i, vel, len, pitch);
        }
    }

    public Sequence Clone()
    {
        var p = new Sequence(StepsTotal);
        for (var i = 0; i < p.Steps.Count; i++)
        {
            p.Steps[i].Hit = Steps[i].Hit;
            p.Steps[i].Velocity = Steps[i].Velocity;
            p.Steps[i].Pitch = Steps[i].Pitch;
            p.Steps[i].Length = Steps[i].Length;
        }
        return p;
    }
}