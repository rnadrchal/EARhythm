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
    int? Seed { get; }
}