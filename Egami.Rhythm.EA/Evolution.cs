using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA;

public sealed class Evolution<TGenotype>
{
    private IEvolutionOptions _options;

    public Evolution(IEvolutionOptions options)
    {
        _options = options;
    }

    public List<Population<TGenotype>> Populations { get; } = new();
    public Population<TGenotype> AddPopulation(TGenotype genotype, int size = 10)
    {
        var population = new Population<TGenotype>(genotype, _options, size);
        Populations.Add(population);
        return population;
    }

    public void ApplyOptions(IEvolutionOptions options)
    {
        _options = options;
        foreach (var population in Populations)
        {
            // Update options for existing populations if needed
            population.ApplyOptions(options);
        }
    }

    public void RemovePopulation(Population<TGenotype> population)
    {
        Populations.Remove(population);
    }
}