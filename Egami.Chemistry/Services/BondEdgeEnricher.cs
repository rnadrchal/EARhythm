using Egami.Chemistry.Model;
using NCDK;
using NCDK.Graphs;
using NCDK.RingSearches;

namespace Egami.Chemistry.Services;

public sealed class BondEdgeEnricher
{
    private readonly IElementPropertyProvider _elements;

    public BondEdgeEnricher(IElementPropertyProvider elements)
        => _elements = elements;

    public IReadOnlyList<BondEdge> BuildEdges(IAtomContainer mol)
    {
        var ringSearch = new RingSearch(mol);
        var smallestRingSizeByBond = ComputeSmallestRingSizePerBond(mol);

        var edges = new List<BondEdge>(mol.Bonds.Count);

        foreach (var bond in mol.Bonds)
        {
            var a = bond.Begin;
            var b = bond.End;

            var aIdx = mol.Atoms.IndexOf(a);
            var bIdx = mol.Atoms.IndexOf(b);
            if (aIdx < 0 || bIdx < 0) continue;

            var isInRing = ringSearch.Cyclic(bond);
            smallestRingSizeByBond.TryGetValue(bond, out var ringSize);

            var isAromatic = bond.IsAromatic;
            var order = BondOrderToInt(bond.Order, isAromatic);

            var length2d = TryGetLength2D(a, b);
            var length3d = TryGetLength3D(a, b);

            var conjugated = IsConjugatedHeuristic(mol, bond);
            var rotatable = IsRotatableHeuristic(mol, bond, isInRing);

            var bondClass = ClassifyBond(mol, bond);
            var polarity = ComputePolarityProxy(bond);

            edges.Add(new BondEdge
            {
                From = aIdx,
                To = bIdx,
                Order = order,
                IsAromatic = isAromatic,
                Stereo = TryGetStereoLabel(bond),

                IsInRing = isInRing,
                SmallestRingSize = isInRing ? ringSize : null,

                IsConjugated = conjugated,
                IsRotatable = rotatable,

                Length2D = length2d,
                Length3D = length3d,

                BondClass = bondClass,
                PolarityProxy = polarity
            });
        }

        return edges;

        double? ComputePolarityProxy(IBond bondLocal)
        {
            var a = bondLocal.Begin;
            var b = bondLocal.End;

            // Prefer computed partial charges if present:
            var qA = TryGetPartialCharge(a);
            var qB = TryGetPartialCharge(b);
            if (qA.HasValue && qB.HasValue)
                return Math.Abs(qA.Value - qB.Value);

            // Fallback: electronegativity difference
            var ea = _elements.TryGet(a.Symbol);
            var eb = _elements.TryGet(b.Symbol);
            if (ea?.ElectronegativityPauling is null || eb?.ElectronegativityPauling is null) return null;
            return Math.Abs(ea.ElectronegativityPauling.Value - eb.ElectronegativityPauling.Value);
        }
    }

    private static int BondOrderToInt(BondOrder? order, bool isAromatic)
        => isAromatic ? 4 : order switch
        {
            BondOrder.Single => 1,
            BondOrder.Double => 2,
            BondOrder.Triple => 3,
            _ => 1
        };

    private static string? TryGetStereoLabel(IBond bond)
        => bond.Stereo switch
        {
            BondStereo.Up => "UP",
            BondStereo.Down => "DOWN",
            BondStereo.UpInverted => "UP_INV",
            BondStereo.DownInverted => "DOWN_INV",
            BondStereo.E => "E",
            BondStereo.Z => "Z",
            BondStereo.EOrZ => "E/Z",
            _ => null
        };

