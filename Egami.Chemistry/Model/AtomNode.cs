using Egami.Chemistry.Spectrum;

namespace Egami.Chemistry.Model;

public sealed record AtomNode
{
    public required int Index { get; init; }             // 0..n-1
    public required string Element { get; init; }        // "C","O","N"...

    public required ElementProperties ElementProps { get; init; }
    public required AtomProperties AtomProps { get; init; }

    public IReadOnlyList<int> NeighborAtomIndices { get; init; } = Array.Empty<int>();
    public IReadOnlyList<SpectralLine> EmissionLines { get; init; } = Array.Empty<SpectralLine>();
}
