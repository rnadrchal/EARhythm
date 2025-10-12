using Egami.Rhythm.EA;
using Egami.Rhythm.EA.Mutation;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;
using FluentAssertions;

namespace Egami.Rhythm.Tests;

[TestClass]
public class RhythmPatternMutatorTests
{
    private EvolutionContext _ctx;
    private RhythmPattern _individual1;
    private RhythmPattern _individual2;


    [TestInitialize]
    public void Initialize()
    {
        _ctx = new EvolutionContext
        {
            MutationRate = 0.1,
            DeletionRate = 0.01,
            InsertionRate = 0.01,
            CrossoverRate = 0.7,
        };

        var rhythmContext = new RhythmContext
        {
            StepsTotal = 16,
            DefaultVelocity = 100,
            Meter = new(4, 4),
            Timebase = new(4),
            Seed = 12345
        };
        _individual1 = new EuclidGenerator(2).Generate(rhythmContext);
    }

    [TestMethod]
    public void Mutate_ShouldReturnMutatedIndividual()
    {
        var mutator = new RhythmPatternMutator();

        var originalHits = (bool[])_individual1.Hits.Clone();
        mutator.Mutate(_individual1, _ctx);
        _individual1.Hits.Should().NotBeEquivalentTo(originalHits);
        int mutationCount = 0;
        for (int i = 0; i < originalHits.Length; i++)
        {
            if (originalHits[i] != _individual1.Hits[i])
            {
                mutationCount++;
            }
        }

        mutationCount.Should().Be(1);
    }

    [TestMethod]
    public void Mutate_DeleteRemovesASinglePositionFromTheRhythmPattern()
    {
        var mutator = new RhythmPatternMutator();

        var originalLength = _individual1.Hits.Length;
        mutator.Delete(_individual1, _ctx);
        _individual1.Hits.Length.Should().Be(originalLength - 1);
    }

    [TestMethod]
    public void Mutate_InsertAddsASinglePositionToTheRhythmPattern()
    {
        var mutator = new RhythmPatternMutator();
        var originalLength = _individual1.Hits.Length;
        mutator.Insert(_individual1, _ctx);
        _individual1.Hits.Length.Should().Be(originalLength + 1);
        _individual1.Lengths.Length.Should().Be(originalLength + 1);
        _individual1.Velocity.Length.Should().Be(originalLength + 1);
        _individual1.StepsTotal.Should().Be(originalLength + 1);
        _individual1.Pitches.Length.Should().Be(originalLength + 1);
    }

    [TestMethod]
    [DataRow(2)]
    public void Crossover_ShouldReturnNewIndividualWithCombinedGenes(int deltaLength)
    {
        var mutator = new RhythmPatternMutator();
        var rhythmContext = new RhythmContext
        {
            StepsTotal = _individual1.StepsTotal + deltaLength,
            DefaultVelocity = 100,
            Meter = new(4, 4),
            Timebase = new(4),
            Seed = 12345
        };
        _individual2 = new EuclidGenerator(6).Generate(rhythmContext);
        var offspring = mutator.Crossover(_individual1, _individual2, _ctx);
        offspring.Hits.Length.Should().BeGreaterThan(0);
        offspring.Hits.Length.Should().BeLessThanOrEqualTo(Math.Max(_individual1.Hits.Length, _individual2.Hits.Length));
        offspring.Hits.Should().NotBeEquivalentTo(_individual1.Hits);
        offspring.Hits.Should().NotBeEquivalentTo(_individual2.Hits);
        (offspring.StepsTotal == _individual1.StepsTotal && offspring.StepsTotal == _individual2.StepsTotal).Should().NotBe(true);
    }
}