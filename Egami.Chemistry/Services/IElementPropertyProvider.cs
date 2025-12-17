namespace Egami.Chemistry.Services;

public interface IElementPropertyProvider
{
    ElementProps? TryGet(string symbol);
}

public sealed record ElementProps(
    int AtomicNumber,
    double AtomicWeight,
    double? ElectronegativityPauling);