using System;

namespace EnvironmentalSequencer.Models;

public class AltitudeMapping(
    string name,
    double value,
    string unit = "m",
    double maxValue = 9000,
    double minValue = -500)
    : ValueMapping(name, value, unit, maxValue, minValue)
{
    // Default: -500 m (z. B. Tote Meer) bis 9000 m (über dem Mount Everest)

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
            return (byte)(v <= min ? 0 : midiMax);

        var norm = (v - min) / (max - min);
        norm = Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)Math.Round(norm * midiMax);
        return (byte)Math.Clamp(mapped, 0, midiMax);
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
            return (ushort)(v <= min ? 0 : midi14Max);

        var norm = (v - min) / (max - min);
        norm = Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)Math.Round(norm * midi14Max);
        return (ushort)Math.Clamp(mapped, 0, midi14Max);
    }
}