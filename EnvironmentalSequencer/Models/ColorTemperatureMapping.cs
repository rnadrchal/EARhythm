namespace EnvironmentalSequencer.Models;

public class ColorTemperatureMapping(
    string name,
    double value,
    string unit = "K",
    double maxValue = 6500,
    double minValue = 2000)
    : ValueMapping(name, value, unit, maxValue, minValue)
{
    public override byte ToByte()
    {
        const int midiMax = 127;
        var norm = GetNormalizedMiredLogScale();
        var mapped = (int)System.Math.Round(norm * midiMax);
        return (byte)System.Math.Clamp(mapped, 0, midiMax);
    }

    public override ushort ToUshort()
    {
        const int midi14Max = 16383;
        var norm = GetNormalizedMiredLogScale();
        var mapped = (int)System.Math.Round(norm * midi14Max);
        return (ushort)System.Math.Clamp(mapped, 0, midi14Max);
    }

    // Convert Kelvin -> Mired and normalize on log-scale (with linear fallback).
    private double GetNormalizedMiredLogScale()
    {
        var kelvin = Value;
        if (double.IsNaN(kelvin) || double.IsInfinity(kelvin) || kelvin <= 0)
            return 0.0;

        // Min/Max are expected in same unit as Value (Kelvin).
        var minK = MinValue;
        var maxK = MaxValue;

        // If bounds invalid, fallback to simple 0/1 decision
        if (maxK <= minK || minK <= 0 || maxK <= 0)
        {
            // Treat single-value cases: if value <= min => 0, if >= max => 1 or use proportion
            if (maxK <= minK)
                return kelvin <= minK ? 0.0 : 1.0;
        }

        // Convert to Mired (micro reciprocal degrees): mired = 1_000_000 / K
        double ToMired(double k) => 1_000_000.0 / k;

        var mired = ToMired(kelvin);
        var minMired = ToMired(maxK); // note: converting bounds: higher K -> lower mired
        var maxMired = ToMired(minK);

        // Ensure positive domain for log
        const double eps = 1e-6;
        minMired = System.Math.Max(minMired, eps);
        maxMired = System.Math.Max(maxMired, eps);
        mired = System.Math.Max(mired, eps);

        // Try log normalization on mired axis
        try
        {
            var denom = System.Math.Log(maxMired) - System.Math.Log(minMired);
            if (System.Math.Abs(denom) > double.Epsilon)
            {
                var norm = (System.Math.Log(mired) - System.Math.Log(minMired)) / denom;
                return System.Math.Clamp(norm, 0.0, 1.0);
            }
        }
        catch
        {
            // fall through to linear fallback
        }

        // Linear fallback on mired
        var linear = (mired - minMired) / (maxMired - minMired);
        return System.Math.Clamp(linear, 0.0, 1.0);
    }
}