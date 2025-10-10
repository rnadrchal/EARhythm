namespace Egami.Rhythm.EA;

public class EvolutionContext
{
    public required double MutationRate { get; init; }
    public required double DeletionRate { get; init; }
    public required double InsertionRate { get; init; }
    public required double CrossoverRate { get; init; }
    public int? Seed { get; init; } = null;
}