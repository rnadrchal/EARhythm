using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public sealed class EuclidGenerator(int pulses, int rotate = 0) : IRhythmGenerator
{
    private readonly int _pulses = pulses;
    private readonly int _rotate = rotate;

    public RhythmPattern Generate(RhythmContext ctx)
    {
        int n = ctx.StepsTotal;
        int k = Math.Clamp(_pulses, 0, n);

        var p = new RhythmPattern(n);

        if (k == 0) return p; // alles Off
        if (k == n)
        {
            for (int i = 0; i < n; i++) { p.Hits[i] = true; p.Velocity[i] = ctx.DefaultVelocity; }
            return ApplyRotate(p, _rotate);
        }

        // Euclid-Verteilung über Floor-Differenzen:
        // Hit an Position i, wenn floor((i+1)*k/n) != floor(i*k/n)
        for (int i = 0; i < n; i++)
        {
            int a = (i * k) / n;
            int b = ((i + 1) * k) / n;
            bool hit = a != b;
            p.Hits[i] = hit;
            p.Velocity[i] = hit ? ctx.DefaultVelocity : (byte)0;
            p.Lengths[i] = hit ? 1 : 0;
        }

        return ApplyRotate(p, _rotate);
    }

    private static RhythmPattern ApplyRotate(RhythmPattern input, int offset)
    {
        if (offset == 0) return input;
        int n = input.StepsTotal;
        var outp = input.Clone();
        void Rot<T>(T[] arr)
        {
            var tmp = new T[n];
            int off = ((offset % n) + n) % n;
            for (int i = 0; i < n; i++) tmp[(i + off) % n] = arr[i];
            Array.Copy(tmp, arr, n);
        }
        Rot(outp.Hits);
        Rot(outp.Velocity);
        Rot(outp.Lengths);
        return outp;
    }
}