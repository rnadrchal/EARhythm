using System.Diagnostics;
using System.Collections.Concurrent;
using Egami.Chemistry.Model;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NCDK;
using NCDK.IO;
using NCDK.Silent;

namespace Egami.Chemistry.PubChem;

public sealed class PubChemClient
{
    private readonly HttpClient _http;
    private readonly MoleculeModelBuilder _modelBuilder;

    // einfacher in-memory cache, reduziert Netzlast
    private readonly ConcurrentDictionary<int, string?> _sdf3dTextCache = new();
    private readonly ConcurrentDictionary<int, IAtomContainer?> _sdf3dContainerCache = new();

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

        var props = await _http.GetFromJsonAsync<PropertyTableDto>(propsUrl, ct);
        var p = props?.PropertyTable?.Properties?.FirstOrDefault()
            ?? throw new InvalidOperationException($"No properties returned for CID {cid}.");

        var smiles = p.ConnectivitySMILES ?? p.IsomericSMILES ?? p.CanonicalSMILES ?? p.SMILES;
        if (string.IsNullOrWhiteSpace(smiles))
            throw new InvalidOperationException($"No SMILES returned for CID {cid}.");

        // Standard: Graph aus SMILES (Fallback)
        var graph = _modelBuilder.BuildGraphFromSmiles(smiles);

        // Versuche 3D-SDF zu holen und zu parsen. Falls vorhanden und wirklich 3D-Koordinaten enthält,
        // baue den Graph direkt aus dem IAtomContainer (dann liefert BondEdgeEnricher Length3D).
        try
        {
            var mol3d = await Fetch3dAtomContainerAsync(cid, ct);
            if (mol3d != null)
            {
                if (Has3DCoordinates(mol3d))
                {
                    graph = _modelBuilder.BuildGraphFromAtomContainer(mol3d);
                    Debug.WriteLine($"PubChemClient: used 3D SDF for CID {cid}, atoms with Point3D: {mol3d.Atoms.Count(a => a.Point3D != null)}");
                }
                else
                {
                    Debug.WriteLine($"PubChemClient: 3D SDF for CID {cid} parsed but contains no Point3D. Falling back to SMILES graph.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: failed to fetch/parse 3D SDF for CID {cid}: {ex.Message}");
        }

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

            Synonyms = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Lädt das 3D-SDF (text) für eine CID und cached es in-memory.
    /// Endpoint: /SDF?record_type=3d
    /// </summary>
    public async Task<string?> Fetch3dSdfAsync(int cid, CancellationToken ct = default)
    {
        if (_sdf3dTextCache.TryGetValue(cid, out var cached)) return cached;

        var url = $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/SDF?record_type=3d";
        string? text = null;
        try
        {
            text = await _http.GetStringAsync(url, ct);
            if (string.IsNullOrWhiteSpace(text)) text = null;
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Warning: failed to download 3D SDF for CID {cid}: {ex.Message}");
            text = null;
        }

        _sdf3dTextCache[cid] = text;
        return text;
    }

    /// <summary>
    /// Parsed den SDF-Text mit NCDK (MDLV2000Reader) in ein IAtomContainer und cached das Ergebnis.
    /// </summary>
    public async Task<IAtomContainer?> Fetch3dAtomContainerAsync(int cid, CancellationToken ct = default)
    {
        if (_sdf3dContainerCache.TryGetValue(cid, out var cached)) return cached;

        var txt = await Fetch3dSdfAsync(cid, ct);
        if (string.IsNullOrWhiteSpace(txt))
        {
            _sdf3dContainerCache[cid] = null;
            return null;
        }

        try
        {
            using var sr = new StringReader(txt);
            using var reader = new MDLV2000Reader(sr);
            var builder = ChemObjectBuilder.Instance;
            var mol = reader.Read(builder.NewAtomContainer()) as IAtomContainer;
            _sdf3dContainerCache[cid] = mol;
            return mol;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: parsing 3D SDF for CID {cid} failed: {ex.Message}");
            _sdf3dContainerCache[cid] = null;
            return null;
        }
    }

    private static bool Has3DCoordinates(IAtomContainer mol)
    {
        if (mol is null) return false;
        foreach (var a in mol.Atoms)
        {
            if (a.Point3D != null) return true;
        }
        return false;
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
        [JsonPropertyName("CID")] public int? CID { get; init; }
        [JsonPropertyName("MolecularFormula")] public string? MolecularFormula { get; init; }
        [JsonPropertyName("MolecularWeight")] public double? MolecularWeight { get; init; }
        [JsonPropertyName("ExactMass")] public double? ExactMass { get; init; }
        [JsonPropertyName("MonoisotopicMass")] public double? MonoisotopicMass { get; init; }
        [JsonIgnore] public double? MonoIsotopicMass => MonoisotopicMass;
        [JsonIgnore] public double? monoIsotopicMass => MonoisotopicMass;
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
        [JsonIgnore] public double? MonoisotopicMassFallback => MonoisotopicMass;
    }
}