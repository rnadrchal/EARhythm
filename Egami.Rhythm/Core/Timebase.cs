namespace Egami.Rhythm.Core;

public readonly record struct Timebase(int StepsPerQuarter)
{
    public int Divider => StepsPerQuarter * 4;
    public int Ticks => 96 / Divider;
}