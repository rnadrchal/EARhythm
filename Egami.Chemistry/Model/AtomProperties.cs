namespace Egami.Chemistry.Model;

public sealed record AtomProperties
{
    public int FormalCharge { get; init; }

    public int? ImplicitHydrogenCount { get; init; }
    public int TotalHydrogenCount { get; init; } // explicit + implicit

    public int Degree { get; init; }
    public int Valence { get; init; }            // approx: sum bond orders

    public bool IsAromatic { get; init; }
    public bool IsInRing { get; init; }
    public int? SmallestRingSize { get; init; }

    public string? Hybridization { get; init; }  // optional / toolkit-dependent

    public double? PartialCharge { get; init; }  // optional (if you compute it)
    public bool IsHBondDonor { get; init; }
    public bool IsHBondAcceptor { get; init; }
    public bool IsHeteroAtom { get; init; }

    public IEnumerable<string> Tags
    {
        get
        {
            if (IsHBondAcceptor) yield return "H-acceptor";
            if (IsHBondDonor) yield return "H-donor";
            if (IsAromatic) yield return "aromatic";
            if (IsInRing) yield return "in-ring";
            if (!string.IsNullOrWhiteSpace(Hybridization)) yield return $"hybridization: {Hybridization}";
            if (IsHeteroAtom) yield return "hetero";
            if (PartialCharge.HasValue) yield return $"part.-charge: {PartialCharge.Value}";
            if (SmallestRingSize.HasValue) yield return $"smallest-ring-size{SmallestRingSize.Value}";
        }
    }

    public bool HasTags => Tags.Any();
}