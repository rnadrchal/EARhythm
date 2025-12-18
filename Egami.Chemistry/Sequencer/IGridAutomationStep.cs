using Melanchall.DryWetMidi.Common;

namespace Egami.Chemistry.Sequencer;

public interface IGridAutomatedStep
{
    bool IgnorePitch { get; }
    bool IgnorePitchbend { get; }
    bool IgnoreCc { get; }

    GridPitchbendRamp? PitchbendRamp { get; }
    IReadOnlyList<GridCcRamp>? CcRamps { get; }
}

public sealed record GridPitchbendRamp(
    int StartValue,      // -8192..+8191
    int EndValue,        // -8192..+8191
    int DurationPulses); // in MIDI Clock Pulses (24 PPQN / pulsesPerStep)

public sealed record GridCcRamp(
    int CcNumber,              // 0..127
    SevenBitNumber StartValue, // 0..127
    SevenBitNumber EndValue,   // 0..127
    int DurationPulses);       // in pulses