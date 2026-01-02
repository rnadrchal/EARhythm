namespace Egami.Chemistry.Model;

public sealed record StructureGraph
{
    public required IReadOnlyList<AtomNode> Atoms { get; init; }
    public required IReadOnlyList<BondEdge> Bonds { get; init; }

    // Optional: 2D Layout (Index → X/Y)
    public IReadOnlyDictionary<int, Atom2DPoint> Layout2D { get; init; }
        = new Dictionary<int, Atom2DPoint>();
}

public sealed record Atom2DPoint(double X, double Y);