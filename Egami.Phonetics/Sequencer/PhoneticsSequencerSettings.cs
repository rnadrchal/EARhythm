using Egami.Sequencer.Common;
using Melanchall.DryWetMidi.Common;

namespace Egami.Phonetics.Sequencer;

public sealed class PhoneticSequencerSettings
{
    public SevenBitNumber RootNote { get; init; } = (SevenBitNumber)62; // D4
    public ScaleType Scale { get; init; } = ScaleType.Chromatic;

    public int BaseStepsPerUnit { get; init; } = 2;
    public int BaseVelocity { get; init; } = 90;

    public double StressDurationFactor { get; init; } = 1.3;
    public double StressLoudnessFactor { get; init; } = 1.15;

    // CC-Mapping für „Klangfarbe“ / Filter
    public int FilterCcNumber { get; init; } = 74;    // meist Brightness/Filter
    public SevenBitNumber FilterFricativeValue { get; init; } = (SevenBitNumber)100;
    public SevenBitNumber FilterNeutralValue { get; init; } = (SevenBitNumber)64;

    // Pitchbend-Range (in Halbtönen) für Glides/Nasale
    public int PitchBendRangeSemitones { get; init; } = 2;

    // Triller-Intervall für r/l (in Halbtönen)
    public int TrillIntervalSemitones { get; init; } = 1;

    // Anzahl der Trillerschläge (Neu)
    public int TrillCount { get; init; } = 1;

    // Nasal-Microglide-Intervall (Halbtöne, typ. 1)
    public int NasalGlideSemitones { get; init; } = 1;

    // Dauerfaktoren für Ornamente (als Faktor der Vokal-Dauer)
    public double PlosiveAccentFraction { get; init; } = 0.25;
    public double TrillFraction { get; init; } = 0.3;
    public double NasalTailFraction { get; init; } = 0.3;
}