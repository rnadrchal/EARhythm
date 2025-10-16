using Egami.Pitch;
using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using FluentAssertions;

namespace Egami.Rhythm.Tests.PitchGeneration;

[TestClass]
public class PitchTransformationTests
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
            Timebase = new Timebase(120),
            Seed = 12345
        };
    }

    [TestMethod]
    [DataRow(60)]
    public void ConstantPitchesAreAppliedToRhythmPattern(int pitch)
    {
        var pattern = new BernoulliGenerator().Generate(_ctx);
        var pitches = new ConstantPitchGenerator().Generate((byte)pitch, pattern.StepsTotal);
        new PitchTransform(pitches).Apply(_ctx, pattern);

        pattern.Hits.Count(h => h).Should().BeGreaterThan(0);
        pattern.Pitches.Count(p => p == pitch).Should().Be(pattern.Hits.Count(h => h));
    }
}