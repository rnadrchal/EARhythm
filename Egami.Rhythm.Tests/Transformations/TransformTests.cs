using Egami.Rhythm.Core;
using Egami.Rhythm.Extensions;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Transformation;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Transformations;

[TestClass]
public class TransformTests
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
            Seed = 999999999
        };
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(100)]
    [DataRow(-100)]
    [DataRow(0)]
    public void TestRotateTransform(int rotate)
    {
        var original = new BernoulliGenerator().Generate(_ctx);
        var transform = new RotateTransform(rotate);
        var transformed = transform.Apply(_ctx, original);

        for (int i = 0; i < original.Hits.Length; ++i)
        {
            if (rotate > 0)
            {
                original.Hits[i].Should().Be(transformed.Hits[(i + rotate) % _ctx.StepsTotal]);
            }
            else
            {
                transformed.Hits[i].Should().Be(original.Hits[(i - rotate) % _ctx.StepsTotal]);
            }
        }
    }

    [TestMethod]
    [DataRow(1.2, 0.8)]
    public void TestSwingVelocityTransform(double strong, double weak)
    {
        var pattern = new BernoulliGenerator().Generate(_ctx);
        var original = pattern.ToEvents().ToList();
        var transformed = new SwingVelocityTransform(strong, weak).Apply(_ctx, pattern).ToEvents().ToList();

        original.Count.Should().Be(transformed.Count);
        original.All(o => o.Velocity == _ctx.DefaultVelocity).Should().BeTrue();
        transformed.All(t => t.Velocity == (byte)(_ctx.DefaultVelocity * strong) || t.Velocity == (byte)(_ctx.DefaultVelocity * weak))
            .Should().BeTrue();
    }

    [TestMethod]
    [DataRow(1000.0, -1000.0)]
    public void AllSwingVelocitiesAreValid(double strong, double weak)
    {
        var pattern = new BernoulliGenerator().Generate(_ctx);
        var original = pattern.ToEvents().ToList();
        var transformed = new SwingVelocityTransform(strong, weak).Apply(_ctx, pattern).ToEvents().ToList();

        transformed.Select(t => t.Velocity).All(v => (v <= 127)).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(1.2, 0.8, 1)]
    public void TransformsCanBeCombined(double strong, double weak, double r)
    {
        var generator = new BernoulliGenerator();
        var rotate = new RotateTransform((int)r);
        var swing = new SwingVelocityTransform(strong, weak);
        var plain = generator.Generate(_ctx);
        var combined = new BernoulliGenerator().GenerateWith(_ctx, rotate, swing);

        for (int i = 0; i < plain.Hits.Length; ++i)
        {
            if (r > 0)
            {
                plain.Hits[i].Should().Be(combined.Hits[((int)(i + r)) % _ctx.StepsTotal]);
            }
            else
            {
                combined.Hits[i].Should().Be(plain.Hits[(byte)(i - r) % _ctx.StepsTotal]);
            }
        }

        var events = combined.ToEvents();
        events.Select(e => e.Velocity).All(v => v == (byte)(strong * _ctx.DefaultVelocity) || v == (byte)(weak * _ctx.DefaultVelocity))
            .Should().BeTrue();
    }

}