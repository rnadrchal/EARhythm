using Egami.Rhythm.Common;
using Egami.Rhythm.EA.Extensions;
using Egami.Rhythm.EA.Mutation;

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

    public void Evolve(EvolutionContext ctx, IMutator<TGenotype> mutator, int generations = 1)
    {
        for (int i = 0; i < generations; i++)
        {
            foreach (var individual in Individuals)
            {
                double r = RandomProvider.Get(ctx.Seed).NextDouble();
                if (1.0 - r < ctx.MutationRate)
                {
                    mutator.Mutate(individual, ctx);
                }
                r = RandomProvider.Get(ctx.Seed).NextDouble();
                if (1.0 - r < ctx.DeletionRate)
                {
                    mutator.Delete(individual, ctx);
                }
                r = RandomProvider.Get(ctx.Seed).NextDouble();
                if (1.0 - r < ctx.InsertionRate)
                {
                    mutator.Insert(individual, ctx);
                }
            }
        }
    }

    public void Pairing(EvolutionContext ctx, IMutator<TGenotype> mutator, Func<TGenotype, double> fitness)
    {
        var result = Individuals.FindTop2AndWorst(fitness);
        if (RandomProvider.Get(ctx.Seed).NextDouble() > 1.0 - ctx.CrossoverRate)
        {
            var offspring = mutator.Crossover(result.Best, result.Second, ctx);
            Individuals.RemoveAt(result.WorstIndex);
            Individuals.Add(offspring);
        }
    }
}