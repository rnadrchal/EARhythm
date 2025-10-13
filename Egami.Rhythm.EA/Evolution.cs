using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA;

public sealed class Evolution<TGenotype>
{
    public List<Population<TGenotype>> Populations { get; } = new();
    public Population<TGenotype> AddPopulation(TGenotype genotype, int size = 10)
    {
        var population = new Population<TGenotype>(genotype, size);
        Populations.Add(population);
        return population;
    }

    public void RemovePopulation(Population<TGenotype> population)
    {
        Populations.Remove(population);
    }
}