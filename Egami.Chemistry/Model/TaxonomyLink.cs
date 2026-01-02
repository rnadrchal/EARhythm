namespace Egami.Chemistry.Model;

public sealed record TaxonomyLink(
    int NcbiTaxonomyId, 
    string? ScientificName, 
    string? Source = null);