    private static double? TryGetLength2D(IAtom a, IAtom b)
    {
        var pa = a.Point2D;
        var pb = b.Point2D;
        if (pa is null || pb is null) return null;
        var dx = pa.Value.X - pb.Value.X;
        var dy = pa.Value.Y - pb.Value.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double? TryGetLength3D(IAtom a, IAtom b)
    {
        var pa = a.Point3D;
        var pb = b.Point3D;
        if (pa is null || pb is null) return null;
        var dx = pa.Value.X - pb.Value.X;
        var dy = pa.Value.Y - pb.Value.Y;
        var dz = pa.Value.Z - pb.Value.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static bool IsConjugatedHeuristic(IAtomContainer mol, IBond bond)
    {
        if (bond.IsAromatic) return true;
        if (bond.Order == BondOrder.Double || bond.Order == BondOrder.Triple) return true;
        if (bond.Order != BondOrder.Single) return false;

        var aHasUnsat = HasAdjacentUnsaturation(mol, bond.Begin, bond);
        var bHasUnsat = HasAdjacentUnsaturation(mol, bond.End, bond);
        return aHasUnsat && bHasUnsat;
    }

    private static bool HasAdjacentUnsaturation(IAtomContainer mol, IAtom atom, IBond exclude)
    {
        foreach (var nb in mol.GetConnectedBonds(atom))
        {
            if (ReferenceEquals(nb, exclude)) continue;
            if (nb.IsAromatic) return true;
            if (nb.Order == BondOrder.Double || nb.Order == BondOrder.Triple) return true;
        }
        return false;
    }

    private static bool IsRotatableHeuristic(IAtomContainer mol, IBond bond, bool isInRing)
    {
        if (isInRing) return false;
        if (bond.IsAromatic) return false;
        if (bond.Order != BondOrder.Single) return false;

        var degA = mol.GetConnectedAtoms(bond.Begin).Count();
        var degB = mol.GetConnectedAtoms(bond.End).Count();
        if (degA <= 1 || degB <= 1) return false;

        if (IsAmideBond(mol, bond)) return false;
        return true;
    }

    private static bool IsAmideBond(IAtomContainer mol, IBond bond)
    {
        var a = bond.Begin;
        var b = bond.End;

        IAtom? c = null;
        IAtom? n = null;

        if (a.Symbol == "C" && b.Symbol == "N") { c = a; n = b; }
        else if (a.Symbol == "N" && b.Symbol == "C") { c = b; n = a; }
        else return false;

        foreach (var cb in mol.GetConnectedBonds(c))
        {
            var other = cb.GetOther(c);
            if (other.Symbol == "O" && cb.Order == BondOrder.Double) return true;
        }

        return false;
    }

    private static string NormalizePair(string s1, string s2)
        => string.CompareOrdinal(s1, s2) <= 0 ? $"{s1}-{s2}" : $"{s2}-{s1}";

    private static string? ClassifyBond(IAtomContainer mol, IBond bond)
    {
        var a = bond.Begin;
        var b = bond.End;

        var pair = NormalizePair(a.Symbol, b.Symbol);

        if (bond.IsAromatic) return $"{pair} (aromatic)";
        if (bond.Order == BondOrder.Double) return $"{pair} (=)";
        if (bond.Order == BondOrder.Triple) return $"{pair} (#)";

        if (pair == "C-O" && IsAlcoholCO(mol, bond)) return "C-O (alcohol)";
        if (pair == "C-O" && IsEsterCO(mol, bond)) return "C-O (ester)";
        if (pair == "C-N" && IsAmideBond(mol, bond)) return "C-N (amide)";

        return $"{pair} (-)";
    }

    private static bool IsAlcoholCO(IAtomContainer mol, IBond bond)
    {
        IAtom? o = bond.Begin.Symbol == "O" ? bond.Begin : (bond.End.Symbol == "O" ? bond.End : null);
        IAtom? c = bond.Begin.Symbol == "C" ? bond.Begin : (bond.End.Symbol == "C" ? bond.End : null);
        if (o is null || c is null) return false;
        if (bond.Order != BondOrder.Single) return false;

        if (IsOAdjacentToCarbonyl(mol, o)) return false;

        var explicitH = mol.GetConnectedAtoms(o).Any(x => x.Symbol == "H");
        var implicitH = (o.ImplicitHydrogenCount ?? 0) > 0;
        return explicitH || implicitH;
    }

    private static bool IsEsterCO(IAtomContainer mol, IBond bond)
    {
        IAtom? o = bond.Begin.Symbol == "O" ? bond.Begin : (bond.End.Symbol == "O" ? bond.End : null);
        if (o is null) return false;
        if (bond.Order != BondOrder.Single) return false;
        return IsOAdjacentToCarbonyl(mol, o);
    }

    private static bool IsOAdjacentToCarbonyl(IAtomContainer mol, IAtom o)
    {
        foreach (var ob in mol.GetConnectedBonds(o))
        {
            var other = ob.GetOther(o);
            if (other.Symbol != "C") continue;

            foreach (var cb in mol.GetConnectedBonds(other))
            {
                var cOther = cb.GetOther(other);
                if (cOther.Symbol == "O" && cb.Order == BondOrder.Double)
                    return true;
            }
        }
        return false;
    }

    private static double? TryGetPartialCharge(IAtom atom)
    {
        // Best practice: store computed charges explicitly:
        // atom.SetProperty("PartialCharge", q);
        if (atom.GetProperty<double?>("PartialCharge") is double q)
            return q;

        return null;
    }

    private static Dictionary<IBond, int?> ComputeSmallestRingSizePerBond(IAtomContainer mol)
    {
        var result = new Dictionary<IBond, int?>();

        try
        {
            var mcb = Cycles.FindMCB(mol).ToRingSet(); // may vary by NCDK version
            foreach (var ring in mcb)
            {
                var ringSize = ring.Atoms.Count;
                foreach (var b in ring.Bonds)
                {
                    if (!result.TryGetValue(b, out var cur) || cur is null || ringSize < cur.Value)
                        result[b] = ringSize;
                }
            }
        }
        catch
        {
            // Ring size is optional; we still provide IsInRing via RingSearch.
        }

        return result;
    }
}