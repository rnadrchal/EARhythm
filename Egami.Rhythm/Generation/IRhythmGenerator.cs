using Egami.Rhythm.Pattern;

namespace Egami.Rhythm.Generation;

// Minimales, gemeinsames Interface aller Generatoren
public interface IRhythmGenerator
{
    Sequence Generate(RhythmContext ctx);
}