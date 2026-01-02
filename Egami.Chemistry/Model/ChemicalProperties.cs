namespace Egami.Chemistry.Model;

public sealed record ChemicalProperties
{
    public double? MolecularWeight { get; init; }
    public double? ExactMass { get; init; }
    public double? MonoisotopicMass { get; init; }

    public int? HydrogenBondDonorCount { get; init; }
    public int? HydrogenBondAcceptorCount { get; init; }
    public int? RotatableBondCount { get; init; }

    public double? XLogP { get; init; }
    public double? TopologicalPolarSurfaceArea { get; init; }

    public int? Charge { get; init; }
}
