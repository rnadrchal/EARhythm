namespace Egami.Pitch;

public interface IPitchGenerator
{
    byte?[] Generate(byte basePitch, int length);
}