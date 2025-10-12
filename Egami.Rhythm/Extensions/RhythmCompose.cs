using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using Egami.Rhythm.Transformation;

namespace Egami.Rhythm.Extensions;

public static class RhythmCompose
{
    // Pipeline: Generator -> Transform -> Transform -> ...
    public static RhythmPattern GenerateWith(this IRhythmGenerator gen, RhythmContext ctx, params IRhythmTransform[] transforms)
    {
        var pat = gen.Generate(ctx);
        foreach (var t in transforms)
            pat = t.Apply(ctx, pat);
        return pat;
    }

    // Layering: mehrere Patterns auf gemeinsames Raster mergen
    public static RhythmPattern Merge(params RhythmPattern[] parts)
    {
        if (parts.Length == 0) throw new ArgumentException("No parts.");
        int n = parts[0].StepsTotal;
        var outp = new RhythmPattern(n);
        foreach (var p in parts)
        {
            if (p.StepsTotal != n) throw new InvalidOperationException("All parts must share the same length.");
            for (int i = 0; i < n; i++)
            {
                if (p.Hits[i])
                {
                    outp.Hits[i] = true;
                    outp.Velocities[i] = (byte)Math.Clamp(outp.Velocities[i] + Math.Max((int)p.Velocities[i], 1), 1, 127);
                    outp.Lengths[i] = Math.Max(outp.Lengths[i], p.Lengths[i]);
                }
            }
        }
        return outp;
    }
}