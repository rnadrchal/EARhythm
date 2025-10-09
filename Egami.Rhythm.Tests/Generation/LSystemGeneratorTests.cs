using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Generation;

[TestClass]
public class LSystemGeneratorTests
{
    private RhythmContext _ctx;
    private Dictionary<char, string> _rules;

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

        // Periodisch
        _rules = new Dictionary<char, string>
        {
            { 'A', "AB" },
            { 'B', "A" }
        };
    }

    [TestMethod]
    [DataRow("A", "B")]
    public void PatternIsProperlyGenerated(string axiom, string hitSymbol)
    {
        var generator = new LSystemGenerator(axiom, _rules, 10, hitSymbol[0]);

        var pattern = generator.Generate(_ctx);
        pattern.StepsTotal.Should().Be(_ctx.StepsTotal);

        var hits = pattern.Hits.Count(h => h);
        hits.Should().BeGreaterThan(0);
        pattern.Lengths.Count(l => l > 0).Should().Be(hits);
        pattern.Velocity.Count(v => v > 0).Should().Be(hits);

        var events = pattern.ToEvents().ToList();
        events.ForEach(e => pattern.Hits[e.Step].Should().BeTrue());
    }

}