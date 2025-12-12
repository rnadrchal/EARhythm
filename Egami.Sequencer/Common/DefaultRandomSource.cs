namespace Egami.Sequencer.Common;

public sealed class DefaultRandomSource : IRandomSource
{
    private readonly Random _random;

    public DefaultRandomSource(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int NextInt(int minInclusive, int maxExclusive)
        => _random.Next(minInclusive, maxExclusive);

    public double NextDouble()
        => _random.NextDouble();
}