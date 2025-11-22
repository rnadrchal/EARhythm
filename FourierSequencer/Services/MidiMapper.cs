using System;

namespace FourierSequencer.Services;

public static class MidiMapper
{
    /// <summary>
    /// Map normalized amplitude (expected roughly in [-1,1]) to MIDI range.
    /// amplitudeLimit clamps the amplitude before mapping (0..1). If amplitudeLimit<=0 then treat as1.
    /// </summary>
    public static int MapToMidi(double amplitude, double amplitudeLimit, int minMidi =0, int maxMidi =127)
    {
        if (double.IsNaN(amplitude) || double.IsInfinity(amplitude)) return minMidi;
        double limit = amplitudeLimit <=0 || double.IsNaN(amplitudeLimit) ?1.0 : Math.Abs(amplitudeLimit);
        // clamp amplitude to [-limit, limit]
        double a = amplitude;
        if (a > limit) a = limit;
        if (a < -limit) a = -limit;

        // normalize to0..1
        double norm = (a + limit) / (2.0 * limit);
        norm = Math.Min(1.0, Math.Max(0.0, norm));

        double value = minMidi + norm * (maxMidi - minMidi);
        return (int)Math.Round(value);
    }
}