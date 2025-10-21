using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public sealed class BernoulliGenerator(double probability01 = 0.5) : IRhythmGenerator
{
    private readonly double _p = Math.Clamp(probability01, 0.0, 1.0);

    public Sequence Generate(RhythmContext ctx)
    {
        var rng = RandomProvider.Get(ctx.Seed);
        var s = new Sequence(ctx.StepsTotal);
        for (int i = 0; i < s.StepsTotal; i++)
        {
            s.Steps[i].Hit = rng.NextDouble() < _p;
            s.Steps[i].Velocity = s.Steps[i].Hit ? ctx.DefaultVelocity : 0;
            s.Steps[i].Pitch = s.Steps[i].Hit ? 60 : 0;
            s.Steps[i].Length = 1;
        }
        return s;
    }
}