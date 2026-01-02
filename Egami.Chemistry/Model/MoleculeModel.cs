using Egami.Chemistry.Graph;

namespace Egami.Chemistry.Model;

public sealed record MoleculeModel
{
    public required MoleculeIdentifiers Ids { get; init; }
    public required string PreferredName { get; init; }
    public IReadOnlyList<string> Synonyms { get; init; } = Array.Empty<string>();
    public required ChemicalProperties Properties { get; init; }

    // Graph = “2D Struktur” (Topologie) + optional Layout-Koordinaten
    public required StructureGraph Graph { get; init; }

    // Optional: PNG-Darstellung als Bytes (kann null sein, falls Download fehlschlägt)
    public byte[]? DepictionPng { get; init; }

    public IReadOnlyList<TaxonomyLink> Taxonomy { get; init; } = Array.Empty<TaxonomyLink>();
    public MeshScrLink? MeshScr { get; init; }

    public AtomNode? GetAtomNode(AtomId id) => Graph.Atoms.SingleOrDefault(a => a.Index == id.Value);

}