using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.EA;

public sealed class Evolution<TGenotype>
{
    public List<Population<TGenotype>> Populations { get; } = new();
    public void AddPopulation(TGenotype genotype, int size = 8)
    {
        Populations.Add(new Population<TGenotype>(genotype, size));
    }
}