using Egami.Chemistry.Model;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Egami.Chemistry.PubChem;

public sealed class PubChemClient
{
    private readonly HttpClient _http;

    public PubChemClient(HttpClient http)
        => _http = http;

    public async Task<IReadOnlyList<int>> SearchCidsByNameAsync(string name, CancellationToken ct = default)
    {
        var url = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/{Uri.EscapeDataString(name)}/cids/JSON";
        var dto = await _http.GetFromJsonAsync<CidListDto>(url, ct);
        return dto?.IdentifierList?.CID ?? Array.Empty<int>();
    }

    public async Task<Molecule> GetMoleculeAsync(int cid, CancellationToken ct = default)
    {
        // properties block (keep it small and stable)
        var propsUrl =
            "https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/" + cid +
            "/property/MolecularFormula,MolecularWeight,ExactMass,MonoisotopicMass," +
            "CanonicalSMILES,IsomericSMILES,InChI,InChIKey,IUPACName,XLogP,TPSA," +
            "HBondDonorCount,HBondAcceptorCount,RotatableBondCount,Charge/JSON";

        var props = await _http.GetFromJsonAsync<PropertyTableDto>(propsUrl, ct);
        var p = props?.PropertyTable?.Properties?.FirstOrDefault()
            ?? throw new InvalidOperationException($"No properties returned for CID {cid}.");

        var depiction = new Uri($"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/PNG");

        // Optional: Synonyms (kann groß werden) – ggf. limitieren
        // /rest/pug/compound/cid/{cid}/synonyms/JSON  (in Doku beschrieben)
        // Für MVP: weglassen oder später hinzufügen.

        return new Molecule
        {
            PreferredName = p.IUPACName ?? $"CID {cid}",
            Ids = new MoleculeIdentifiers
            {
                PubChemCid = cid,
                MolecularFormula = p.MolecularFormula,
                MolecularWeight = p.MolecularWeight,
                ExactMass = p.ExactMass,
                InChI = p.InChI,
                InChIKey = p.InChIKey,
                CanonicalSmiles = p.CanonicalSMILES,
                IsomericSmiles = p.IsomericSMILES,
                IupacName = p.IUPACName
            },
            Properties = new ChemicalProperties
            {
                MolecularWeight = p.MolecularWeight,
                ExactMass = p.ExactMass,
                MonoisotopicMass = p.MonoisotopicMass,
                HydrogenBondDonorCount = p.HBondDonorCount,
                HydrogenBondAcceptorCount = p.HBondAcceptorCount,
                RotatableBondCount = p.RotatableBondCount,
                XLogP = p.XLogP,
                TopologicalPolarSurfaceArea = p.TPSA,
                Charge = p.Charge
            },
            Structure2D = new Structure2D
            {
                Smiles = p.IsomericSMILES ?? p.CanonicalSMILES,
                InChI = p.InChI,
                DepictionPngUrl = depiction
                // Atoms/Bonds: später per SDF Parser befüllen
            }
        };
    }

    private sealed class CidListDto
    {
        [JsonPropertyName("IdentifierList")] public IdentifierListDto? IdentifierList { get; init; }
    }

    private sealed class IdentifierListDto
    {
        [JsonPropertyName("CID")] public int[] CID { get; init; } = Array.Empty<int>();
    }

    private sealed class PropertyTableDto
    {
        [JsonPropertyName("PropertyTable")] public PropertyTableInnerDto? PropertyTable { get; init; }
    }

    private sealed class PropertyTableInnerDto
    {
        [JsonPropertyName("Properties")] public PropertyRowDto[] Properties { get; init; } = Array.Empty<PropertyRowDto>();
    }

    private sealed class PropertyRowDto
    {
        public string? MolecularFormula { get; init; }
        public double? MolecularWeight { get; init; }
        public double? ExactMass { get; init; }
        //public double? MonoIsotopicMass { get; init; } // sometimes capitalization differs; handle both if needed
        public double? MonoisotopicMass { get; init; }

        public string? CanonicalSMILES { get; init; }
        public string? IsomericSMILES { get; init; }
        public string? InChI { get; init; }
        public string? InChIKey { get; init; }
        public string? IUPACName { get; init; }

        public double? XLogP { get; init; }
        public double? TPSA { get; init; }

        public int? HBondDonorCount { get; init; }
        public int? HBondAcceptorCount { get; init; }
        public int? RotatableBondCount { get; init; }
        public int? Charge { get; init; }
    }
}