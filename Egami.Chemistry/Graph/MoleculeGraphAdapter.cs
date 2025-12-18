using System.Collections.Concurrent;
using Egami.Chemistry.Model;
using Egami.Chemistry.Spectrum;
using Egami.Sequencer.Common;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Tools;
using NCDK;
using NCDK.IO.Formats;

namespace Egami.Chemistry.Graph;

public sealed record PacketSettings(
    bool IgnorePitch = false,
    bool IgnorePitchbend = false,
    bool IgnoreCCs = false
)
{
    public bool IgnorePitch { get; set; }
    public bool IgnorePitchbend { get; set; }
    public bool IgnoreCCs { get; set; }
}

public sealed class MoleculeGraphAdapter : IMoleculeGraph
{
    private readonly StructureGraph _structure;
    private readonly WavelengthToMidiPitchMapper _wavelengthMapper;

    // Adjazenzliste: AtomId.Value -> sortierte Neighbor-AtomIds
    private readonly Dictionary<int, List<AtomId>> _neighbors;

    // Bond-Lookup: kanonischer EdgeKey -> Bond
    private readonly Dictionary<EdgeKey, Bond> _bonds;

    // AtomPackets: AtomId.Value -> AtomPacket
    private readonly Dictionary<int, AtomPacket> _atomPackets;

    public AtomId StartAtom { get; }
    public IReadOnlyList<AtomId> Atoms { get; }

    public MoleculeGraphAdapter(StructureGraph structure, PacketSettings packetSettings)
    {
        _structure = structure;
        _wavelengthMapper = new WavelengthToMidiPitchMapper();
        _atomPackets = new Dictionary<int, AtomPacket>();

        StartAtom = new AtomId(_structure.Atoms.Min(a => a.Index));

        // 1) AtomPackets erzeugen (Mapping ist dein künstlerischer Teil)
        foreach (var a in _structure.Atoms)
        {
            var pitch = GetPitchFromAtomNode(a);
            var velocity = (int)Math.Clamp(20 + a.ElementProps.AtomicWeight, 0, 127);
            var ccs = new ConcurrentDictionary<int, int>();
            var cc1 = (int)NumericUtils.Map(a.ElementProps.ElectronegativityPauling ?? 2.2, 0.7, 4.0, 0, 127, true);
            ccs[1] = cc1;
            _atomPackets.Add(
                a.Index,
                new AtomPacket(
                    pitch.pitch,
                    velocity,
                    pitch.pitchbend,
                    ccs,
                    IgnorePitch: packetSettings.IgnorePitch,
                    IgnorePitchbend: packetSettings.IgnorePitchbend,
                    IgnoreCc: packetSettings.IgnoreCCs));
        }

        // 2) Bonds + Adjazenzlisten aufbauen
        _neighbors = new Dictionary<int, List<AtomId>>();
        _bonds = new Dictionary<EdgeKey, Bond>();

        foreach (var atom in _structure.Atoms)
        {
            _neighbors[atom.Index] = new List<AtomId>();
        }

        foreach (var bond in _structure.Bonds)
        {
            var a = new AtomId(bond.From);
            var b = new AtomId(bond.To);

            _neighbors[a.Value].Add(b);
            _neighbors[b.Value].Add(a);

            var length = bond.Length3D;

            var key = EdgeKey.From(a, b);
            _bonds[key] = new Bond(a, b, length ?? 1.0);

            // 3) Neighbor-Listen deterministisch sortieren
            foreach (var list in _neighbors.Values)
            {
                list.Sort((x, y) => x.Value.CompareTo(y.Value));
            }
        }
    }

    public AtomPacket GetAtomPacket(AtomId atom) => _atomPackets[atom.Value];


    public IReadOnlyList<AtomId> GetNeighbors(AtomId atom)
        => _neighbors.TryGetValue(atom.Value, out var list)
            ? list
            : Array.Empty<AtomId>();

    public Bond GetBond(AtomId u, AtomId v)
    {
        var key = EdgeKey.From(u, v);
        if (_bonds.TryGetValue(key, out var bond))
            return bond;

        throw new InvalidOperationException($"No bond between atom {u.Value} and {v.Value}");
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private (int pitch, int pitchbend) GetPitchFromAtomNode(AtomNode a)
    { 
        var midiPitchSigned = _wavelengthMapper.MapWavelengthNmToMidi(a.EmissionLines.First().WavelengthNm, 21, 108);
        return (midiPitchSigned.Note, midiPitchSigned.PitchBendSigned);
    }

}