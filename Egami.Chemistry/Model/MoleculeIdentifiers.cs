namespace Egami.Chemistry.Model;

public sealed record MoleculeIdentifiers
{
    public int? PubChemCid { get; init; }
    public int? PubChemSid { get; init; }

    public double? MolecularWeight { get; init; }
    public double? ExactMass { get; init; }

    public string? InChI { get; init; }
    public string? InChIKey { get; init; }
    public string? CanonicalSmiles { get; init; }
    public string? IsomericSmiles { get; init; }

    public string? MolecularFormula { get; init; }
    public string? IupacName { get; init; }

    public string? Cas { get; init; }
    public string? Unii { get; init; }
}