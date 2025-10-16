using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Transformation;

public sealed class RotateTransform(int offset) : IRhythmTransform
{
    private readonly int _offset = offset;

    public RhythmPattern Apply(RhythmContext ctx, RhythmPattern input)
    {
        var n = input.StepsTotal;
        var outp = input.Clone();
        void Rot<T>(T[] a)
        {
            var b = new T[n];
            for (int i = 0; i < n; i++) b[(i + _offset % n + n) % n] = a[i];
            Array.Copy(b, a, n);
        }
        Rot(outp.Hits);
        Rot(outp.Velocities);
        Rot(outp.Lengths);
        return outp;
    }
}