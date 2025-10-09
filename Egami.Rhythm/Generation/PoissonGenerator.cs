using Egami.Rhythm.Common;
using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public sealed class PoissonGenerator(double lambdaPerBar) : IRhythmGenerator
{
    private readonly double _lambda = Math.Max(0.0001, lambdaPerBar);

    public RhythmPattern Generate(RhythmContext ctx)
    {
        var rng = RandomProvider.Get(ctx.Seed);
        var p = new RhythmPattern(ctx.StepsTotal);

        // Erzeuge Ereignisse innerhalb [0, 1 Bar), verteile nach Exponentialabständen
        double t = 0.0;
        while (t < 1.0)
        {
            // Exponentialverteilung: Abstand = -ln(U)/lambda
            double u = 1.0 - rng.NextDouble();
            double dt = -Math.Log(u) / _lambda;
            t += dt;
            if (t >= 1.0) break;
            int step = (int)Math.Round(t * (ctx.StepsTotal - 1));
            p.Hits[step] = true;
            p.Lengths[step] = 1;
            p.Velocity[step] = ctx.DefaultVelocity;
        }
        return p;
    }
}