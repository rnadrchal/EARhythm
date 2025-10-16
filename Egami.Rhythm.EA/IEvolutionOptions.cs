namespace Egami.Rhythm.EA;

public interface IEvolutionOptions
{
    double MutationRate { get; set; }
    double DeletionRate { get; set; }
    double InsertionRate { get; set; }
    double LengthRate { get; set; }
    double SwapRate { get; set; }
    double CrossoverRate { get; set; }
    int? Seed { get; set; }
}