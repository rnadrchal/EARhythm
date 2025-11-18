namespace EnvironmentalSequencer.Models;

public sealed class PercentMapping(
    string name,
    double value,
    string unit = "%",
    double maxValue = 100,
    double minValue = 0)
    : ValueMapping(name, value, unit, maxValue, minValue)
{
    public override byte ToByte()
    {
        return (byte)(Value * 127 / 100);
    }

    public override ushort ToUshort()
    {
        return (ushort)(Value / 100 * 16383);
    }
}