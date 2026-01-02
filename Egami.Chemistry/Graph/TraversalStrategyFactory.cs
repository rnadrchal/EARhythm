namespace Egami.Chemistry.Graph;

public static class TraversalStrategyFactory
{
    public static ITraversalStrategy Create(TraversalMode mode) => mode switch
    {
        TraversalMode.DfsEdgeVisited => new DfsEdgeVisitedTraversal(),
        TraversalMode.BfsSequential => new BfsSequentialTraversal(),
        TraversalMode.BfsLayerChord => new BfsLayerChordTraversal(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}