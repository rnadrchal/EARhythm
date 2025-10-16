using Egami.Rhythm.Common;
using Egami.Rhythm.EA.Extensions;
using Egami.Rhythm.EA.Mutation;

namespace Egami.Rhythm.EA;

public class Population<TGenotype>
{
    private IEvolutionOptions _options;

    public List<TGenotype> Individuals { get; } = new();

    public Population(TGenotype genotype, IEvolutionOptions options, int size = 8)
    {
        _options = options;
        for (int i = 0; i < size; i++)
        {
            Individuals.Add(genotype);
        }
    }

    public void Evolve(IMutator<TGenotype> mutator, int generations = 1)
    {
        for (int i = 0; i < generations; i++)
        {
            foreach (var individual in Individuals)
            {
                double r = RandomProvider.Get(_options.Seed).NextDouble();
                if (1.0 - r < _options.MutationRate)
                {
                    mutator.Mutate(individual, _options);
                }
                r = RandomProvider.Get(_options.Seed).NextDouble();
                if (1.0 - r < _options.DeletionRate)
                {
                    mutator.Delete(individual, _options);
                }
                r = RandomProvider.Get(_options.Seed).NextDouble();
                if (1.0 - r < _options.InsertionRate)
                {
                    mutator.Insert(individual, _options);
                }
            }
        }
    }

    public void Pairing(IMutator<TGenotype> mutator, Func<TGenotype, double> fitness)
    {
        var result = Individuals.FindTop2AndWorst(fitness);
        if (RandomProvider.Get(_options.Seed).NextDouble() > 1.0 - _options.CrossoverRate)
        {
            var offspring = mutator.Crossover(result.Best, result.Second, _options);
            Individuals.RemoveAt(result.WorstIndex);
            Individuals.Add(offspring);
        }
    }

    public void ApplyOptions(IEvolutionOptions options)
    {
        _options = options;
    }
}