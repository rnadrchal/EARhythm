using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Transformation;

public sealed class SwingVelocityTransform(double strong = 1.2, double weak = 0.8) : IRhythmTransform
{
    private readonly double _strong = strong;
    private readonly double _weak = weak;

    public Sequence Apply(RhythmContext ctx, Sequence input)
    {
        var outp = input.Clone();
        for (int i = 0; i < outp.StepsTotal; i++)
        {
            if (!outp.Hits[i]) continue;
            var factor = (i % 2 == 0) ? _strong : _weak;
            var v = outp.Steps[i].Velocity == 0 ? 100 : outp.Steps[i].Velocity;
            outp.Steps[i].Velocity = (byte)Math.Clamp((int)Math.Round(v * factor), 1, 127);
        }
        return outp;
    }
}