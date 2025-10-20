using Egami.Rhythm;
using Egami.Rhythm.Pattern;
using Egami.Rhythm.Transformation;

namespace Egami.Pitch;

public sealed class PitchTransform : IRhythmTransform
{
    private int[] _pitches;
    private bool _hitOnly;
    public PitchTransform(IEnumerable<int> pitches, bool hitOnly)
    {
        _pitches = pitches.ToArray();
        _hitOnly = hitOnly;
    }
    public Sequence Apply(RhythmContext ctx, Sequence input)
    {
        int j = 0;
        for (int i = 0; i < ctx.StepsTotal; i++)
        {
            if (input.Steps[i].Hit)
            {
                input.Steps[i].Pitch = _pitches[j++];
            }
            else if (!_hitOnly) j++;

            if (j >= _pitches.Length) j = 0;
        }
        return input;
    }
}