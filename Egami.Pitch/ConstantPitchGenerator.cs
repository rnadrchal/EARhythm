namespace Egami.Pitch;

public class ConstantPitchGenerator : IPitchGenerator
{
    public byte?[] Generate(byte basePitch, int length)
    {
        if (length <= 0)
            return Array.Empty<byte?>();

        var result = new byte?[length];
        for (int i = 0; i < length; i++)
            result[i] = (byte)Math.Clamp((int)basePitch, 0, 127);

        return result;
    }
}