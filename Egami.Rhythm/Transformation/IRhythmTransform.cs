using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Transformation;

public interface IRhythmTransform
{
    RhythmPattern Apply(RhythmContext ctx, RhythmPattern input);
}