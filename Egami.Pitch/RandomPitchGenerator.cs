using Egami.Rhythm.Common;

namespace Egami.Pitch;

public class RandomPitchGenerator : IPitchGenerator
{
    public int Octaves { get; set; } = 1;

    public int? Seed { get; set; } = null;
    public byte?[] Generate(byte basePitch, int length)
    {
        var result = new byte?[length];
        var random = RandomProvider.Get(Seed);
        for (int i = 0; i < length; i++)
        {
            result[i] = (byte)random.Next(basePitch, Math.Min(127, basePitch + Octaves * 12));
        }

        return result;
    }
}