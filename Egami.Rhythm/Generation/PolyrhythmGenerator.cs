using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

public sealed class PolyrhythmGenerator(int a, int b, byte velA = 110, byte velB = 90, int lengthA = 1, int lengthB = 1) : IRhythmGenerator
{
    private readonly int _a = a;
    private readonly int _b = b;
    private readonly byte _va = Math.Clamp(velA, (byte)0, (byte)127);
    private readonly byte _vb = Math.Clamp(velA, (byte)0, (byte)127);
    private readonly int _la = lengthA;
    private readonly int _lb = lengthB;

    public Sequence Generate(RhythmContext ctx)
    {
        int lcm = Lcm(_a, _b);
        int n = ctx.StepsTotal > 0 ? ctx.StepsTotal : lcm;
        var p = new Sequence(n);
        p.Steps.ForEach(s => s.Length = 1);
        for (int i = 0; i < n; i++)
        {
            bool hitA = i % (n / _a) == 0;
            bool hitB = i % (n / _b) == 0;
            if (hitA || hitB)
            {
                p.Steps[i].Hit = true;
                p.Steps[i].Length = hitA && hitB ? Math.Max(_la, _lb) : (hitA ? _la : hitB ? _lb : 1);
                p.Steps[i].Velocity = (byte)Math.Clamp((hitA ? _va : 0) + (hitB ? _vb : 0), 1, 127);
            }
        }
        return p;

        static int Gcd(int x, int y) => y == 0 ? x : Gcd(y, x % y);
        static int Lcm(int x, int y) => x / Gcd(x, y) * y;
    }
}