using Egami.Sequencer;
using Melanchall.DryWetMidi.Common;
using TextSequencer.ViewModels;

namespace TextSequencer.Services.SequenceGeneration;

public sealed class HarmonyFromChars : IMusicalSequenceFromChars
{
    public MusicalSequence Generate(ICharacterArray characterArray)
    {
        var steps = new SequenceStep[characterArray.IndicesPc.Length];
        for (var i = 0; i < steps.Length; i++)
        {
            steps[i] = characterArray.IndicesPc[i] >= 0
                ? new SequenceStep(i * 16, 16, (SevenBitNumber)(characterArray.IndicesPc[i] + 60), characterArray.Characters[i].CharToVelocity())
                : new SequenceStep(i * 16, 16, (SevenBitNumber)0, (SevenBitNumber)0);

        }

        return new MusicalSequence(steps);
    }
}