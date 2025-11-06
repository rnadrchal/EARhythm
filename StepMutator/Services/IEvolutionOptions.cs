namespace StepMutator.Services;

public interface IEvolutionOptions
{
    int GenerationLength { get; }
    int PopulationSize { get; }

    double DeletionRate { get; }
    double InsertionRate { get; }
    double SwapRate { get; }
    double InversionRate { get; }
    double TranspositionRate { get; }
    double CrossoverRate { get; }
    int TournamentSize { get; set; }
    double ExtinctionRate { get; set; }
    int MaxOffsprings { get; set; }

    int? Seed { get; }
}