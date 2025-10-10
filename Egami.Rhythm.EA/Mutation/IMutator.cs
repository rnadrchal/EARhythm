namespace Egami.Rhythm.EA.Mutation;

public interface IMutator<TGenotype>
{
    void Mutate(TGenotype individual, EvolutionContext ctx);
    void Delete(TGenotype individual, EvolutionContext ctx);
    void Insert(TGenotype individual, EvolutionContext ctx);
    TGenotype Crossover(TGenotype individual1, TGenotype individual2, EvolutionContext ctx);
}