using Egami.Chemistry.Services;
using Egami.Chemistry.Spectrum;
using NCDK;
using NCDK.Silent;
using NCDK.Smiles;

namespace Egami.Chemistry.Model
{
    public sealed class MoleculeModelBuilder
    {
        private readonly SmilesParser _smilesParser;
        private readonly AtomGraphBuilder _atomBuilder;
        private readonly BondEdgeEnricher _bondEnricher;

        public MoleculeModelBuilder(IElementPropertyProvider elements, ISpectralLineProvider spectralLineProvider)
        {
            // Wenn kein NCDK.Silent verfügbar ist, benutze den allgemeinen ChemObjectBuilder.
            _smilesParser = new SmilesParser(ChemObjectBuilder.Instance);
            _atomBuilder = new AtomGraphBuilder(elements, spectralLineProvider);
            _bondEnricher = new BondEdgeEnricher(elements);
        }

        public StructureGraph BuildGraphFromSmiles(string smiles)
        {
            if (string.IsNullOrWhiteSpace(smiles))
                throw new ArgumentException("SMILES darf nicht leer sein.", nameof(smiles));

            var mol = _smilesParser.ParseSmiles(smiles) ?? throw new InvalidOperationException("SMILES konnte nicht geparst werden.");
            return BuildGraphFromAtomContainer(mol);
        }

        public StructureGraph BuildGraphFromAtomContainer(IAtomContainer mol)
        {
            if (mol is null) throw new ArgumentNullException(nameof(mol));

            var atoms = _atomBuilder.BuildAtoms(mol);
            var bonds = _bondEnricher.BuildEdges(mol);

            return new StructureGraph
            {
                Atoms = atoms,
                Bonds = bonds
            };
        }
    }
}