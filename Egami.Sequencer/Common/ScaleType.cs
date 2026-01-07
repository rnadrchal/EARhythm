using System.ComponentModel.DataAnnotations;

namespace Egami.Sequencer.Common;

public enum ScaleType
{
    [Display(Name = "Chromatic")]
    Chromatic,

    [Display(Name = "Major")]
    Major,

    [Display(Name = "Major 5")]
    MajorPentatonic,

    [Display(Name = "Natural Minor")]
    NaturalMinor,

    [Display(Name = "Harmonic Minor")]
    HarmonicMinor,

    [Display(Name = "Melodic Minor")]
    MelodicMinor,

    [Display(Name = "Minor 5")]
    MinorPentatonic,

    [Display(Name = "Ionian")]
    Ionian,

    [Display(Name = "Dorian")]
    Dorian,

    [Display(Name = "Phrygian")]
    Phrygian,

    [Display(Name = "Lydian")]
    Lydian,

    [Display(Name = "Mixolydian")]
    Mixolydian,

    [Display(Name = "Locrian")]
    Locrian,

    [Display(Name = "Whole Tone")]
    WholeTone,

    [Display(Name = "Blues")]
    Blues,

    [Display(Name = "Bebop Maj")]
    BebopMajor,

    [Display(Name = "Bebop Dom")]
    BebopDominant,

    [Display(Name = "Phrygian Dom")]
    PhrygianDominant,

    [Display(Name = "Spanish Gypsy")]
    SpanishGypsy,

    [Display(Name = "Octatonic")]
    Octatonic,

    [Display(Name = "Byzantine")]
    DoubleHarmonic,

    [Display(Name = "Hungarian Min")]
    HungarianMinor,

    [Display(Name = "Neapolitan Min")]
    NeapolitanMinor,

    [Display(Name = "Neapolitan Maj")]
    NeapolitanMajor,

    [Display(Name = "Enigmatic")]
    Enigmatic,

    [Display(Name = "Augmented")]
    Augmented,

    [Display(Name = "Persian")]
    Persian,

    [Display(Name = "Prometheus")]
    Prometheus,

    [Display(Name = "Egyptian")]
    Egyptian,

    [Display(Name = "Lydian Aug")]
    LydianAugmented,

    [Display(Name = "Pelog")]
    Pelog,

    [Display(Name = "Hirajoshi")]
    Hirajoshi
}

public static class ScaleTypeExtensions
{
    /// <summary>
    /// Gibt die Skalenstufen als Semiton-Abstände (ints, 0 = Tonika) innerhalb einer Oktave zurück.
    /// Beispiel: Major => {0,2,4,5,7,9,11}
    /// </summary>
    public static int[] GetDegrees(this ScaleType scaleType) =>
        scaleType switch
        {
            ScaleType.Major => new[] { 0, 2, 4, 5, 7, 9, 11 },
            ScaleType.Ionian => new[] { 0, 2, 4, 5, 7, 9, 11 },
            ScaleType.NaturalMinor => new[] { 0, 2, 3, 5, 7, 8, 10 },
            ScaleType.HarmonicMinor => new[] { 0, 2, 3, 5, 7, 8, 11 },
            ScaleType.MelodicMinor => new[] { 0, 2, 3, 5, 7, 9, 11 },
            ScaleType.Dorian => new[] { 0, 2, 3, 5, 7, 9, 10 },
            ScaleType.Phrygian => new[] { 0, 1, 3, 5, 7, 8, 10 },
            ScaleType.Lydian => new[] { 0, 2, 4, 6, 7, 9, 11 },
            ScaleType.Mixolydian => new[] { 0, 2, 4, 5, 7, 9, 10 },
            ScaleType.Locrian => new[] { 0, 1, 3, 5, 6, 8, 10 },
            ScaleType.MajorPentatonic => new[] { 0, 2, 4, 7, 9 },
            ScaleType.MinorPentatonic => new[] { 0, 3, 5, 7, 10 },
            ScaleType.Blues => new[] { 0, 3, 5, 6, 7, 10 },
            ScaleType.WholeTone => new[] { 0, 2, 4, 6, 8, 10 },
            ScaleType.Chromatic => new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
            ScaleType.Octatonic => new[] { 0, 2, 3, 5, 6, 8, 9, 11 }, // diminished / alternierende Ganz-/Halbton
            ScaleType.DoubleHarmonic => new[] { 0, 1, 4, 5, 7, 8, 11 }, // Byzantine
            ScaleType.HungarianMinor => new[] { 0, 2, 3, 6, 7, 8, 11 },
            ScaleType.NeapolitanMinor => new[] { 0, 1, 3, 5, 7, 8, 11 },
            ScaleType.NeapolitanMajor => new[] { 0, 1, 3, 5, 7, 9, 11 },
            ScaleType.Enigmatic => new[] { 0, 1, 4, 6, 8, 10, 11 },
            ScaleType.Augmented => new[] { 0, 3, 4, 7, 8, 11 },
            ScaleType.BebopMajor => new[] { 0, 2, 4, 5, 7, 9, 10, 11 }, // Major + chromatic passing tone
            ScaleType.BebopDominant => new[] { 0, 2, 4, 5, 7, 9, 10, 11 }, // Mixolydian + passing tone
            ScaleType.PhrygianDominant => new[] { 0, 1, 4, 5, 7, 8, 10 },
            ScaleType.SpanishGypsy => new[] { 0, 1, 4, 5, 7, 8, 10 }, // Variante des phrygisch-dominanten
            ScaleType.Persian => new[] { 0, 1, 4, 5, 6, 8, 11 },
            ScaleType.Prometheus => new[] { 0, 2, 4, 6, 9, 10 },
            ScaleType.Egyptian => new[] { 0, 2, 5, 7, 10 },
            ScaleType.LydianAugmented => new[] { 0, 2, 4, 6, 8, 9, 11 },
            ScaleType.Pelog => new[] { 0, 1, 3, 7, 8 },
            ScaleType.Hirajoshi => new[] { 0, 2, 3, 7, 8 },
            _ => throw new ArgumentOutOfRangeException(nameof(scaleType), scaleType, "Unbekannter ScaleType")
        };
}