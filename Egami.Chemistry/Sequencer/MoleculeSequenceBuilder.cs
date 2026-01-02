using Egami.Chemistry.Graph;
using Egami.Chemistry.Model;
using Egami.Sequencer;
using Melanchall.DryWetMidi.Common;
using NCDK.AtomTypes;

namespace Egami.Chemistry.Sequencer;

public sealed record MoleculeSequenceBuildOptions(
    TraversalMode Mode,
    TraversalOptions TraversalOptions,
    bool Polyphonic = false,
    bool BfsLayerDurationUseMax = true,
    int InitialHoldSteps = 1,

    // --- Backtracking-Marker (nur relevant für DFS mit PlayBacktrackEdges=true) ---
    bool MarkBacktracking = true,
    int BacktrackMarkerCcNumber = 11, // 0..127
    SevenBitNumber BacktrackMarkerCcValue = default, // default -> 127 (siehe ctor unten)
    double BacktrackVelocityScale = 0.85, // 0..1 (0.85 = leicht leiser)
    bool BacktrackInvertPitchbend = false, // invertiert PB für Backtrack-Schritte
    int BacktrackPitchbendOffset = 0 // additiver Offset (clamped)
)
{
    public TraversalMode Mode { get; set; }
    public bool Polyphonic { get; set; }
    public bool MarkBacktracking { get; set; }
    public double BacktrackVelocityScale { get; set; }
    public int BacktrackMarkerCcNumber { get; set; }
    public SevenBitNumber BacktrackMarkerCcValue { get; set; }
    public bool BacktrackInvertPitchbend { get; set; }
    public int BacktrackPitchbendOffset { get; set; }
}

public sealed class MoleculeSequenceBuilder
{
    private readonly IMoleculeGraph _graph;
    private readonly IBondDurationMapper _durationMapper;
    private readonly MoleculeModel _model;

    public MoleculeSequenceBuilder(IMoleculeGraph graph, IBondDurationMapper durationMapper, MoleculeModel model)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _durationMapper = durationMapper ?? throw new ArgumentNullException(nameof(durationMapper));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public MusicalSequence Build(MoleculeSequenceBuildOptions opt)
        => opt.Mode switch
        {
            TraversalMode.DfsEdgeVisited => BuildFromSegments(
                TraversalStrategyFactory.Create(opt.Mode).Traverse(_graph, opt.TraversalOptions),
                opt),

            TraversalMode.BfsSequential => BuildFromSegments(
                TraversalStrategyFactory.Create(opt.Mode).Traverse(_graph, opt.TraversalOptions),
                opt),

            TraversalMode.BfsLayerChord => BuildBfsLayerChord(opt),

            _ => throw new ArgumentOutOfRangeException(nameof(opt.Mode), opt.Mode, null)
        };

    private MusicalSequence BuildFromSegments(IEnumerable<TraversalSegment> segments, MoleculeSequenceBuildOptions opt)
    {
        var steps = new List<SequenceStep>();
        int stepIndex = 0;

        // Optionaler Start-Hold
        if (opt.InitialHoldSteps > 0)
        {
            var startPacket = _graph.GetAtomPacket(_graph.StartAtom);
            var step = CreateStep(stepIndex, opt.InitialHoldSteps, startPacket, isBacktrack: false, opt);
            var atom = _model.GetAtomNode(_graph.StartAtom);
            if (atom != null) step.Atoms = [atom];
            steps.Add(step);
            stepIndex += opt.InitialHoldSteps;
        }

        // Monophon: Segment => Step (gespielt wird "From"; Dauer kommt von BondLength)
        foreach (var seg in segments)
        {
            var duration = _durationMapper.GetDurationInSteps(seg.BondLength3D);
            var packet = _graph.GetAtomPacket(seg.From);
            var step = CreateStep(stepIndex, duration, packet, seg.IsBacktrack, opt);
            var atoms = new List<AtomNode>();
            var atomFrom = _model.GetAtomNode(seg.From);
            var atomTo = _model.GetAtomNode(seg.To);
            if (atomFrom != null && atomTo != null)
                step.Atoms = [atomFrom, atomTo];
            steps.Add(step);
            stepIndex += duration;
        }

        return new MusicalSequence(steps);
    }

