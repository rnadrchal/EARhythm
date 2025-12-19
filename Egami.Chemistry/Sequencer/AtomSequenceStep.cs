using System.Security.Cryptography.X509Certificates;
using Egami.Chemistry.Graph;
using Egami.Chemistry.Model;
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

    // --- Neu: optionale Referenzen / Indizes zur Korrelation mit Graph-Modellen ---
    /// <summary>
    /// Laufzeit-Referenz auf den verknüpften AtomNode (nicht zwingend serialisierbar).
    /// Kann gesetzt werden, wenn ein Sequenzschritt an ein konkretes Atom im StructureGraph gebunden ist.
    /// </summary>
    public IEnumerable<AtomNode> Atoms { get; set; }

    /// <summary>
    /// Laufzeit-Referenz auf den verknüpften BondEdge (nicht zwingend serialisierbar).
    /// Kann gesetzt werden, wenn ein Sequenzschritt an eine konkrete Bindung gebunden ist.
    /// </summary>
    public BondEdge? Bond { get; set; }

    /// <summary>
    /// Optionaler Atom-Index im zugehörigen StructureGraph (hilft bei Serialisierung/Mapping).
    /// </summary>
    public int? AtomIndex { get; set; }

    /// <summary>
    /// Optionaler Bond-Index im zugehörigen StructureGraph (hilft bei Serialisierung/Mapping).
    /// </summary>
    public int? BondIndex { get; set; }
    // -------------------------------------------------------------------------

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