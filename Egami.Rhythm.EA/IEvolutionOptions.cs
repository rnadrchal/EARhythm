namespace Egami.Rhythm.EA;

public interface IEvolutionOptions
{
    int PopulationSize { get; set; }
    double DeletionRate { get; set; }
    double InsertionRate { get; set; }
    double SwapRate { get; set; }
    double InversionRate { get; set; }
    double TranspositionRate { get; set; }
    double RetrogradeRate { get; set; }
    double MelodicInversionRate { get; set; }
    double CrossoverRate { get; set; }
    int MaxStepLength { get; set; }
    int? Seed { get; set; }
}