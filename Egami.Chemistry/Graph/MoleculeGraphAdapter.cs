using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Egami.Chemistry.Model;
using Egami.Chemistry.Spectrum;
using Egami.Sequencer.Common;

namespace Egami.Chemistry.Graph;

public enum PitchSelect
{
    [Display(Name = "λ1")]
    Wavelength1,
    [Display(Name = "λ2")]
    Wavelength2,
    [Display(Name = "λ3")]
    Wavelength3,
    [Display(Name = "Intensity")]
    Intensity,
    [Display(Name = "Random")]
    Random,
    [Display(Name = "Average")]
    Average
}

public sealed record PacketSettings(
    bool IgnorePitch = false,
    bool IgnorePitchbend = false,
    bool IgnoreCCs = false,
    PitchSelect PitchSelect = PitchSelect.Wavelength1,
    PitchRange PitchRange = PitchRange.Piano
)
{
    public bool IgnorePitch { get; set; }
    public bool IgnorePitchbend { get; set; }
    public bool IgnoreCCs { get; set; }
    public PitchSelect PitchSelect { get; set; }
    public PitchRange PitchRange { get; set; }
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
            var pitch = GetPitchFromAtomNode(a, packetSettings);
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

    private (int pitch, int pitchbend) GetPitchFromAtomNode(AtomNode a, PacketSettings settings)
    {
        var pitchRange = MapPitchRange(settings.PitchRange);
        var emissionLines = a.EmissionLines.ToArray();
        var midiPitchSigned = settings.PitchSelect switch
        {
            PitchSelect.Wavelength1 => _wavelengthMapper.MapWavelengthNmToMidi(emissionLines[0].WavelengthNm, pitchRange.lower, pitchRange.upper),
            PitchSelect.Wavelength2 => _wavelengthMapper.MapWavelengthNmToMidi(
                emissionLines[emissionLines.Length > 1 ? 1 : 0].WavelengthNm, pitchRange.lower, pitchRange.upper),
            PitchSelect.Wavelength3 => _wavelengthMapper.MapWavelengthNmToMidi(
                emissionLines[emissionLines.Length > 2 ? 2 : 0].WavelengthNm, pitchRange.lower, pitchRange.upper),
            PitchSelect.Intensity => _wavelengthMapper.MapWavelengthNmToMidi(
                ChooseWeightedRandom(emissionLines)?.WavelengthNm ?? emissionLines[0].WavelengthNm, pitchRange.lower, pitchRange.upper),
            PitchSelect.Random => _wavelengthMapper.MapWavelengthNmToMidi(
                emissionLines[Random.Shared.Next(emissionLines.Length)].WavelengthNm, pitchRange.lower, pitchRange.upper),
            PitchSelect.Average => _wavelengthMapper.MapWavelengthNmToMidi(
                emissionLines.Select(e => e.WavelengthNm).Average(), pitchRange.lower, pitchRange.upper),
            _ => _wavelengthMapper.MapWavelengthNmToMidi(emissionLines[0].WavelengthNm, 21, 108)
        };
        return (midiPitchSigned.Note, midiPitchSigned.PitchBendSigned);
    }

    /// <summary>
    /// Wählt zufällig eine <see cref="SpectralLine"/> aus <paramref name="lines"/>,
    /// gewichtet nach <see cref="SpectralLine.Intensity"/>.
    /// Liefert null, wenn keine gültigen Einträge vorhanden sind.
    /// </summary>
    /// <param name="lines">Quellsequenz von SpectralLine</param>
    /// <param name="rng">Optionaler Random; wenn null wird ein neuer Random erzeugt</param>
    public static SpectralLine? ChooseWeightedRandom(IEnumerable<SpectralLine>? lines, Random? rng = null)
    {
        if (lines == null) return null;

        // Filtere nur auf finite, nicht-negative Intensitäten (negativ -> 0)
        var valid = lines
            .Select(s => (Line: s, Weight: double.IsFinite(s.Intensity) && s.Intensity > 0 ? s.Intensity : 0.0))
            .ToArray();

        double total = 0.0;
        foreach (var v in valid) total += v.Weight;

        if (!(total > 0.0)) // total <= 0 oder NaN/Infinity treated as no valid weight
            return null;

        rng ??= Random.Shared;

        double r = rng.NextDouble() * total;
        double acc = 0.0;
        foreach (var v in valid)
        {
            acc += v.Weight;
            if (r <= acc)
                return v.Line;
        }

        // Numerische Sicherheit: gib das letzte Element zurück, falls nichts getroffen wurde
        return valid.Length > 0 ? valid[^1].Line : (SpectralLine?)null;
    }

    /// <summary>
    /// Versucht, ein Element zu wählen; gibt true zurück und liefert das Ergebnis in out parameter.
    /// </summary>
    public static bool TryChooseWeightedRandom(IEnumerable<SpectralLine>? lines, out SpectralLine result, Random? rng = null)
    {
        var sel = ChooseWeightedRandom(lines, rng);
        if (sel is null)
        {
            result = default;
            return false;
        }
        result = sel.Value;
        return true;
    }

    public static double[]? NormalizedWeights(IEnumerable<SpectralLine>? lines, double power = 1.0)
    {
        if (lines == null) return null;
        var arr = lines.Select(s => Math.Max(0.0, double.IsFinite(s.Intensity) ? s.Intensity : 0.0))
            .Select(v => Math.Pow(v, power))
            .ToArray();
        double sum = arr.Sum();
        if (!(sum > 0.0)) return null;
        for (int i = 0; i < arr.Length; i++) arr[i] /= sum;
        return arr;
    }

    public static (int lower, int upper) MapPitchRange(PitchRange range)
    {
        return range switch
        {
            PitchRange.Full => (0, 127),
            PitchRange.Piano => (21, 108),
            PitchRange.Bass => (21, 55),
            PitchRange.Cello => (36, 81),
            PitchRange.Violin => (55, 100),
            PitchRange.Guitar => (40, 88),
            PitchRange.Flute => (60, 96),
            PitchRange.BassVocal => (40, 64),
            PitchRange.BartoneVocal => (45, 69),
            PitchRange.TenorVocal => (48, 72),
            PitchRange.AltVocal => (55, 76),
            PitchRange.SopranoVocal => (60, 81),
            _ => (21, 108)
        };
    }
}

public enum PitchRange
{
    [Display(Name = "Full")]
    Full,       // 0 - 127
    [Display(Name = "Piano")]
    Piano,      // 21 - 108
    [Display(Name = "Bass")]
    Bass,       // 21 - 55
    [Display(Name = "Cello")]
    Cello,       // 36 - 81
    [Display(Name = "Violin")]
    Violin,     // 55 - 100
    [Display(Name = "Guitar")]
    Guitar,     // 40 - 88
    [Display(Name = "Flute")]
    Flute,      // 60 - 96
    [Display(Name = "Bass Voc.")]
    BassVocal,  // 40 - 64
    [Display(Name = "Bar. Voc.")]
    BartoneVocal,   // 45 - 69
    [Display(Name = "Ten. Voc.")]
    TenorVocal, // 48 - 72
    [Display(Name = "Alt Voc.")]
    AltVocal,   // 55 - 76
    [Display(Name = "Sop. Voc.")]
    SopranoVocal, // 60 - 81
}

