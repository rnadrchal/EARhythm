using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using Egami.Rhythm.Transformation;

namespace Egami.Rhythm.Extensions;

public static class RhythmCompose
{
    // Pipeline: Generator -> Transform -> Transform -> ...
    public static Sequence GenerateWith(this IRhythmGenerator gen, RhythmContext ctx, params IRhythmTransform[] transforms)
    {
        var pat = gen.Generate(ctx);
        foreach (var t in transforms)
            pat = t.Apply(ctx, pat);
        return pat;
    }

    // Layering: mehrere Patterns auf gemeinsames Raster mergen
    public static Sequence Merge(params Sequence[] parts)
    {
        if (parts.Length == 0) throw new ArgumentException("No parts.");
        int n = parts[0].StepsTotal;
        var outp = new Sequence(n);
        foreach (var p in parts)
        {
            if (p.StepsTotal != n) throw new InvalidOperationException("All parts must share the same length.");
            for (int i = 0; i < n; i++)
            {
                if (p.Hits[i])
                {
                    outp.Hits[i] = true;
                    outp.Steps[i].Velocity = (byte)Math.Clamp(outp.Steps[i].Velocity + Math.Max((int)p.Steps[i].Velocity, 1), 1, 127);
                    outp.Steps[i].Velocity = Math.Max(outp.Steps[i].Velocity, p.Steps[i].Velocity);
                }
            }
        }
        return outp;
    }
}