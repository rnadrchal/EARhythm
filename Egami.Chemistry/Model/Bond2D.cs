namespace Egami.Chemistry.Model;

public sealed record Bond2D(
    int FromAtomIndex, 
    int ToAtomIndex, 
    int Order, 
    string? Stereo = null);