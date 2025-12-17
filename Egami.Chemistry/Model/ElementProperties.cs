namespace Egami.Chemistry.Model;

public sealed record ElementProperties
{
    public required int AtomicNumber { get; init; }
    public required double AtomicWeight { get; init; }
    public double? ElectronegativityPauling { get; init; }
}