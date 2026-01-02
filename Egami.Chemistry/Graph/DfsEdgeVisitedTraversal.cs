namespace Egami.Chemistry.Graph;

internal sealed class DfsEdgeVisitedTraversal : ITraversalStrategy
{
    private sealed class Frame
    {
        public AtomId Node { get; }
        public AtomId? Parent { get; }
        public int NextIndex { get; set; }
        public int Depth { get; }
        public bool Entered { get; set; }

        public Frame(AtomId node, AtomId? parent, int depth)
        {
            Node = node;
            Parent = parent;
            Depth = depth;
            NextIndex = 0;
            Entered = false;
        }
    }

    public IEnumerable<TraversalSegment> Traverse(IMoleculeGraph graph, TraversalOptions options)
    {
        var visitedEdges = new HashSet<EdgeKey>();
        var nodeVisits = new Dictionary<int, int>();

        // für OnlyInCycles:
        var onStack = new HashSet<int>();
        var nodeInCycle = new HashSet<int>();

        // für OnlyWhenBranching:
        var nodeIsBranching = new Dictionary<int, bool>();

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
            return c < options.MaxRevisitsPerNode;
        }

        bool IsBranchingAt(AtomId u)
            => nodeIsBranching.TryGetValue(u.Value, out var b) && b;

        var start = graph.StartAtom;
        IncNode(start);

        var stack = new Stack<Frame>();
        stack.Push(new Frame(start, parent: null, depth: 0));

        while (stack.Count > 0)
        {
            var frame = stack.Pop();
            var u = frame.Node;

            // "enter" node (für Zyklusdetektion)
            if (!frame.Entered)
            {
                frame.Entered = true;
                onStack.Add(u.Value);
            }

            var neighbors = graph.GetNeighbors(u);

            // Branching-Flag (einmalig pro Node berechnen): wie viele "vorwärts" Kandidaten?
            // (ohne Parent, nur echte "weiter" Kanten)
            if (!nodeIsBranching.ContainsKey(u.Value))
            {
                int forwardCandidates = 0;
                foreach (var v in neighbors)
                {
                    if (frame.Parent.HasValue && v.Value == frame.Parent.Value.Value) continue;

                    var key = EdgeKey.From(u, v);
                    var isNewEdge = !visitedEdges.Contains(key);
                    if (isNewEdge || CanVisitNode(v))
                        forwardCandidates++;

                    if (forwardCandidates >= 2) break;
                }
                nodeIsBranching[u.Value] = forwardCandidates >= 2;
            }

            // suche nächsten gültigen Nachbarn
            while (frame.NextIndex < neighbors.Count)
            {
                var v = neighbors[frame.NextIndex];
                frame.NextIndex++;

                // Zyklusdetektion: Nachbar bereits auf Stack (aber nicht Parent)
                if (onStack.Contains(v.Value) && (!frame.Parent.HasValue || v.Value != frame.Parent.Value.Value))
                {
                    nodeInCycle.Add(u.Value);
                    nodeInCycle.Add(v.Value);
                }

                var key = EdgeKey.From(u, v);
                var isNewEdge = !visitedEdges.Contains(key);

                if (isNewEdge || CanVisitNode(v))
                {
                    // wir kommen später zu u zurück -> frame wieder pushen
                    stack.Push(frame);

                    // traverse u -> v (immer als "forward"-Segment)
                    var bond = graph.GetBond(u, v);
                    if (isNewEdge)
                        visitedEdges.Add(key);

                    IncNode(v);

                    yield return new TraversalSegment(u, v, bond.Length3D, IsBacktrack: false);

                    // dive
                    stack.Push(new Frame(v, parent: u, depth: frame.Depth + 1));
                    goto ContinueOuter;
                }
            }

            // Node fertig => Stack verlassen
            onStack.Remove(u.Value);

            // Backtrack Segment nur selektiv ausgeben
            if (frame.Parent.HasValue && ShouldPlayBacktrack(options, frame, nodeInCycle, IsBranchingAt))
            {
                var p = frame.Parent.Value;
                var bond = graph.GetBond(u, p);
                yield return new TraversalSegment(u, p, bond.Length3D, IsBacktrack: true);
            }

        ContinueOuter:
            ;
        }
    }

    private static bool ShouldPlayBacktrack(
        TraversalOptions opt,
        Frame frame,
        HashSet<int> nodeInCycle,
        Func<AtomId, bool> isBranchingAt)
    {
        return opt.BacktrackPolicy switch
        {
            BacktracePolicy.Always => true,
            BacktracePolicy.Never => false,

            // Ring/Zyklus-Betonung:
            BacktracePolicy.OnlyInCycles
                => nodeInCycle.Contains(frame.Node.Value),

            // Verzweigungs-Betonung:
            // (Rückweg nur, wenn der aktuelle Node oder sein Parent als branching markiert ist)
            BacktracePolicy.OnlyWhenBranching
                => isBranchingAt(frame.Node) || (frame.Parent.HasValue && isBranchingAt(frame.Parent.Value)),

            // Form-Betonung:
            BacktracePolicy.OnlyIfDepthAtLeast
                => frame.Depth >= opt.BacktrackMinDepth,

            _ => true
        };
    }
}
