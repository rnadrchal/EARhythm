namespace Egami.Chemistry.Model;

public sealed record Molecule
{
    public required MoleculeIdentifiers Ids { get; init; }
    public required string PreferredName { get; init; }
    public IReadOnlyList<string> Synonyms { get; init; } = Array.Empty<string>();

    public required ChemicalProperties Properties { get; init; }
    public required Structure2D Structure2D { get; init; }

    // “Taxonomisch” im Sinne von: Quelle/Organismus-Bezüge aus Annotationen (falls vorhanden)
    public IReadOnlyList<TaxonomyLink> Taxonomy { get; init; } = Array.Empty<TaxonomyLink>();

    // Optional: PubMed/MeSH SCR (substance concept), wenn du es mitführen willst
    public MeshScrLink? MeshScr { get; init; }
}