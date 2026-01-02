namespace Egami.Chemistry.Model;

public sealed record Atom2D(
    int Index, 
    string Element, 
    double X, 
    double Y, 
    int? FormalCharge = null);