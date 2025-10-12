using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Generation;

[TestClass]
public class PolyrhythmGeneratorTests
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
    [DataRow(4, 5, 60, 80, 2, 3)]
    public void PatternIsProperlyGenerated(int a, int b, int velA, int velB, int lengthA, int lengthB)
    {
        var generator = new PolyrhythmGenerator(a, b, (byte)velA, (byte)velB, lengthA, lengthB);

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