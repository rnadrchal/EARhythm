using Egami.Rhythm.Core;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Generation;
using Egami.Rhythm.Generation;

[TestClass]
public class EuclidGeneratorTests
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
            TempoBpm = 120
        };
    }

    [TestMethod]
    [DataRow(4)]
    public void PatternIsProperlyGenerated(int pulses)
    {
        var generator = new EuclidGenerator(pulses);

        var pattern = generator.Generate(_ctx);

        pattern.StepsTotal.Should().Be(_ctx.StepsTotal);

        pattern.Lengths.Count(l => l > 0).Should().Be(pulses);
        pattern.Hits.Count(h => h).Should().Be(pulses);
        pattern.Velocities.Count(v => v > 0).Should().Be(pulses);

        var events = pattern.ToEvents().ToList();
        events.ForEach(e => pattern.Hits[e.Step].Should().BeTrue());
    }

}