using Egami.Rhythm.Common;
using Egami.Rhythm.EA.Extensions;
using Egami.Rhythm.EA.Mutation;

namespace Egami.Rhythm.EA;

public class Population<TGenotype>
{
    private IEvolutionOptions _options;

    public List<TGenotype> Individuals { get; } = new();

    public Population(TGenotype genotype, IEvolutionOptions options)
    {
        _options = options;
        for (int i = 0; i < options.PopulationSize; i++)
        {
            Individuals.Add(genotype);
        }
    }

    public void Evolve(TGenotype individual, IMutator<TGenotype> mutator)
    {
        if (RandomProvider.Get(_options.Seed).NextDouble() <= 1.0 / Individuals.Count)
        {
            mutator.Mutate(individual, _options);
        }
        var r = RandomProvider.Get(_options.Seed).NextDouble();
        if (r <= _options.DeletionRate)
        {
            mutator.Delete(individual, _options);
        }
        r = RandomProvider.Get(_options.Seed).NextDouble();
        if (r <= _options.InsertionRate)
        {
            mutator.Insert(individual, _options);
        }

        r = RandomProvider.Get(_options.Seed).NextDouble();
        if (r <= _options.SwapRate)
        {
            mutator.Swap(individual, _options);
        }
    }

    public void Evolve(IMutator<TGenotype> mutator, int generations = 1)
    {
        foreach (var individual in Individuals)
        {
            Evolve(individual, mutator);
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

    public void Tournament(IMutator<TGenotype> mutator, Func<TGenotype, double> fitness, IEvolutionOptions options)
    {
        var participants = Individuals.TakeRandom(4, RandomProvider.Get(_options.Seed))
            .OrderByDescending(fitness).Take(2);
        var offspring = mutator.Crossover(participants.First(), participants.Last(), _options);
        Evolve(offspring, mutator);
        var remove = Individuals.FirstOrDefault(i => Math.Abs(fitness(i) - fitness(offspring)) <= 0.1);
        if (remove == null)
        {
            remove = Individuals.OrderBy(fitness).Last();
        }

        Individuals.Remove(remove);
        Individuals.Add(offspring);
    }

    public void ApplyOptions(IEvolutionOptions options)
    {
        _options = options;
    }
}