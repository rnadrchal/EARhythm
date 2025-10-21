namespace Egami.Rhythm.EA.Mutation;

public interface IMutator<TGenotype>
{
    void Mutate(TGenotype individual, IEvolutionOptions options);
    void Delete(TGenotype individual, IEvolutionOptions options);
    void Insert(TGenotype individual, IEvolutionOptions options);
    void Swap(TGenotype individual, IEvolutionOptions options);
    TGenotype Crossover(TGenotype individual1, TGenotype individual2, IEvolutionOptions options);
}