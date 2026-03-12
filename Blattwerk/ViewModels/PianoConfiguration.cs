namespace Blattwerk.ViewModels;

public sealed class PianoConfiguration : PitchConfiguration
{
    public override int GetValue(float normalizedValue) => (int)(21 + normalizedValue * (108 - 21));

}