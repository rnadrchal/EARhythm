using System.Diagnostics;
using Egami.Chemistry.Model;
using Egami.Chemistry.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Egami.Chemistry.PubChem;

public sealed class PubChemClient
{
    private readonly HttpClient _http;
    private readonly MoleculeModelBuilder _modelBuilder;

    public PubChemClient(HttpClient http, MoleculeModelBuilder modelBuilder)
    {
        _http = http;
        _modelBuilder = modelBuilder;
    }

    public async Task<IReadOnlyList<int>> SearchCidsByNameAsync(string name, CancellationToken ct = default)
    {
        var url = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/{Uri.EscapeDataString(name)}/cids/JSON";
        var dto = await _http.GetFromJsonAsync<CidListDto>(url, ct);
        return dto?.IdentifierList?.CID ?? Array.Empty<int>();
    }

    public async Task<MoleculeModel> GetMoleculeModelAsync(int cid, CancellationToken ct = default)
    {
        var propsUrl =
            "https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/" + cid +
            "/property/MolecularFormula,MolecularWeight,ExactMass,MonoisotopicMass," +
            "CanonicalSMILES,IsomericSMILES,InChI,InChIKey,IUPACName,XLogP,TPSA," +
            "HBondDonorCount,HBondAcceptorCount,RotatableBondCount,Charge/JSON";

        //var raw = await _http.GetStringAsync(propsUrl, ct);
        //Debug.WriteLine(raw);
        var props = await _http.GetFromJsonAsync<PropertyTableDto>(propsUrl, ct);
        var p = props?.PropertyTable?.Properties?.FirstOrDefault()
            ?? throw new InvalidOperationException($"No properties returned for CID {cid}.");

        var smiles = p.ConnectivitySMILES ?? p.IsomericSMILES ?? p.CanonicalSMILES ?? p.SMILES;
        if (string.IsNullOrWhiteSpace(smiles))
            throw new InvalidOperationException($"No SMILES returned for CID {cid}.");

        var graph = _modelBuilder.BuildGraphFromSmiles(smiles);

        var depictionUri = new Uri($"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/PNG");

        byte[]? depictionPng = null;
        try
        {
            using var resp = await _http.GetAsync(depictionUri, ct);
            if (resp.IsSuccessStatusCode)
            {
                depictionPng = await resp.Content.ReadAsByteArrayAsync(ct);
            }
            else
            {
                Debug.WriteLine($"Warning: PubChem PNG request for CID {cid} returned status {resp.StatusCode}.");
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Warning: failed to download depiction PNG for CID {cid}: {ex.Message}");
        }

        return new MoleculeModel
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
                MonoisotopicMass = p.MonoIsotopicMass ?? p.MonoisotopicMass ?? p.MonoisotopicMassFallback,
                HydrogenBondDonorCount = p.HBondDonorCount,
                HydrogenBondAcceptorCount = p.HBondAcceptorCount,
                RotatableBondCount = p.RotatableBondCount,
                XLogP = p.XLogP,
                TopologicalPolarSurfaceArea = p.TPSA,
                Charge = p.Charge
            },

            Graph = graph,

            // PNG bytes (optional, kann null sein wenn Download fehlschlägt)
            DepictionPng = depictionPng,

            // Optional extras (Synonyms/Taxonomy/MeSH SCR) kannst du später per weiteren Endpoints ergänzen
            Synonyms = Array.Empty<string>()
        };
    }

    // --- DTOs ---

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
        // Einige Felder im PubChem-JSON kommen mit anderen Keys (z.B. "SMILES"), deshalb explizit mappen.
        [JsonPropertyName("CID")] public int? CID { get; init; }

        [JsonPropertyName("MolecularFormula")] public string? MolecularFormula { get; init; }

        // PubChem kann Zahlen als Strings liefern; für Robustheit als double? belassen (bei Bedarf string->double parsen).
        [JsonPropertyName("MolecularWeight")] public double? MolecularWeight { get; init; }
        [JsonPropertyName("ExactMass")] public double? ExactMass { get; init; }

        // PubChem key: "MonoisotopicMass"
        [JsonPropertyName("MonoisotopicMass")] public double? MonoisotopicMass { get; init; }
        [JsonIgnore]
        public double? MonoIsotopicMass => MonoisotopicMass;
        [JsonIgnore]
        public double? monoIsotopicMass => MonoisotopicMass; // zusätzliche alias-Form (falls irgendwo klein geschrieben referenziert)

        // SMILES-Felder (PubChem liefert "SMILES" und "ConnectivitySMILES")
        [JsonPropertyName("CanonicalSMILES")] public string? CanonicalSMILES { get; init; }
        [JsonPropertyName("IsomericSMILES")] public string? IsomericSMILES { get; init; }
        [JsonPropertyName("SMILES")] public string? SMILES { get; init; }
        [JsonPropertyName("ConnectivitySMILES")] public string? ConnectivitySMILES { get; init; }

        [JsonPropertyName("InChI")] public string? InChI { get; init; }
        [JsonPropertyName("InChIKey")] public string? InChIKey { get; init; }
        [JsonPropertyName("IUPACName")] public string? IUPACName { get; init; }

        [JsonPropertyName("XLogP")] public double? XLogP { get; init; }
        [JsonPropertyName("TPSA")] public double? TPSA { get; init; }

        [JsonPropertyName("HBondDonorCount")] public int? HBondDonorCount { get; init; }
        [JsonPropertyName("HBondAcceptorCount")] public int? HBondAcceptorCount { get; init; }
        [JsonPropertyName("RotatableBondCount")] public int? RotatableBondCount { get; init; }
        [JsonPropertyName("Charge")] public int? Charge { get; init; }

        // Fallback für MonoisotopicMass falls PubChem in Zukunft andere Schreibweise nutzt (nur defensive Ergänzung)
        [JsonIgnore] public double? MonoisotopicMassFallback => MonoisotopicMass;
    }
}