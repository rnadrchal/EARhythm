namespace StepMutator.Services;

public interface IEvolutionOptions
{
    int GenerationLength { get; }
    int? Seed { get; }
}