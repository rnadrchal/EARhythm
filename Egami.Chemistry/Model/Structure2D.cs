namespace Egami.Chemistry.Model;

public sealed record Structure2D
{
    // Minimum viable: line notation + optional depiction
    public string? Smiles { get; init; }
    public string? InChI { get; init; }

    // Optional: echte 2D-Graph-Struktur (aus SDF/MOL)
    public IReadOnlyList<Atom2D> Atoms { get; init; } = Array.Empty<Atom2D>();
    public IReadOnlyList<Bond2D> Bonds { get; init; } = Array.Empty<Bond2D>();

    // Optional: PubChem 2D depiction als PNG endpoint (oder du speicherst Bytes)
    public Uri? DepictionPngUrl { get; init; }
}