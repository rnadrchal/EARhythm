using Egami.Sequencer.Grid;

namespace Egami.Chemistry.Graph;

public sealed class BondDurationMapper : IBondDurationMapper
{
    public GridDivision BaseDivision { get; init; } = GridDivision.Sixteenth;
    public bool Compressed { get; init; } = true;
    public bool Quantize { get; init; } = true;

    public double ReferenceBondLength { get; init; } = 1.0; // z.B. C-C ~ 1.54Å -> frei wählbar
    public int BaseSteps { get; init; } = 1;                // 1 Step in BaseDivision

    public double CompressionAlpha { get; init; } = 0.5;    // 0.3..0.7 typ.

    public int GetDurationInSteps(double bondLength3D)
    {
        if (bondLength3D <= 0) return 1;

        var ratio = bondLength3D / ReferenceBondLength;
        var scaled = Compressed ? Math.Pow(ratio, CompressionAlpha) : ratio;

        var rawSteps = (int)Math.Round(BaseSteps * scaled);
        if (rawSteps < 1) rawSteps = 1;

        return Quantize ? QuantizeSteps(rawSteps) : rawSteps;
    }

    private static int QuantizeSteps(int steps)
    {
        // Minimal-invasiv: auf 1/2/3/4/6/8/12/16/... runden (musikalisch stabil)
        // (Hier nur Beispiel; du kannst es an GridDivision koppeln.)
        int[] allowed = { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64 };
        int best = allowed[0];
        int bestDist = Math.Abs(steps - best);
        foreach (var a in allowed)
        {
            var d = Math.Abs(steps - a);
            if (d < bestDist) { best = a; bestDist = d; }
        }
        return best;
    }
}