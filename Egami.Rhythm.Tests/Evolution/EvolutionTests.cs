using Egami.Rhythm.EA;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Evolution;

[TestClass]
public class EvolutionTests
{
    private Evolution<RhythmPattern> _evolution = null!;
    private RhythmContext _ctx;

    [TestInitialize]
    public void Initialize()
    {
        _evolution = new Evolution<RhythmPattern>();
        _ctx = new RhythmContext
        {
            StepsTotal = 16,
            DefaultVelocity = 100,
            Meter = new(4, 4),
            Timebase = new(4)
        };
    }

    [TestMethod]
    [DataRow(5)]
    public void AddPopulation_ShouldAddPopulationAndGenerateIndividuals(int populationSize)
    {
        var genotype = new EuclidGenerator(4).Generate(_ctx);
        _evolution.AddPopulation(genotype, size: populationSize);

        _evolution.Populations.Count.Should().Be(1);
        _evolution.Populations[0].Individuals.Count.Should().Be(populationSize);
    }
}