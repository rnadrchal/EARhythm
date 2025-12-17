using System.ComponentModel.DataAnnotations;
using Melanchall.DryWetMidi.Common;

namespace Egami.Sequencer.Extensions;

public static class SequenceTransformations
{
    public static MusicalSequence Transpose(this MusicalSequence source, int semitones)
    {
        if (source.LengthInSteps == 0) return MusicalSequence.Empty;

        var transformed = source.Steps.Select(s =>
        {
            var newPitch = ClampToSevenBit(s.Pitch + semitones);
            return s.With(pitch: newPitch);
        });

        return new MusicalSequence(transformed);
    }

    /// <summary>
    /// Inversion um eine Achse (z. B. Achse = 60 = C4).
    /// newPitch = 2*axis - oldPitch.    /// </summary>
    /// <param name="source"></param>
    /// <param name="axisPitch"></param>
    /// <returns></returns>
    public static MusicalSequence Invert(this MusicalSequence source, int axisPitch)
    {
        if (source.LengthInSteps == 0) return MusicalSequence.Empty;

        var transformed = source.Steps.Select(s =>
        {
            var inverted = 2 * axisPitch - s.Pitch;
            var newPitch = ClampToSevenBit(inverted);
            return s.With(pitch: newPitch);
        });

        return new MusicalSequence(transformed);
    }

    /// <summary>
    /// Retrograde (Umkehrung in der Zeitachse).
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static MusicalSequence Retrograde(this MusicalSequence source)
    {
        if (source.LengthInSteps == 0) return MusicalSequence.Empty;

        var maxEnd = source.LengthInSteps;

        var transformed = source.Steps.Select(s =>
        {
            // Beispiel: maxEnd = 16, Step von 4..6 (Länge 2)
            // -> neuer Start = 16 - (4 + 2) = 10, endet bei 12
            var newIndex = maxEnd - (s.StepIndex + s.LengthInSteps);
            return s.With(stepIndex: newIndex);
        });

        return new MusicalSequence(transformed);
    }

    public static SevenBitNumber ClampToSevenBit(int value)
    {
        if (value < 0) value = 0;
        if (value > 127) value = 127;
        return (SevenBitNumber)value;
    }
}