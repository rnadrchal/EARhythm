using Egami.Rhythm.Common;

namespace Egami.Rhythm.EA;

public class Population<TGenotype>
{
    public List<TGenotype> Individuals { get; } = new();

    public Population(TGenotype genotype, int size = 8)
    {
        for (int i = 0; i < size; i++)
        {
            Individuals.Add(genotype);
        }
    }

    public void PerformMutations(EvolutionContext ctx)
    {
        foreach (var individual in Individuals)
        {
            double r = RandomProvider.Get(ctx.Seed).NextDouble();
        }
    }
}