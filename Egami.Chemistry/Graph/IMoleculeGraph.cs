namespace Egami.Chemistry.Graph;

public interface IMoleculeGraph
{
    AtomId StartAtom { get; }                 // "1. Atom der Strukturformel"
    IReadOnlyList<AtomId> Atoms { get; }

    AtomPacket GetAtomPacket(AtomId atom);

    /// <summary>Neighbors deterministisch sortiert (für reproduzierbare BFS/DFS).</summary>
    IReadOnlyList<AtomId> GetNeighbors(AtomId atom);

    /// <summary>Bindungslänge für u-v (ungerichtet).</summary>
    Bond GetBond(AtomId u, AtomId v);
}