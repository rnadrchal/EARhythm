namespace EnvironmentalSequencer.Models;

public sealed class PressureMapping(
    string name,
    double value,
    string unit = "hPa",
    double maxValue = 1030,
    double minValue = 950)
    : ValueMapping(name, value, unit, maxValue, minValue)
{
    public override byte ToByte()
    {
        // MIDI 7-bit range 0..127
        const int midiMax = 127;
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0;

        var min = MinValue;
        var max = MaxValue;

        if (max <= min)
        {
            // Fallback: binär entscheiden
            return (byte)(v <= min ? 0 : midiMax);
        }

        var norm = (v - min) / (max - min);
        norm = System.Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)System.Math.Round(norm * midiMax);
        return (byte)System.Math.Clamp(mapped, 0, midiMax);
    }

    public override ushort ToUshort()
    {
        // 14-bit MIDI range 0..16383
        const int midi14Max = 16383;
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0;

        var min = MinValue;
        var max = MaxValue;

        if (max <= min)
        {
            return (ushort)(v <= min ? 0 : midi14Max);
        }

        var norm = (v - min) / (max - min);
        norm = System.Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)System.Math.Round(norm * midi14Max);
        return (ushort)System.Math.Clamp(mapped, 0, midi14Max);
    }
}