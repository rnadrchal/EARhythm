using System;

namespace EnvironmentalSequencer.Models;

public sealed class RmsMapping : ValueMapping
{
    // Sensible defaults for microphone RMS/peak values
    // Typical normalized audio amplitude range is0.0 ..1.0
    private const double DefaultMin =0.0;
    private const double DefaultMax =1.0;

    public RmsMapping(string name, double value, string unit, double maxValue = 1.0, double minValue = 0 ) : base(name, value, unit, maxValue, minValue)
    {
        // No mutation of base properties here (setter may be inaccessible).    
        // Rely on provided values; ToByte/ToUshort use safe fallbacks.
    }

    /// <summary>
    /// Map current Value to MIDI7-bit (0..127).
    /// Linear mapping from [MinValue..MaxValue] -> [0..127], clamped.
    /// Uses safe fallbacks if Min/Max are invalid.
    /// </summary>
    public override byte ToByte()
    {
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0;

        double min = MinValue;
        double max = MaxValue;

        if (!(double.IsFinite(min) && double.IsFinite(max) && max > min))
        {
            min = DefaultMin;
            max = DefaultMax;
        }

        double norm = (v - min) / (max - min);
        norm = Math.Clamp(norm,0.0,1.0);

        int midi = (int)Math.Round(norm *127.0);
        midi = Math.Clamp(midi,0,127);
        return (byte)midi;
    }

    /// <summary>
    /// Map current Value to MIDI Pitchbend14-bit (0..16383).
    /// Linear mapping from [MinValue..MaxValue] -> [0..16383], clamped.
    /// Uses safe fallbacks if Min/Max are invalid.
    /// </summary>
    public override ushort ToUshort()
    {
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v)) return 0;

        double min = MinValue;
        double max = MaxValue;

        if (!(double.IsFinite(min) && double.IsFinite(max) && max > min))
        {
            min = DefaultMin;
            max = DefaultMax;
        }

        double norm = (v - min) / (max - min);
        norm = Math.Clamp(norm,0.0,1.0);

        int pb = (int)Math.Round(norm *16383.0);
        pb = Math.Clamp(pb,0,16383);
        return (ushort)pb;
    }
}