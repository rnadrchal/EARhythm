using System;

namespace EnvironmentalSequencer.Models;

public sealed class LuxMapping(
    string name,
    double value,
    string unit = "Lx",
    double maxValue = 2000,
    double minValue = 1)
    : ValueMapping(name, value, unit, maxValue, minValue)
{


    public override byte ToByte()
    {
        // MIDI 7-bit range 0..127
        const int midiMax = 127;
        var norm = GetNormalizedLogScale();
        var mapped = (int)Math.Round(norm * midiMax);
        return (byte)Math.Clamp(mapped, 0, midiMax);
    }

    public override ushort ToUshort()
    {
        // 14-bit MIDI (0..16383)
        const int midi14Max = 16383;
        var norm = GetNormalizedLogScale();
        var mapped = (int)Math.Round(norm * midi14Max);
        return (ushort)Math.Clamp(mapped, 0, midi14Max);
    }

    private double GetNormalizedLogScale()
    {
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0.0;

        // copy configured bounds
        var min = MinValue;
        var max = MaxValue;

        // guard: sensible defaults if bounds are invalid
        if (max <= min)
        {
            // fallback: treat max as value if not set -> return either 0 or 1 depending on v
            return v <= min ? 0.0 : 1.0;
        }

        // For log scale we need strictly positive domain. Use small epsilon if min <= 0.
        const double eps = 1e-3; // 0.001 lux as practical minimum for log mapping
        var logMinSource = Math.Max(min, eps);
        var logVsource = Math.Max(v, eps);
        var logMaxSource = Math.Max(max, eps);

        try
        {
            // If range is wide enough use log scale
            var denom = Math.Log(logMaxSource) - Math.Log(logMinSource);
            if (Math.Abs(denom) > double.Epsilon)
            {
                var norm = (Math.Log(logVsource) - Math.Log(logMinSource)) / denom;
                return Math.Clamp(norm, 0.0, 1.0);
            }
        }
        catch
        {
            // fall through to linear fallback
        }

        // Linear fallback (safe when log is not applicable)
        var linearNorm = (v - min) / (max - min);
        return Math.Clamp(linearNorm, 0.0, 1.0);
    }
}