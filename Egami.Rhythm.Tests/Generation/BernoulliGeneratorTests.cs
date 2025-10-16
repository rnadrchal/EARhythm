using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Generation;

[TestClass]
public class BernoulliGeneratorTests
{
    private RhythmContext _ctx;
    [TestInitialize]
    public void Setup()
    {
        _ctx = new RhythmContext
        {
            StepsTotal = 16,
            DefaultVelocity = 100,
            Meter = new Meter(4, 4),
            Timebase = new Timebase(4), // 16tel Raster
            TempoBpm = 120,
            Seed = 1234
        };
    }

    [TestMethod]
    [DataRow(0.5)]
    public void PatternIsProperlyGenerated(double probability01)
    {
        var generator = new BernoulliGenerator(probability01);

        var pattern = generator.Generate(_ctx);

        pattern.StepsTotal.Should().Be(_ctx.StepsTotal);

        var hits = pattern.Hits.Count(h => h);
        hits.Should().BeGreaterThan(0);
        pattern.Lengths.Count(l => l > 0).Should().Be(hits);
        pattern.Velocities.Count(v => v > 0).Should().Be(hits);

        var events = pattern.ToEvents().ToList();
        events.ForEach(e => pattern.Hits[e.Step].Should().BeTrue());
    }
}