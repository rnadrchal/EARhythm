using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public sealed class BernoulliGenerator(double probability01 = 0.5) : IRhythmGenerator
{
    private readonly double _p = Math.Clamp(probability01, 0.0, 1.0);

    public RhythmPattern Generate(RhythmContext ctx)
    {
        var rng = RandomProvider.Get(ctx.Seed);
        var p = new RhythmPattern(ctx.StepsTotal);
        for (int i = 0; i < p.StepsTotal; i++)
        {
            bool hit = rng.NextDouble() < _p;
            p.Hits[i] = hit;
            p.Lengths[i] = 1;
            p.Velocities[i] = (byte) (hit ? ctx.DefaultVelocity : 0);
        }
        return p;
    }
}