using Egami.Pitch;
using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Extensions;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Evolution;

[TestClass]
public class EvolutionTests
{
    private Evolution<RhythmPattern> _evolution = null!;
    private RhythmContext _ctx;
    private EvolutionContext _evolutionContext;

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
        _evolutionContext = new EvolutionContext
        {
            MutationRate = 0.1,
            DeletionRate = 0.01,
            InsertionRate = 0.01,
            CrossoverRate = 0.7
        };
    }

    [TestMethod]
    [DataRow(5)]
    public void AddPopulation_ShouldAddPopulationAndGenerateIndividuals(int populationSize)
    {
        var pitches = new ConstantPitchGenerator().Generate(60, _ctx.StepsTotal);
        var genotype = new EuclidGenerator(4).GenerateWith(_ctx, new PitchTransform(pitches));
        _evolution.AddPopulation(genotype, size: populationSize);

        _evolution.Populations.Count.Should().Be(1);
        _evolution.Populations[0].Individuals.Count.Should().Be(populationSize);
    }

    [TestMethod]
    [DataRow(10)]
    public void Evolve_ShouldEvolvePopulation(int populationSize)
    {
        var pitches = new ConstantPitchGenerator().Generate(60, _ctx.StepsTotal);
        var genotype = new EuclidGenerator(4).GenerateWith(_ctx, new PitchTransform(pitches));
        _evolution.AddPopulation(genotype, size: populationSize);
        var initialIndividuals = _evolution.Populations[0].Individuals.Select(ind => ind.Clone()).ToList();
        _evolution.Populations[0].Evolve(_evolutionContext, new RhythmPatternMutator(), 5);
        var evolvedIndividuals = _evolution.Populations[0].Individuals;
        evolvedIndividuals.Count.Should().Be(populationSize);
        evolvedIndividuals.Should().NotBeEquivalentTo(initialIndividuals);
    }
}