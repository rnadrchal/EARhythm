using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Transformation;

public interface IRhythmTransform
{
    Sequence Apply(RhythmContext ctx, Sequence input);
}