    private MusicalSequence BuildBfsLayerChord(MoleculeSequenceBuildOptions opt)
    {
        var steps = new List<SequenceStep>();
        int stepIndex = 0;

        var visited = new HashSet<int>();
        var nodeVisits = new Dictionary<int, int>();

        int IncNode(AtomId id)
        {
            if (!nodeVisits.TryGetValue(id.Value, out var c)) c = 0;
            c++;
            nodeVisits[id.Value] = c;
            return c;
        }

        bool CanVisitNode(AtomId id)
        {
            nodeVisits.TryGetValue(id.Value, out var c);
            return c < opt.TraversalOptions.MaxRevisitsPerNode;
        }

        var layer = new List<AtomId> { _graph.StartAtom };
        visited.Add(_graph.StartAtom.Value);
        IncNode(_graph.StartAtom);

        while (layer.Count > 0)
        {
            var nextLayer = new List<AtomId>();
            var nextDurations = new List<int>();

            foreach (var u in layer)
            {
                foreach (var v in _graph.GetNeighbors(u))
                {
                    var isNew = !visited.Contains(v.Value);
                    if (!isNew && !CanVisitNode(v))
                        continue;

                    var bond = _graph.GetBond(u, v);
                    nextDurations.Add(_durationMapper.GetDurationInSteps(bond.Length3D));

                    if (isNew)
                    {
                        visited.Add(v.Value);
                        nextLayer.Add(v);
                    }

                    IncNode(v);
                }
            }

            int layerDuration;
            if (nextDurations.Count == 0)
            {
                layerDuration = Math.Max(1, opt.InitialHoldSteps);
            }
            else if (opt.BfsLayerDurationUseMax)
            {
                layerDuration = nextDurations.Max();
            }
            else
            {
                layerDuration = Math.Max(1, (int)Math.Round(nextDurations.Average()));
            }

            // Layer als Akkord: alle Nodes starten auf gleichem stepIndex
            foreach (var node in layer)
            {
                var packet = _graph.GetAtomPacket(node);
                var step = CreateStep(stepIndex, layerDuration, packet, isBacktrack: false, opt);
                var atom = _model.GetAtomNode(node);
                if (atom != null) step.Atoms = [atom];
                steps.Add(step);
            }

            stepIndex += layerDuration;
            layer = nextLayer;
        }

        return new MusicalSequence(steps);
    }

    private static AtomSequenceStep CreateStep(
        int stepIndex,
        int lengthInSteps,
        AtomPacket packet,
        bool isBacktrack,
        MoleculeSequenceBuildOptions opt)
    {
        // 1) CCs kopieren (oder leer lassen)
        var cc = packet.Ccs is null
            ? new Dictionary<int, SevenBitNumber>()
            : new Dictionary<int, SevenBitNumber>(packet.Ccs.Count);

        if (packet.Ccs != null)
        {
            foreach (var kv in packet.Ccs)
                cc[kv.Key] = (SevenBitNumber)kv.Value;
        }

        // 2) Backtrack-Markierung anwenden (wenn gewünscht)
        var pitchBend = packet.PitchBend;
        var velocity = packet.Velocity;

        if (isBacktrack && opt.MarkBacktracking)
        {
            // CC Marker
            if (!packet.IgnoreCc && opt.BacktrackMarkerCcNumber is >= 0 and <= 127)
            {
                cc[opt.BacktrackMarkerCcNumber] = opt.BacktrackMarkerCcValue;
            }

            // Velocity scaling (clamp 1..127)
            if (!packet.IgnorePitch)
            {
                var scaled = (int)Math.Round(velocity * Math.Clamp(opt.BacktrackVelocityScale, 0.0, 1.0));
                if (scaled < 1) scaled = 1;
                if (scaled > 127) scaled = 127;
                velocity = (SevenBitNumber)scaled;
            }

            // Pitchbend mod
            if (!packet.IgnorePitchbend)
            {
                var pb = pitchBend;

                if (opt.BacktrackInvertPitchbend)
                    pb = -pb;

                pb += opt.BacktrackPitchbendOffset;

                // clamp to -8192..+8191
                if (pb < -8192) pb = -8192;
                if (pb > 8191) pb = 8191;

                pitchBend = pb;
            }
        }
        else
        {
            if (!packet.IgnoreCc && opt.BacktrackMarkerCcNumber is >= 0 and <= 127)
            {
                cc[opt.BacktrackMarkerCcNumber] = (SevenBitNumber)0;
            }

        }

        // 3) Ramps: optional auch Backtrack-Invert/Offset auf PitchbendRamp anwenden
        GridPitchbendRamp? pbRamp = packet.PitchbendRamp is null
                ? null
                : new GridPitchbendRamp(
                    StartValue: packet.PitchbendRamp.StartValue,
                    EndValue: packet.PitchbendRamp.EndValue,
                    DurationPulses: packet.PitchbendRamp.DurationPulses);

        if (isBacktrack && opt.MarkBacktracking && pbRamp is not null && !packet.IgnorePitchbend)
        {
            var s = pbRamp.StartValue;
            var e = pbRamp.EndValue;

            if (opt.BacktrackInvertPitchbend)
            {
                s = -s;
                e = -e;
            }

            s += opt.BacktrackPitchbendOffset;
            e += opt.BacktrackPitchbendOffset;

            s = Math.Clamp(s, -8192, 8191);
            e = Math.Clamp(e, -8192, 8191);

            pbRamp = pbRamp with { StartValue = s, EndValue = e };
        }

        IReadOnlyList<GridCcRamp>? ccRamps = packet.CcRamps; // unverändert (Backtrack könnte man später auch einfärben)

        var step = new AtomSequenceStep(
            stepIndex: stepIndex,
            lengthInSteps: lengthInSteps,
            pitch: (SevenBitNumber)packet.Pitch,
            velocity: (SevenBitNumber)velocity,
            pitchBend: pitchBend,
            ccValues: cc,
            ignorePitch: packet.IgnorePitch,
            ignorePitchbend: packet.IgnorePitchbend,
            ignoreCc: packet.IgnoreCc,
            pitchbendRamp: pbRamp,
            ccRamps: ccRamps);

        return step;
    }
}
