namespace Egami.Chemistry.Graph;

internal sealed class BfsSequentialTraversal : ITraversalStrategy
{
    public IEnumerable<TraversalSegment> Traverse(IMoleculeGraph graph, TraversalOptions options)
    {
        var visitedNodes = new HashSet<int>();
        var nodeVisits = new Dictionary<int, int>();
        var q = new Queue<AtomId>();

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

        var start = graph.StartAtom;
        visitedNodes.Add(start.Value);
        IncNode(start);
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            var neighbors = graph.GetNeighbors(u);

            foreach (var v in neighbors)
            {
                var isNew = !visitedNodes.Contains(v.Value);
                if (!isNew && !CanVisitNode(v))
                    continue;

                var bond = graph.GetBond(u, v);
                yield return new TraversalSegment(u, v, bond.Length3D, IsBacktrack: false);

                IncNode(v);

                if (isNew)
                {
                    visitedNodes.Add(v.Value);
                    q.Enqueue(v);
                }
            }
        }
    }
}