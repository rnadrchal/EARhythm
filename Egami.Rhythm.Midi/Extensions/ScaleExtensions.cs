using Melanchall.DryWetMidi.MusicTheory;

namespace Egami.Rhythm.Midi.Extensions;

public static class ScaleExtensions
{
    public static IEnumerable<int> ToIndices(this IEnumerable<Interval> intervals)
    {
        var i = 0;
        yield return i;
        foreach (var interval in intervals)
        {
            i += interval;
            yield return i;
        }
    }
}