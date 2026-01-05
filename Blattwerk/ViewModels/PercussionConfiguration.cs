namespace Blattwerk.ViewModels;

public sealed class PercussionConfiguration : PitchConfiguration
{
    public override int GetValue(float normalizedValue) => (int)(35 + normalizedValue * (81 - 35));
}
