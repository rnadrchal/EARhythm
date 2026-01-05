using System;

namespace Blattwerk.ViewModels;

public static class PitchConfigurationFactory
{
    public static PitchConfiguration Create(PitchType pitchType) => pitchType switch
    {
        PitchType.Percussion => new PercussionConfiguration(),
        PitchType.DrumKit => new DrumkitConfiguration(),
        PitchType.Piano => new PianoConfiguration(),
        PitchType.Vocals => new VocalConfiguration(),
        PitchType.Strings => new StringsConfiguration(),
        PitchType.Reeds => new ReedsConfiguration(),
        _ => throw new NotSupportedException($"Pitch type {pitchType} is not supported."),
    };
}