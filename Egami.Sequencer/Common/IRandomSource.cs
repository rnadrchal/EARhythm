namespace Egami.Sequencer.Common;

public interface IRandomSource
{
    int NextInt(int minInclusive, int maxInclusive);
    double NextDouble();
}