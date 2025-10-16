using Egami.Rhythm;
using Egami.Rhythm.Pattern;
using Egami.Rhythm.Transformation;

namespace Egami.Pitch;

public sealed class PitchTransform : IRhythmTransform
{
    private byte?[] _pitches;
    public PitchTransform(IEnumerable<byte?> pitches)
    {
        _pitches = pitches.ToArray();
    }
    public RhythmPattern Apply(RhythmContext ctx, RhythmPattern input)
    {
        input.Pitches = new int?[ctx.StepsTotal];
        int j = 0;
        for (int i = 0; i < ctx.StepsTotal; i++)
        {
            input.Pitches[i] = input.Hits[i] ? _pitches[j++ % _pitches.Length] : null;
        }
        return input;
    }
}