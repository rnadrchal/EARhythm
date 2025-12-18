using Egami.Chemistry.Model;
using Egami.Chemistry.Spectrum;
using NCDK.Smiles;

namespace Egami.Chemistry.Services;

public sealed class MoleculeModelBuilder
{
    private readonly SmilesParser _smilesParser;
    private readonly AtomGraphBuilder _atomBuilder;
    private readonly BondEdgeEnricher _bondEnricher;

    public MoleculeModelBuilder(IElementPropertyProvider elements, ISpectralLineProvider spectralLineProvider)
    {
        _smilesParser = new SmilesParser(NCDK.Silent.ChemObjectBuilder.Instance);
        _atomBuilder = new AtomGraphBuilder(elements, spectralLineProvider);
        _bondEnricher = new BondEdgeEnricher(elements);
    }

    public StructureGraph BuildGraphFromSmiles(string smiles)
    {
        if (string.IsNullOrWhiteSpace(smiles))
            throw new ArgumentException("SMILES is required.", nameof(smiles));

        var mol = _smilesParser.ParseSmiles(smiles);

        // Optional: 2D coordinate generation could be added here
        // If you want this, tell me which NCDK version you use, then I’ll wire it in reliably.

        var atoms = _atomBuilder.BuildAtoms(mol);
        var bonds = _bondEnricher.BuildEdges(mol);

        // Optional: if you generated 2D coords, fill Layout2D from atom.Point2D
        var layout2D = new Dictionary<int, Atom2DPoint>();
        for (var i = 0; i < mol.Atoms.Count; i++)
        {
            var p = mol.Atoms[i].Point2D;
            if (p is null) continue;
            layout2D[i] = new Atom2DPoint(p.Value.X, p.Value.Y);
        }

        return new StructureGraph
        {
            Atoms = atoms,
            Bonds = bonds,
            Layout2D = layout2D
        };
    }
}