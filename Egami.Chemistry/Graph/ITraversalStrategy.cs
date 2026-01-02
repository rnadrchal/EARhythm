using System.ComponentModel.DataAnnotations;

namespace Egami.Chemistry.Graph;

public enum TraversalMode
{
    [Display(Name = "DFS")]
    DfsEdgeVisited,
    [Display(Name = "BFS seq.")]
    BfsSequential,
    [Display(Name = "BFS poly")]
    BfsLayerChord
}

public sealed record TraversalOptions(
    int MaxRevisitsPerNode = 2,

    // ersetzt PlayBacktrackEdges / oder du lässt beides drin
    BacktracePolicy BacktrackPolicy = BacktracePolicy.Always,

    // Policy-Parameter:
    int BacktrackMinDepth = 2
)
{
    public int MaxRevisitsPerNode { get; set; } = MaxRevisitsPerNode;
    public BacktracePolicy BacktrackPolicy { get; set; } = BacktrackPolicy;
    public int BacktrackMinDepth { get; set; } = BacktrackMinDepth;
}

public readonly record struct EdgeKey(int Min, int Max)
{
    public static EdgeKey From(AtomId a, AtomId b)
    {
        var x = a.Value;
        var y = b.Value;
        return x <= y ? new EdgeKey(x, y) : new EdgeKey(y, x);
    }
}

public interface ITraversalStrategy
{
    IEnumerable<TraversalSegment> Traverse(IMoleculeGraph graph, TraversalOptions options);
}

public readonly record struct TraversalSegment(
    AtomId From,
    AtomId To,
    double BondLength3D,
    bool IsBacktrack);
