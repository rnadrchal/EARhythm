using System;

namespace EnvironmentalSequencer.Models;

public sealed class GasResistanceMapping(
    string name,
    double value,
    string unit = "Ω",
    double maxValue = 10000000,
    double minValue = 10000)
    : ValueMapping(name, value, unit, maxValue, minValue)
{

    public double ScaledValue => Value / 1000.0;

    public override double Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                RaisePropertyChanged(nameof(ScaledValue));
            }
        }
    }

    public override byte ToByte()
    {
        const int midiMax = 127;
        var norm = GetNormalizedLogScale();
        var mapped = (int)Math.Round(norm * midiMax);
        return (byte)Math.Clamp(mapped, 0, midiMax);
    }

    public override ushort ToUshort()
    {
        const int midi14Max = 16383;
        var norm = GetNormalizedLogScale();
        var mapped = (int)Math.Round(norm * midi14Max);
        return (ushort)Math.Clamp(mapped, 0, midi14Max);
    }

    private double GetNormalizedLogScale()
    {
        var v = Value;
        if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0)
            return 0.0;

        var min = MinValue;
        var max = MaxValue;
        if (max <= min)
            return v <= min ? 0.0 : 1.0;

        const double eps = 1.0; // 1 Ohm als untere Grenze für Log
        var logMin = Math.Max(min, eps);
        var logV = Math.Max(v, eps);
        var logMax = Math.Max(max, eps);

        var denom = Math.Log(logMax) - Math.Log(logMin);
        if (Math.Abs(denom) > double.Epsilon)
        {
            var norm = (Math.Log(logV) - Math.Log(logMin)) / denom;
            return Math.Clamp(norm, 0.0, 1.0);
        }

        // fallback linear
        var linear = (v - min) / (max - min);
        return Math.Clamp(linear, 0.0, 1.0);
    }
}