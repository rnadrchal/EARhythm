using Egami.Chemistry.Graph;
using Egami.Sequencer;
using Melanchall.DryWetMidi.Common;

namespace Egami.Chemistry.Sequencer;

public sealed class AtomSequenceStep : SequenceStep, IGridAutomatedStep
{
    public bool IgnorePitch { get; }
    public bool IgnorePitchbend { get; }
    public bool IgnoreCc { get; }

    public GridPitchbendRamp? PitchbendRamp { get; }
    public IReadOnlyList<GridCcRamp>? CcRamps { get; }

    public AtomSequenceStep(
        int stepIndex,
        int lengthInSteps,
        SevenBitNumber pitch,
        SevenBitNumber velocity,
        int pitchBend,
        IDictionary<int, SevenBitNumber>? ccValues,
        bool ignorePitch,
        bool ignorePitchbend,
        bool ignoreCc,
        GridPitchbendRamp? pitchbendRamp,
        IReadOnlyList<GridCcRamp>? ccRamps)
        : base(stepIndex, lengthInSteps, pitch, velocity, pitchBend, ccValues)
    {
        IgnorePitch = ignorePitch;
        IgnorePitchbend = ignorePitchbend;
        IgnoreCc = ignoreCc;
        PitchbendRamp = pitchbendRamp;
        CcRamps = ccRamps;
    }

    public static AtomSequenceStep FromPacket(int stepIndex, int lengthInSteps, AtomPacket p)
        => new(
            stepIndex,
            lengthInSteps,
            (SevenBitNumber)p.Pitch,
            (SevenBitNumber)p.Velocity,
            p.PitchBend,
            p.Ccs.ToDictionary(kv => kv.Key, kv => (SevenBitNumber)kv.Value),
            p.IgnorePitch,
            p.IgnorePitchbend,
            p.IgnoreCc,
            p.PitchbendRamp,
            p.CcRamps);
}