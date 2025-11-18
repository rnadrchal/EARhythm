using System;

namespace EnvironmentalSequencer.Models;

public class TemperatureMapping(
    string name,
    double value,
    string unit = "°C",
    double maxValue = 40,
    double minValue = -20)
    : ValueMapping(name, value, unit, maxValue, minValue)
{
    public override byte ToByte()
    {
        // Map temperature linearly to MIDI 7-bit (0..127)
        const int midiMax = 127;
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0;

        var min = MinValue;
        var max = MaxValue;

        if (max <= min)
        {
            // fallback: treat as binary threshold
            return (byte)(v <= min ? 0 : midiMax);
        }

        var norm = (v - min) / (max - min);
        norm = Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)Math.Round(norm * midiMax);
        return (byte)Math.Clamp(mapped, 0, midiMax);
    }

    public override ushort ToUshort()
    {
        // Map temperature linearly to 14-bit MIDI (0..16383)
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
        norm = Math.Clamp(norm, 0.0, 1.0);
        var mapped = (int)Math.Round(norm * midi14Max);
        return (ushort)Math.Clamp(mapped, 0, midi14Max);
    }
}