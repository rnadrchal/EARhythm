using MathNet.Numerics;

namespace Egami.Chemistry.Model;

public sealed record BondEdge
{
    public required int From { get; init; }
    public required int To { get; init; }

    public required int Order { get; init; }          // 1,2,3; 4=aromatic (Konvention)
    public bool IsAromatic { get; init; }
    public string? Stereo { get; init; }

    public bool IsInRing { get; init; }
    public int? SmallestRingSize { get; init; }

    public bool IsConjugated { get; init; }
    public bool IsRotatable { get; init; }

    public double? Length2D { get; init; }
    public double? Length3D { get; init; }

    public string? BondClass { get; init; }
    public double? PolarityProxy { get; init; }

    public IEnumerable<string> Tags
    {
        get
        {
            if (IsAromatic) yield return "aromatic";
            if (!String.IsNullOrWhiteSpace(Stereo)) yield return $"stereo: {Stereo}";
            if (IsInRing) yield return "in-ring";
            if (IsConjugated) yield return "conjugated";
            if (IsRotatable) yield return "rotatable";
            if (Length2D.HasValue) yield return $"Length 2D:{Length2D}";
            if (Length3D.HasValue) yield return $"Length 3D-{Length3D}";
            if (!String.IsNullOrWhiteSpace(BondClass)) yield return $"class: {BondClass}";
            if (PolarityProxy.HasValue) yield return $"polarity: {PolarityProxy:N2}";
        }
    }

    public bool HasTags => Tags.Any();
}