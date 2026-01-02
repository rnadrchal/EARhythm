using Egami.Chemistry.Model;
using Egami.Chemistry.Spectrum;
using NCDK;
using NCDK.RingSearches;

namespace Egami.Chemistry.Services;

public sealed class AtomGraphBuilder
{
    private readonly IElementPropertyProvider _elements;
    private readonly ISpectralLineProvider _spectralLineProvider;

    public AtomGraphBuilder(IElementPropertyProvider elements, ISpectralLineProvider spectralLineProvider)
    {
        _elements = elements;
        _spectralLineProvider = spectralLineProvider;
    }

    public IReadOnlyList<AtomNode> BuildAtoms(IAtomContainer mol)
    {
        var ringSearch = new RingSearch(mol);

        // optional: smallest ring size per atom (can be computed similar to bond ring sizes; here minimal)
        var atoms = new List<AtomNode>(mol.Atoms.Count);

        for (var i = 0; i < mol.Atoms.Count; i++)
        {
            var a = mol.Atoms[i];

            var elem = a.Symbol;
            var elemProps = _elements.TryGet(elem) ?? new ElementProps(0, 0, null);

            var neighbors = mol.GetConnectedAtoms(a).Select(x => mol.Atoms.IndexOf(x)).Where(ix => ix >= 0).ToArray();
            var bonds = mol.GetConnectedBonds(a).ToArray();

            var degree = neighbors.Length;
            var valenceApprox = bonds.Sum(b => BondOrderToInt(b.Order, b.IsAromatic));

            var implicitH = a.ImplicitHydrogenCount;
            var explicitH = mol.GetConnectedAtoms(a).Count(x => x.Symbol == "H");
            var totalH = (implicitH ?? 0) + explicitH;

            var isInRing = ringSearch.Cyclic(a);

            // Donor/acceptor quick heuristics (kept intentionally simple + interpretable)
            var isHetero = elem is not ("C" or "H");
            var isAcceptor = elem is "O" or "N" or "S" && (a.FormalCharge ?? 0) <= 0;
            var isDonor = (elem is "O" or "N") && totalH > 0;

            atoms.Add(new AtomNode
            {
                Index = i,
                Element = elem,
                ElementProps = new ElementProperties
                {
                    AtomicNumber = elemProps.AtomicNumber,
                    AtomicWeight = elemProps.AtomicWeight,
                    ElectronegativityPauling = elemProps.ElectronegativityPauling
                },
                AtomProps = new AtomProperties
                {
                    FormalCharge = a.FormalCharge ?? 0,
                    ImplicitHydrogenCount = implicitH,
                    TotalHydrogenCount = totalH,

                    Degree = degree,
                    Valence = valenceApprox,

                    IsAromatic = a.IsAromatic,
                    IsInRing = isInRing,
                    SmallestRingSize = null, // optional (can be added later)

                    Hybridization = null,    // optional (toolkit dependent)
                    PartialCharge = a.GetProperty<double?>("PartialCharge"),

                    IsHBondDonor = isDonor,
                    IsHBondAcceptor = isAcceptor,
                    IsHeteroAtom = isHetero
                },
                NeighborAtomIndices = neighbors,
                EmissionLines = _spectralLineProvider.GetDominentSpectralLines(elem).ToList()
            });
        }

        return atoms;
    }

    private static int BondOrderToInt(BondOrder? order, bool isAromatic)
        => isAromatic ? 4 : order switch
        {
            BondOrder.Single => 1,
            BondOrder.Double => 2,
            BondOrder.Triple => 3,
            _ => 1
        };
}