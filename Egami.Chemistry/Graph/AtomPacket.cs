using Egami.Chemistry.Sequencer;

namespace Egami.Chemistry.Graph;

public readonly record struct AtomId(int Value);

public sealed record AtomPacket(
    int Pitch,                // 0..127
    int Velocity,             // 0..127
    int PitchBend,            // -8192..+8191 (DryWetMIDI-PB-Event expects 0..16383)
    IReadOnlyDictionary<int, int> Ccs,
    bool IgnorePitch = false,
    bool IgnorePitchbend = false,
    bool IgnoreCc = false,
    GridPitchbendRamp? PitchbendRamp = null,
    IReadOnlyList<GridCcRamp>? CcRamps = null);

public sealed record Bond(
    AtomId A,
    AtomId B,
    double Length3D);

public sealed record RampSpec(
    int StartValue,
    int EndValue,
    int DurationPulses); // in MIDI clock pulses, nicht Steps! -> nutzt MidiClockGrid.Pulse

public sealed record CcRampSpec(
    int CcNumber,
    int StartValue,
    int EndValue,
    int DurationPulses);